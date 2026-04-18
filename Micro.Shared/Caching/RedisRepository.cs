using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Micro.Shared.Caching;

public class RedisRepository : IRedisRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisRepository(
        IConnectionMultiplexer redis,
        ILogger<RedisRepository> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    #region String Operations

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting string from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(value))
                return null;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing object from Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.StringSetAsync(key, value, expiry);

            if (result)
            {
                _logger.LogDebug("Successfully cached key: {Key} with TTL: {Expiry}", key, expiry?.ToString() ?? "No expiration");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting string in Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            return await SetStringAsync(key, json, expiry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing and setting object in Redis for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> TrySetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            return await _database.StringSetAsync(key, json, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to set object in Redis for key: {Key}", key);
            throw;
        }
    }

    #endregion

    #region List Operations

    public async Task<long> ListRightPushAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var length = await _database.ListRightPushAsync(key, value);
            _logger.LogDebug("Pushed value to right of list {Key}. New length: {Length}", key, length);
            return length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to right of list: {Key}", key);
            throw;
        }
    }

    public async Task<long> ListLeftPushAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var length = await _database.ListLeftPushAsync(key, value);
            _logger.LogDebug("Pushed value to left of list {Key}. New length: {Length}", key, length);
            return length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to left of list: {Key}", key);
            throw;
        }
    }

    public async Task<string?> ListRightPopAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.ListRightPopAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping from right of list: {Key}", key);
            throw;
        }
    }

    public async Task<string?> ListLeftPopAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.ListLeftPopAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping from left of list: {Key}", key);
            throw;
        }
    }

    public async Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _database.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting range from list: {Key}", key);
            throw;
        }
    }

    public async Task<long> ListLengthAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting length of list: {Key}", key);
            throw;
        }
    }

    #endregion

    #region Hash (Dictionary) Operations

    public async Task<bool> HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.HashSetAsync(key, field, value);
            _logger.LogDebug("Set hash field {Field} in {Key}. IsNew: {IsNew}", field, key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Field} in {Key}", field, key);
            throw;
        }
    }

    public async Task HashSetMultipleAsync(string key, Dictionary<string, string> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            var hashEntries = entries.Select(e => new HashEntry(e.Key, e.Value)).ToArray();
            await _database.HashSetAsync(key, hashEntries);
            _logger.LogDebug("Set {Count} hash fields in {Key}", entries.Count, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple hash fields in {Key}", key);
            throw;
        }
    }

    public async Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.HashGetAsync(key, field);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Field} from {Key}", field, key);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _database.HashGetAllAsync(key);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields from {Key}", key);
            throw;
        }
    }

    public async Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.HashDeleteAsync(key, field);
            _logger.LogDebug("Deleted hash field {Field} from {Key}. Success: {Success}", field, key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash field {Field} from {Key}", field, key);
            throw;
        }
    }

    public async Task<bool> HashExistsAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.HashExistsAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if hash field {Field} exists in {Key}", field, key);
            throw;
        }
    }

    #endregion

    #region Sorted Set (Rank Set) Operations

    public async Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SortedSetAddAsync(key, member, score);
            _logger.LogDebug("Added member to sorted set {Key} with score {Score}. IsNew: {IsNew}", key, score, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to sorted set {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _database.SortedSetRangeByRankAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted set range by rank from {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SortedSetRangeByRankDescendingAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _database.SortedSetRangeByRankAsync(key, start, stop, Order.Descending);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted set range by rank descending from {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SortedSetRangeByScoreAsync(string key, double minScore, double maxScore, CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _database.SortedSetRangeByScoreAsync(key, minScore, maxScore);
            return values.Select(v => v.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted set range by score from {Key}", key);
            throw;
        }
    }

    public async Task<long?> SortedSetRankAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SortedSetRankAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rank from sorted set {Key}", key);
            throw;
        }
    }

    public async Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SortedSetScoreAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting score from sorted set {Key}", key);
            throw;
        }
    }

    public async Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SortedSetRemoveAsync(key, member);
            _logger.LogDebug("Removed member from sorted set {Key}. Success: {Success}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from sorted set {Key}", key);
            throw;
        }
    }

    #endregion

    #region Set Operations

    public async Task<bool> SetAddAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SetAddAsync(key, member);
            _logger.LogDebug("Added member to set {Key}. IsNew: {IsNew}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to set {Key}", key);
            throw;
        }
    }

    public async Task<long> SetAddMultipleAsync(string key, string[] members, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisValues = members.Select(m => (RedisValue)m).ToArray();
            var count = await _database.SetAddAsync(key, redisValues);
            _logger.LogDebug("Added {Count} members to set {Key}", count, key);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple members to set {Key}", key);
            throw;
        }
    }

    public async Task<string[]> SetMembersAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _database.SetMembersAsync(key);
            return members.Select(m => m.ToString()).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members from set {Key}", key);
            throw;
        }
    }

    public async Task<bool> SetContainsAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SetContainsAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if set {Key} contains member", key);
            throw;
        }
    }

    public async Task<bool> SetRemoveAsync(string key, string member, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.SetRemoveAsync(key, member);
            _logger.LogDebug("Removed member from set {Key}. Success: {Success}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from set {Key}", key);
            throw;
        }
    }

    public async Task<long> SetLengthAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SetLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting length of set {Key}", key);
            throw;
        }
    }

    #endregion

    #region Key Management & Cache Invalidation

    public async Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            throw;
        }
    }

    public async Task<bool> KeyDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);

            if (result)
            {
                _logger.LogInformation("Cache invalidated for key: {Key}", key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key: {Key}", key);
            throw;
        }
    }

    public async Task<long> KeyDeleteMultipleAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var count = await _database.KeyDeleteAsync(redisKeys);

            _logger.LogInformation("Bulk cache invalidation: {Count} keys deleted", count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting multiple keys");
            throw;
        }
    }

    public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _database.KeyExpireAsync(key, expiry);

            if (result)
            {
                _logger.LogDebug("Set TTL for key {Key}: {Expiry}", key, expiry);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiration for key: {Key}", key);
            throw;
        }
    }

    public async Task<TimeSpan?> KeyTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyTimeToLiveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for key: {Key}", key);
            throw;
        }
    }

    public async Task<long> KeyDeleteByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length == 0)
            {
                _logger.LogDebug("No keys found matching pattern: {Pattern}", pattern);
                return 0;
            }

            var count = await _database.KeyDeleteAsync(keys);

            _logger.LogWarning("Pattern-based cache invalidation: {Count} keys deleted for pattern: {Pattern}", count, pattern);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    #endregion

    #region Database Operations

    public IDatabase GetDatabase()
    {
        return _database;
    }

    #endregion
}