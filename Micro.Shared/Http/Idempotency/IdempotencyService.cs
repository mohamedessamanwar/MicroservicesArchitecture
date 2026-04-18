using Micro.Shared.Caching;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Micro.Shared.Http.Idempotency;

public class IdempotencyService
{
    private readonly IRedisRepository _redisRepository;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(IRedisRepository redisRepository, ILogger<IdempotencyService> logger)
    {
        _redisRepository = redisRepository;
        _logger = logger;
    }

    public async Task<IdempotencyResult?> GetResultAsync(string key, CancellationToken ct = default)
    {
        return await _redisRepository.GetAsync<IdempotencyResult>($"idempotency:{key}", ct);
    }

    public async Task SetResultAsync(string key, IdempotencyResult result, TimeSpan expiry, CancellationToken ct = default)
    {
        await _redisRepository.SetAsync($"idempotency:{key}", result, expiry, ct);
    }

    public async Task<bool> TrySetProcessingAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var cacheKey = $"idempotency:{key}";

        var existing = await _redisRepository.GetAsync<IdempotencyResult>(cacheKey, ct);
        if (existing != null) return false;

        var processingResult = new IdempotencyResult
        {
            IsProcessing = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _redisRepository.SetAsync(cacheKey, processingResult, expiry, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _redisRepository.KeyDeleteAsync($"idempotency:{key}", ct);
    }
}