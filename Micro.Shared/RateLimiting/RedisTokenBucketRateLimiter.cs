using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Micro.Shared.RateLimiting.Models;

namespace Micro.Shared.RateLimiting;

public class RedisTokenBucketRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenBucketRateLimiter> _logger;

    // Lua script for atomic token bucket evaluation
    // KEYS[1] = bucket key
    // ARGV[1] = capacity
    // ARGV[2] = tokens_per_period
    // ARGV[3] = period_ms
    // ARGV[4] = cost
    // ARGV[5] = ttl_seconds
    private const string Script = @"
        local key = KEYS[1]
        local capacity = tonumber(ARGV[1])
        local rate = tonumber(ARGV[2])
        local period_ms = tonumber(ARGV[3])
        local cost = tonumber(ARGV[4])
        local ttl_seconds = tonumber(ARGV[5])

        -- Use Redis time to prevent clock drift issues between application servers
        local redis_time = redis.call('TIME')
        -- time[1] is seconds, time[2] is microseconds
        local current_time_ms = tonumber(redis_time[1]) * 1000 + math.floor(tonumber(redis_time[2]) / 1000)

        local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
        local tokens = tonumber(bucket[1])
        local last_refill = tonumber(bucket[2])

        if not tokens or not last_refill then
            -- First time seeing this bucket
            tokens = capacity
            last_refill = current_time_ms
        else
            -- Calculate tokens to add based on elapsed time
            local elapsed_ms = math.max(0, current_time_ms - last_refill)
            local refill_amount = (elapsed_ms / period_ms) * rate
            tokens = math.min(capacity, tokens + refill_amount)
        end

        local allowed = 0
        local retry_after_ms = -1

        if tokens >= cost then
            allowed = 1
            tokens = tokens - cost
            last_refill = current_time_ms
            
            redis.call('HMSET', key, 'tokens', tokens, 'last_refill', last_refill)
            redis.call('EXPIRE', key, ttl_seconds)
        else
            -- Not enough tokens, do not deduct. Calculate wait time.
            local deficit = cost - tokens
            -- How many ms to get 'deficit' tokens?
            -- time = (deficit / rate) * period_ms
            retry_after_ms = math.ceil((deficit / rate) * period_ms)
        end

        return { allowed, tokens, retry_after_ms }
    ";

    public RedisTokenBucketRateLimiter(IConnectionMultiplexer redis, ILogger<RedisTokenBucketRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RateLimitResult> IsAllowedAsync(string key, RateLimitOptions options, int cost = 1)
    {
        try
        {
            var db = _redis.GetDatabase();
            
            var periodMs = options.PeriodSeconds * 1000;
            
            var result = (RedisResult[]?) await db.ScriptEvaluateAsync(Script,
                new RedisKey[] { key },
                new RedisValue[] 
                { 
                    options.Capacity, 
                    options.TokensPerPeriod, 
                    periodMs, 
                    cost, 
                    options.TtlSeconds 
                });

            if (result == null || result.Length != 3)
            {
                _logger.LogWarning("Redis rate limiter script returned invalid result.");
                return new RateLimitResult { IsAllowed = true, RemainingTokens = 0 }; // Fail open
            }

            var allowed = (int)result[0] == 1;
            var tokens = (long)result[1];
            var retryAfterMs = (long)result[2];

            return new RateLimitResult
            {
                IsAllowed = allowed,
                RemainingTokens = tokens,
                RetryAfter = allowed ? null : TimeSpan.FromMilliseconds(retryAfterMs)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Redis rate limiter script. Failing open.");
            // In a distributed system, if Redis goes down, we prefer availability (Fail Open).
            return new RateLimitResult { IsAllowed = true, RemainingTokens = 1 };
        }
    }
}
