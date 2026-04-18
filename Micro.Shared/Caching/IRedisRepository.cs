using StackExchange.Redis;

namespace Micro.Shared.Caching;

public interface IRedisRepository
{
    #region String Operations
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> TrySetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;

    #endregion

    #region List Operations
    Task<long> ListRightPushAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<long> ListLeftPushAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<string?> ListRightPopAsync(string key, CancellationToken cancellationToken = default);
    Task<string?> ListLeftPopAsync(string key, CancellationToken cancellationToken = default);
    Task<string[]> ListRangeAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default);
    Task<long> ListLengthAsync(string key, CancellationToken cancellationToken = default);

    #endregion

    #region Hash (Dictionary) Operations
    Task<bool> HashSetAsync(string key, string field, string value, CancellationToken cancellationToken = default);
    Task HashSetMultipleAsync(string key, Dictionary<string, string> entries, CancellationToken cancellationToken = default);
    Task<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default);
    Task<bool> HashExistsAsync(string key, string field, CancellationToken cancellationToken = default);

    #endregion

    #region Sorted Set (Rank Set) Operations
    Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken cancellationToken = default);
    Task<string[]> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default);
    Task<string[]> SortedSetRangeByRankDescendingAsync(string key, long start = 0, long stop = -1, CancellationToken cancellationToken = default);
    Task<string[]> SortedSetRangeByScoreAsync(string key, double minScore, double maxScore, CancellationToken cancellationToken = default);
    Task<long?> SortedSetRankAsync(string key, string member, CancellationToken cancellationToken = default);
    Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken cancellationToken = default);
    Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken cancellationToken = default);

    #endregion

    #region Set Operations
    Task<bool> SetAddAsync(string key, string member, CancellationToken cancellationToken = default);
    Task<long> SetAddMultipleAsync(string key, string[] members, CancellationToken cancellationToken = default);
    Task<string[]> SetMembersAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> SetContainsAsync(string key, string member, CancellationToken cancellationToken = default);
    Task<bool> SetRemoveAsync(string key, string member, CancellationToken cancellationToken = default);
    Task<long> SetLengthAsync(string key, CancellationToken cancellationToken = default);

    #endregion

    #region Key Management & Cache Invalidation
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> KeyDeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<long> KeyDeleteMultipleAsync(string[] keys, CancellationToken cancellationToken = default);
    Task<bool> KeyExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<TimeSpan?> KeyTimeToLiveAsync(string key, CancellationToken cancellationToken = default);
    Task<long> KeyDeleteByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    #endregion

    #region Database Operations
    IDatabase GetDatabase();

    #endregion
}