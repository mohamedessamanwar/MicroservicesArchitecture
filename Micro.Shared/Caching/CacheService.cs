using Microsoft.Extensions.Logging;

namespace Micro.Shared.Caching;

public interface ICacheService
{
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class;
    Task<T?> ReadThroughAsync<T>(
        string key,
        Func<Task<T?>> dataLoader,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class;
    Task<bool> WriteThroughAsync<T>(
        string key,
        T value,
        Func<T, Task<bool>> dataWriter,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class;
    Task<bool> WriteBehindAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class;
    Task<bool> InvalidateAsync(string key, CancellationToken cancellationToken = default);
    Task<long> InvalidateMultipleAsync(string[] keys, CancellationToken cancellationToken = default);
    Task<long> InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
public class CacheService : ICacheService
{
    private readonly IRedisRepository _redis;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IRedisRepository redis, ILogger<CacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _redis.GetAsync<T>(key, cancellationToken);

            if (cachedValue != null)
            {
                _logger.LogDebug("[Cache-Aside] Cache HIT for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("[Cache-Aside] Cache MISS for key: {Key}", key);
            var value = await factory();

            if (value == null)
            {
                _logger.LogDebug("[Cache-Aside] Factory returned null for key: {Key}", key);
                return null;
            }
            await _redis.SetAsync(key, value, expiry, cancellationToken);
            _logger.LogInformation("[Cache-Aside] Cached data for key: {Key} with TTL: {Expiry}", key, expiry?.ToString() ?? "No expiration");

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cache-Aside] Error in GetOrSetAsync for key: {Key}", key);
            try
            {
                return await factory();
            }
            catch (Exception factoryEx)
            {
                _logger.LogError(factoryEx, "[Cache-Aside] Factory also failed for key: {Key}", key);
                throw;
            }
        }
    }
    public async Task<T?> ReadThroughAsync<T>(
        string key,
        Func<Task<T?>> dataLoader,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _redis.GetAsync<T>(key, cancellationToken);

            if (cachedValue != null)
            {
                _logger.LogDebug("[Read-Through] Cache HIT for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("[Read-Through] Cache MISS for key: {Key}, loading from source", key);
            var value = await dataLoader();

            if (value != null)
            {
                await _redis.SetAsync(key, value, expiry, cancellationToken);
                _logger.LogInformation("[Read-Through] Loaded and cached data for key: {Key}", key);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Read-Through] Error for key: {Key}", key);
            throw;
        }
    }
    public async Task<bool> WriteThroughAsync<T>(
        string key,
        T value,
        Func<T, Task<bool>> dataWriter,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("[Write-Through] Writing data for key: {Key}", key);
            var cacheResult = await _redis.SetAsync(key, value, expiry, cancellationToken);

            if (!cacheResult)
            {
                _logger.LogWarning("[Write-Through] Failed to write to cache for key: {Key}", key);
                return false;
            }
            var dbResult = await dataWriter(value);

            if (!dbResult)
            {
                await _redis.KeyDeleteAsync(key, cancellationToken);
                _logger.LogWarning("[Write-Through] Database write failed, cache invalidated for key: {Key}", key);
                return false;
            }

            _logger.LogInformation("[Write-Through] Successfully wrote to cache and database for key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Write-Through] Error for key: {Key}", key);
            try
            {
                await _redis.KeyDeleteAsync(key, cancellationToken);
            }
            catch (Exception invalidateEx)
            {
                _logger.LogError(invalidateEx, "[Write-Through] Failed to invalidate cache after error for key: {Key}", key);
            }

            throw;
        }
    }
    public async Task<bool> WriteBehindAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogDebug("[Write-Behind] Writing to cache for key: {Key}", key);
            var result = await _redis.SetAsync(key, value, expiry, cancellationToken);

            if (result)
            {
                await _redis.SetAddAsync("dirty_keys", key, cancellationToken);

                _logger.LogInformation("[Write-Behind] Cached data and marked as dirty for key: {Key}", key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Write-Behind] Error for key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _redis.KeyDeleteAsync(key, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Cache invalidated for key: {Key}", key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key: {Key}", key);
            throw;
        }
    }

    public async Task<long> InvalidateMultipleAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _redis.KeyDeleteMultipleAsync(keys, cancellationToken);
            _logger.LogInformation("Bulk cache invalidation: {Count} keys invalidated", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk cache invalidation");
            throw;
        }
    }

    public async Task<long> InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _redis.KeyDeleteByPatternAsync(pattern, cancellationToken);
            _logger.LogWarning("Pattern-based cache invalidation: {Count} keys invalidated for pattern: {Pattern}", count, pattern);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in pattern-based cache invalidation for pattern: {Pattern}", pattern);
            throw;
        }
    }
}