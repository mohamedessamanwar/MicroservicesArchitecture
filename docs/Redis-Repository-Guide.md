# Redis Repository Documentation

## Overview

This Redis repository implementation provides a comprehensive, production-ready caching solution for microservices architecture. It implements the **Repository Pattern** and supports multiple **caching strategies** with all Redis data structures.

---

## Table of Contents

1. [Architecture & Patterns](#architecture--patterns)
2. [Caching Strategies](#caching-strategies)
3. [Redis Data Structures](#redis-data-structures)
4. [TTL & Cache Invalidation](#ttl--cache-invalidation)
5. [Setup & Configuration](#setup--configuration)
6. [Usage Examples](#usage-examples)
7. [Best Practices](#best-practices)

---

## Architecture & Patterns

### Repository Pattern

The implementation follows the **Repository Pattern** to:

- Abstract Redis operations from business logic
- Provide a clean, testable interface
- Enable easy mocking for unit tests
- Centralize caching logic

**Key Components:**

```
IRedisRepository (Interface)
    ↓
RedisRepository (Implementation)
    ↓
ICacheService (Strategy Interface)
    ↓
CacheService (Strategy Implementation)
```

### Dependency Injection

All components are registered via DI for loose coupling and testability.

---

## Caching Strategies

### 1. Cache-Aside (Lazy Loading) ⭐ **Most Common**

**Pattern:** Application manages cache explicitly

**Flow:**

```
1. Check cache
2. If MISS → Load from database
3. Update cache
4. Return data
```

**When to Use:**

- ✅ Read-heavy workloads
- ✅ Data doesn't change frequently
- ✅ Cache misses are acceptable

**Pros:**

- Simple to implement
- Cache only contains requested data
- Resilient to cache failures

**Cons:**

- Cache miss penalty (extra database call)
- Possible cache stampede on popular keys

**Code Example:**

```csharp
// Using CacheService
var order = await _cacheService.GetOrSetAsync(
    key: $"order:{orderId}",
    factory: async () => await _orderRepository.GetByIdAsync(orderId),
    expiry: TimeSpan.FromMinutes(10)
);

// Using RedisRepository directly
var cachedOrder = await _redis.GetAsync<Order>($"order:{orderId}");
if (cachedOrder == null)
{
    cachedOrder = await _orderRepository.GetByIdAsync(orderId);
    await _redis.SetAsync($"order:{orderId}", cachedOrder, TimeSpan.FromMinutes(10));
}
```

---

### 2. Read-Through

**Pattern:** Cache layer automatically loads data on miss

**Flow:**

```
1. Application requests data from cache
2. Cache checks if data exists
3. If MISS → Cache loads from database automatically
4. Cache returns data
```

**When to Use:**

- ✅ Consistent data access patterns
- ✅ Want to hide cache logic from application
- ✅ Read-heavy with predictable queries

**Pros:**

- Cleaner application code
- Centralized cache logic
- Automatic cache population

**Cons:**

- Tighter coupling to cache layer
- Less control over cache behavior

**Code Example:**

```csharp
var product = await _cacheService.ReadThroughAsync(
    key: $"product:{productId}",
    dataLoader: async () => await _productRepository.GetByIdAsync(productId),
    expiry: TimeSpan.FromHours(1)
);
```

---

### 3. Write-Through

**Pattern:** Data written to cache AND database synchronously

**Flow:**

```
1. Write to cache
2. Write to database
3. Both must succeed
4. Return success
```

**When to Use:**

- ✅ Data consistency is critical
- ✅ Can tolerate write latency
- ✅ Read-heavy after writes

**Pros:**

- Cache always consistent with database
- No stale data
- Immediate cache availability

**Cons:**

- Higher write latency (two operations)
- More complex error handling
- Potential for partial failures

**Code Example:**

```csharp
var success = await _cacheService.WriteThroughAsync(
    key: $"user:{userId}",
    value: updatedUser,
    dataWriter: async (user) => await _userRepository.UpdateAsync(user),
    expiry: TimeSpan.FromMinutes(30)
);

if (!success)
{
    // Handle failure - cache was rolled back
}
```

---

### 4. Write-Behind (Write-Back)

**Pattern:** Write to cache immediately, database write is queued

**Flow:**

```
1. Write to cache immediately
2. Mark key as "dirty"
3. Background job flushes to database later
4. Return success immediately
```

**When to Use:**

- ✅ Write-heavy workloads
- ✅ Can tolerate eventual consistency
- ✅ Need high write throughput

**Pros:**

- Excellent write performance
- Reduced database load
- Batch writes possible

**Cons:**

- Risk of data loss (cache failure before flush)
- Eventual consistency
- Requires background job implementation

**Code Example:**

```csharp
// Write to cache immediately
var success = await _cacheService.WriteBehindAsync(
    key: $"session:{sessionId}",
    value: sessionData,
    expiry: TimeSpan.FromHours(2)
);

// Background job (implement separately) to flush dirty keys
// This should run periodically
var dirtyKeys = await _redis.SetMembersAsync("dirty_keys");
foreach (var key in dirtyKeys)
{
    var data = await _redis.GetAsync<SessionData>(key);
    if (data != null)
    {
        await _sessionRepository.SaveAsync(data);
        await _redis.SetRemoveAsync("dirty_keys", key);
    }
}
```

---

## Redis Data Structures

### 1. String (Key-Value)

**Use Cases:** Simple caching, session storage, counters

```csharp
// Set string
await _redis.SetStringAsync("user:1:name", "John Doe", TimeSpan.FromMinutes(10));

// Get string
var name = await _redis.GetStringAsync("user:1:name");

// Set object (auto-serialized to JSON)
await _redis.SetAsync("order:123", orderObject, TimeSpan.FromHours(1));

// Get object (auto-deserialized)
var order = await _redis.GetAsync<Order>("order:123");
```

---

### 2. List (Ordered Collection)

**Use Cases:** Activity feeds, queues, recent items

```csharp
// Push to end (right)
await _redis.ListRightPushAsync("user:1:notifications", "New order received");

// Push to beginning (left)
await _redis.ListLeftPushAsync("recent:products", productId);

// Get range (0 to 9 = first 10 items)
var recentProducts = await _redis.ListRangeAsync("recent:products", 0, 9);

// Pop from end
var lastNotification = await _redis.ListRightPopAsync("user:1:notifications");

// Get list length
var count = await _redis.ListLengthAsync("user:1:notifications");
```

**Example: Recent Activity Feed**

```csharp
// Add activity
await _redis.ListLeftPushAsync($"user:{userId}:activity", activityJson);

// Keep only last 50 activities (trim list)
var db = _redis.GetDatabase();
await db.ListTrimAsync($"user:{userId}:activity", 0, 49);

// Get recent 10 activities
var activities = await _redis.ListRangeAsync($"user:{userId}:activity", 0, 9);
```

---

### 3. Hash (Dictionary/Object Fields)

**Use Cases:** User profiles, product details, configuration

```csharp
// Set single field
await _redis.HashSetAsync("user:1", "name", "John Doe");
await _redis.HashSetAsync("user:1", "email", "john@example.com");

// Set multiple fields
await _redis.HashSetMultipleAsync("product:123", new Dictionary<string, string>
{
    ["name"] = "Laptop",
    ["price"] = "999.99",
    ["stock"] = "50"
});

// Get single field
var email = await _redis.HashGetAsync("user:1", "email");

// Get all fields
var userFields = await _redis.HashGetAllAsync("user:1");

// Check if field exists
var hasEmail = await _redis.HashExistsAsync("user:1", "email");

// Delete field
await _redis.HashDeleteAsync("user:1", "temp_field");
```

**Example: Product Cache**

```csharp
// Cache product details as hash
await _redis.HashSetMultipleAsync($"product:{productId}", new Dictionary<string, string>
{
    ["id"] = product.Id.ToString(),
    ["name"] = product.Name,
    ["price"] = product.Price.ToString(),
    ["category"] = product.Category,
    ["inStock"] = product.InStock.ToString()
});

// Set TTL on the entire hash
await _redis.KeyExpireAsync($"product:{productId}", TimeSpan.FromHours(6));
```

---

### 4. Sorted Set (Ranked Set)

**Use Cases:** Leaderboards, priority queues, time-series data

```csharp
// Add member with score
await _redis.SortedSetAddAsync("leaderboard", "player1", 1500.0);
await _redis.SortedSetAddAsync("leaderboard", "player2", 2000.0);

// Get top 10 players (highest scores)
var topPlayers = await _redis.SortedSetRangeByRankDescendingAsync("leaderboard", 0, 9);

// Get players by score range
var midRangePlayers = await _redis.SortedSetRangeByScoreAsync("leaderboard", 1000, 1500);

// Get player rank (0-based, ascending)
var rank = await _redis.SortedSetRankAsync("leaderboard", "player1");

// Get player score
var score = await _redis.SortedSetScoreAsync("leaderboard", "player1");

// Remove player
await _redis.SortedSetRemoveAsync("leaderboard", "player1");
```

**Example: Leaderboard**

```csharp
// Update player score
await _redis.SortedSetAddAsync("game:leaderboard", playerId, newScore);

// Get top 10 with scores
var db = _redis.GetDatabase();
var topPlayersWithScores = await db.SortedSetRangeByRankWithScoresAsync(
    "game:leaderboard",
    0,
    9,
    Order.Descending
);

foreach (var entry in topPlayersWithScores)
{
    Console.WriteLine($"{entry.Element}: {entry.Score} points");
}

// Get player's rank (1-based for display)
var rank = await _redis.SortedSetRankAsync("game:leaderboard", playerId);
var displayRank = rank.HasValue ? rank.Value + 1 : 0;
```

---

### 5. Set (Unique Collection)

**Use Cases:** Tags, unique visitors, relationships

```csharp
// Add member
await _redis.SetAddAsync("product:123:tags", "electronics");
await _redis.SetAddAsync("product:123:tags", "laptop");

// Add multiple members
await _redis.SetAddMultipleAsync("user:1:interests", new[] { "sports", "music", "tech" });

// Get all members
var tags = await _redis.SetMembersAsync("product:123:tags");

// Check if member exists
var hasTag = await _redis.SetContainsAsync("product:123:tags", "electronics");

// Remove member
await _redis.SetRemoveAsync("product:123:tags", "laptop");

// Get set size
var tagCount = await _redis.SetLengthAsync("product:123:tags");
```

**Example: Unique Daily Visitors**

```csharp
var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
var visitorKey = $"visitors:{today}";

// Track visitor
await _redis.SetAddAsync(visitorKey, userId);

// Set TTL to expire after 7 days
await _redis.KeyExpireAsync(visitorKey, TimeSpan.FromDays(7));

// Get unique visitor count
var uniqueVisitors = await _redis.SetLengthAsync(visitorKey);
```

---

## TTL & Cache Invalidation

### Time-To-Live (TTL)

**Set TTL on creation:**

```csharp
// String with TTL
await _redis.SetAsync("session:abc", sessionData, TimeSpan.FromMinutes(30));

// Hash with TTL (set after creation)
await _redis.HashSetAsync("user:1", "name", "John");
await _redis.KeyExpireAsync("user:1", TimeSpan.FromHours(1));
```

**Check remaining TTL:**

```csharp
var ttl = await _redis.KeyTimeToLiveAsync("session:abc");
if (ttl.HasValue)
{
    Console.WriteLine($"Expires in: {ttl.Value.TotalMinutes} minutes");
}
```

**Common TTL Patterns:**

```csharp
// Session data: 30 minutes
TimeSpan.FromMinutes(30)

// User profile: 1 hour
TimeSpan.FromHours(1)

// Product catalog: 6 hours
TimeSpan.FromHours(6)

// Static content: 24 hours
TimeSpan.FromDays(1)

// Temporary data: 5 minutes
TimeSpan.FromMinutes(5)
```

---

### Cache Invalidation Strategies

#### 1. Single Key Invalidation

```csharp
// Invalidate specific cache entry
await _redis.KeyDeleteAsync($"order:{orderId}");

// Or using CacheService
await _cacheService.InvalidateAsync($"order:{orderId}");
```

#### 2. Bulk Invalidation

```csharp
// Invalidate multiple related keys
var keys = new[]
{
    $"user:{userId}:profile",
    $"user:{userId}:settings",
    $"user:{userId}:preferences"
};

await _redis.KeyDeleteMultipleAsync(keys);
// Or
await _cacheService.InvalidateMultipleAsync(keys);
```

#### 3. Pattern-Based Invalidation ⚠️ **Use with Caution**

```csharp
// Invalidate all user-related caches
await _redis.KeyDeleteByPatternAsync($"user:{userId}:*");

// Invalidate all product caches
await _cacheService.InvalidateByPatternAsync("product:*");

// ⚠️ WARNING: This scans all keys - expensive on large datasets!
```

#### 4. Event-Based Invalidation

```csharp
// When order is updated
public async Task UpdateOrderAsync(Order order)
{
    await _orderRepository.UpdateAsync(order);

    // Invalidate related caches
    await _cacheService.InvalidateAsync($"order:{order.Id}");
    await _cacheService.InvalidateAsync($"user:{order.UserId}:orders");
}
```

#### 5. Time-Based Invalidation (TTL)

```csharp
// Set appropriate TTL based on data volatility
await _redis.SetAsync(
    "product:123",
    product,
    expiry: product.IsFlashSale
        ? TimeSpan.FromMinutes(5)  // Volatile data
        : TimeSpan.FromHours(6)     // Stable data
);
```

---

## Setup & Configuration

### 1. Update Docker Compose

Redis is already configured in your `DockerCompose.yaml`:

```yaml
redis:
  image: redis:7-alpine
  container_name: redis
  command: redis-server --appendonly yes
  ports:
    - "6379:6379"
  volumes:
    - redis_data:/data
  networks:
    - microservices-network
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 5s
    retries: 10
    start_period: 10s
```

### 2. Add Connection String

**appsettings.json:**

```json
{
  "ConnectionStrings": {
    "OrderDatabase": "Host=write-db;Port=5432;Database=OrderDb;Username=admin;Password=pass;",
    "Redis": "redis:6379,abortConnect=false"
  }
}
```

**appsettings.Development.json** (for local development):

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

### 3. Register Services

**Program.cs or DependencyInjection.cs:**

```csharp
using Micro.Shared.Caching;

// In your service configuration
builder.Services.AddRedisCaching(builder.Configuration);

// Or with custom options
builder.Services.AddRedisCaching(options =>
{
    options.EndPoints.Add("redis", 6379);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 5000;
    options.Password = "your-redis-password"; // if using password
});
```

### 4. Restore NuGet Packages

```bash
dotnet restore
```

---

## Usage Examples

### Example 1: Order Service with Cache-Aside

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICacheService cacheService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        var cacheKey = $"order:{orderId}";

        var order = await _cacheService.GetOrSetAsync(
            key: cacheKey,
            factory: async () =>
            {
                _logger.LogInformation("Loading order {OrderId} from database", orderId);
                return await _orderRepository.GetByIdAsync(orderId);
            },
            expiry: TimeSpan.FromMinutes(10)
        );

        return order;
    }

    public async Task<bool> UpdateOrderAsync(Order order)
    {
        // Update database
        var success = await _orderRepository.UpdateAsync(order);

        if (success)
        {
            // Invalidate cache
            await _cacheService.InvalidateAsync($"order:{order.Id}");
            await _cacheService.InvalidateAsync($"user:{order.UserId}:orders");
        }

        return success;
    }
}
```

### Example 2: Product Catalog with Hash

```csharp
public class ProductCacheService
{
    private readonly IRedisRepository _redis;
    private readonly IProductRepository _productRepository;

    public async Task<Product?> GetProductAsync(Guid productId)
    {
        var key = $"product:{productId}";

        // Try to get from hash
        var productHash = await _redis.HashGetAllAsync(key);

        if (productHash.Any())
        {
            return new Product
            {
                Id = Guid.Parse(productHash["id"]),
                Name = productHash["name"],
                Price = decimal.Parse(productHash["price"]),
                Stock = int.Parse(productHash["stock"])
            };
        }

        // Load from database
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;

        // Cache as hash
        await _redis.HashSetMultipleAsync(key, new Dictionary<string, string>
        {
            ["id"] = product.Id.ToString(),
            ["name"] = product.Name,
            ["price"] = product.Price.ToString(),
            ["stock"] = product.Stock.ToString()
        });

        await _redis.KeyExpireAsync(key, TimeSpan.FromHours(6));

        return product;
    }

    public async Task UpdateProductStockAsync(Guid productId, int newStock)
    {
        var key = $"product:{productId}";

        // Update database
        await _productRepository.UpdateStockAsync(productId, newStock);

        // Update cache field only (no need to reload entire product)
        await _redis.HashSetAsync(key, "stock", newStock.ToString());
    }
}
```

### Example 3: Leaderboard with Sorted Set

```csharp
public class LeaderboardService
{
    private readonly IRedisRepository _redis;
    private const string LeaderboardKey = "game:global:leaderboard";

    public async Task UpdatePlayerScoreAsync(string playerId, double score)
    {
        await _redis.SortedSetAddAsync(LeaderboardKey, playerId, score);
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(int count = 10)
    {
        var topPlayers = await _redis.SortedSetRangeByRankDescendingAsync(
            LeaderboardKey,
            0,
            count - 1
        );

        var entries = new List<LeaderboardEntry>();
        for (int i = 0; i < topPlayers.Length; i++)
        {
            var playerId = topPlayers[i];
            var score = await _redis.SortedSetScoreAsync(LeaderboardKey, playerId);

            entries.Add(new LeaderboardEntry
            {
                Rank = i + 1,
                PlayerId = playerId,
                Score = score ?? 0
            });
        }

        return entries;
    }

    public async Task<PlayerRank?> GetPlayerRankAsync(string playerId)
    {
        var rank = await _redis.SortedSetRankAsync(LeaderboardKey, playerId);
        if (!rank.HasValue) return null;

        var score = await _redis.SortedSetScoreAsync(LeaderboardKey, playerId);

        return new PlayerRank
        {
            PlayerId = playerId,
            Rank = rank.Value + 1, // Convert to 1-based
            Score = score ?? 0
        };
    }
}
```

### Example 4: Recent Activity Feed with List

```csharp
public class ActivityFeedService
{
    private readonly IRedisRepository _redis;
    private const int MaxActivities = 50;

    public async Task AddActivityAsync(Guid userId, Activity activity)
    {
        var key = $"user:{userId}:activity";
        var activityJson = JsonSerializer.Serialize(activity);

        // Add to beginning of list
        await _redis.ListLeftPushAsync(key, activityJson);

        // Trim to keep only recent activities
        var db = _redis.GetDatabase();
        await db.ListTrimAsync(key, 0, MaxActivities - 1);

        // Set TTL
        await _redis.KeyExpireAsync(key, TimeSpan.FromDays(7));
    }

    public async Task<List<Activity>> GetRecentActivitiesAsync(Guid userId, int count = 10)
    {
        var key = $"user:{userId}:activity";
        var activities = await _redis.ListRangeAsync(key, 0, count - 1);

        return activities
            .Select(json => JsonSerializer.Deserialize<Activity>(json))
            .Where(a => a != null)
            .ToList()!;
    }
}
```

---

## Best Practices

### 1. Key Naming Conventions

```csharp
// Use hierarchical naming with colons
"entity:id"                          // ✅ order:123
"entity:id:field"                    // ✅ user:456:profile
"entity:id:collection"               // ✅ user:456:orders
"namespace:entity:id"                // ✅ cache:product:789

// Avoid
"order_123"                          // ❌ Use colons, not underscores
"user-456-profile"                   // ❌ Use colons, not dashes
```

### 2. TTL Guidelines

```csharp
// Set TTL based on data volatility
- Session data: 15-30 minutes
- User profiles: 30-60 minutes
- Product catalog: 4-6 hours
- Static content: 12-24 hours
- Real-time data: 1-5 minutes
```

### 3. Cache Invalidation

```csharp
// Always invalidate on updates
public async Task UpdateOrderAsync(Order order)
{
    await _repository.UpdateAsync(order);
    await _cache.InvalidateAsync($"order:{order.Id}"); // ✅
}

// Invalidate related caches
public async Task UpdateUserEmailAsync(Guid userId, string newEmail)
{
    await _repository.UpdateEmailAsync(userId, newEmail);

    // Invalidate all related caches
    await _cache.InvalidateMultipleAsync(new[]
    {
        $"user:{userId}:profile",
        $"user:{userId}:settings",
        $"user:{userId}:session"
    });
}
```

### 4. Error Handling

```csharp
// Always have fallback to database
public async Task<Order?> GetOrderAsync(Guid orderId)
{
    try
    {
        return await _cache.GetOrSetAsync(
            $"order:{orderId}",
            async () => await _repository.GetByIdAsync(orderId),
            TimeSpan.FromMinutes(10)
        );
    }
    catch (RedisException ex)
    {
        _logger.LogError(ex, "Redis error, falling back to database");
        return await _repository.GetByIdAsync(orderId); // Fallback
    }
}
```

### 5. Avoid Cache Stampede

```csharp
// Use distributed locking for expensive operations
public async Task<Product?> GetProductAsync(Guid productId)
{
    var key = $"product:{productId}";
    var lockKey = $"lock:{key}";

    var cached = await _redis.GetAsync<Product>(key);
    if (cached != null) return cached;

    // Try to acquire lock
    var db = _redis.GetDatabase();
    var lockAcquired = await db.StringSetAsync(
        lockKey,
        "locked",
        TimeSpan.FromSeconds(10),
        When.NotExists
    );

    if (lockAcquired)
    {
        try
        {
            // Load from database
            var product = await _repository.GetByIdAsync(productId);
            if (product != null)
            {
                await _redis.SetAsync(key, product, TimeSpan.FromHours(1));
            }
            return product;
        }
        finally
        {
            await db.KeyDeleteAsync(lockKey);
        }
    }
    else
    {
        // Wait and retry
        await Task.Delay(100);
        return await GetProductAsync(productId);
    }
}
```

### 6. Monitor Cache Hit Rate

```csharp
// Log cache hits and misses
public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory) where T : class
{
    var cached = await _redis.GetAsync<T>(key);

    if (cached != null)
    {
        _logger.LogInformation("Cache HIT: {Key}", key);
        _metrics.IncrementCacheHit(); // Track metrics
        return cached;
    }

    _logger.LogInformation("Cache MISS: {Key}", key);
    _metrics.IncrementCacheMiss(); // Track metrics

    var value = await factory();
    if (value != null)
    {
        await _redis.SetAsync(key, value, TimeSpan.FromMinutes(10));
    }

    return value;
}
```

---

## Performance Tips

1. **Use pipelining for bulk operations**
2. **Set appropriate TTLs** - don't cache forever
3. **Monitor memory usage** - Redis is in-memory
4. **Use compression** for large objects
5. **Avoid storing large collections** - paginate instead
6. **Use hash for objects** instead of serializing entire object
7. **Implement circuit breaker** for Redis failures

---

## Summary

| Strategy          | Read Performance | Write Performance | Consistency | Complexity |
| ----------------- | ---------------- | ----------------- | ----------- | ---------- |
| **Cache-Aside**   | ⭐⭐⭐           | ⭐⭐⭐            | Eventual    | Low        |
| **Read-Through**  | ⭐⭐⭐           | ⭐⭐⭐            | Eventual    | Medium     |
| **Write-Through** | ⭐⭐⭐           | ⭐⭐              | Strong      | Medium     |
| **Write-Behind**  | ⭐⭐⭐           | ⭐⭐⭐⭐⭐        | Eventual    | High       |

**Recommendation:** Start with **Cache-Aside** for most use cases. It's simple, reliable, and covers 80% of scenarios.

---

## Additional Resources

- [Redis Documentation](https://redis.io/documentation)
- [StackExchange.Redis GitHub](https://github.com/StackExchange/StackExchange.Redis)
- [Caching Best Practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/caching)
