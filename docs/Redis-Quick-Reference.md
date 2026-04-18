# Redis Repository - Quick Reference

## Setup (One-Time)

### 1. Register Services in Program.cs

```csharp
using Micro.Shared.Caching;

builder.Services.AddRedisCaching(builder.Configuration);
```

### 2. Add Connection String to appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "redis:6379,abortConnect=false"
  }
}
```

### 3. Inject in Your Service

```csharp
public class OrderService
{
    private readonly ICacheService _cache;
    private readonly IRedisRepository _redis;

    public OrderService(ICacheService cache, IRedisRepository redis)
    {
        _cache = cache;
        _redis = redis;
    }
}
```

---

## Common Patterns

### Cache-Aside (Most Common) ⭐

```csharp
// Simple cache-aside
var order = await _cache.GetOrSetAsync(
    key: $"order:{orderId}",
    factory: async () => await _db.GetOrderAsync(orderId),
    expiry: TimeSpan.FromMinutes(10)
);
```

### Update with Invalidation

```csharp
// Update database
await _db.UpdateOrderAsync(order);

// Invalidate cache
await _cache.InvalidateAsync($"order:{order.Id}");
```

### Write-Through (Strong Consistency)

```csharp
var success = await _cache.WriteThroughAsync(
    key: $"user:{userId}",
    value: user,
    dataWriter: async (u) => await _db.UpdateUserAsync(u),
    expiry: TimeSpan.FromMinutes(30)
);
```

---

## Data Structures Cheat Sheet

### String (Simple Key-Value)

```csharp
// Set
await _redis.SetAsync("key", object, TimeSpan.FromMinutes(10));

// Get
var obj = await _redis.GetAsync<MyType>("key");

// Delete
await _redis.KeyDeleteAsync("key");
```

### List (Ordered, Duplicates Allowed)

```csharp
// Add to end
await _redis.ListRightPushAsync("notifications", message);

// Get recent 10
var recent = await _redis.ListRangeAsync("notifications", 0, 9);

// Get count
var count = await _redis.ListLengthAsync("notifications");
```

### Hash (Object with Fields)

```csharp
// Set fields
await _redis.HashSetMultipleAsync("user:1", new Dictionary<string, string>
{
    ["name"] = "John",
    ["email"] = "john@example.com"
});

// Get field
var email = await _redis.HashGetAsync("user:1", "email");

// Get all
var user = await _redis.HashGetAllAsync("user:1");
```

### Sorted Set (Ranked, Unique)

```csharp
// Add with score
await _redis.SortedSetAddAsync("leaderboard", playerId, score);

// Get top 10
var top = await _redis.SortedSetRangeByRankDescendingAsync("leaderboard", 0, 9);

// Get rank
var rank = await _redis.SortedSetRankAsync("leaderboard", playerId);
```

### Set (Unique, Unordered)

```csharp
// Add
await _redis.SetAddAsync("tags", "electronics");

// Check exists
var exists = await _redis.SetContainsAsync("tags", "electronics");

// Get all
var allTags = await _redis.SetMembersAsync("tags");
```

---

## TTL & Expiration

```csharp
// Set with TTL
await _redis.SetAsync("key", value, TimeSpan.FromMinutes(30));

// Add TTL to existing key
await _redis.KeyExpireAsync("key", TimeSpan.FromHours(1));

// Check remaining TTL
var ttl = await _redis.KeyTimeToLiveAsync("key");
```

---

## Cache Invalidation

```csharp
// Single key
await _cache.InvalidateAsync("order:123");

// Multiple keys
await _cache.InvalidateMultipleAsync(new[] { "key1", "key2", "key3" });

// Pattern (⚠️ expensive!)
await _cache.InvalidateByPatternAsync("user:123:*");
```

---

## Key Naming Convention

```
entity:id                    → order:123
entity:id:field             → user:456:profile
entity:id:collection        → user:456:orders
namespace:entity:id         → cache:product:789
```

---

## Recommended TTLs

| Data Type      | TTL         | Reason               |
| -------------- | ----------- | -------------------- |
| Session        | 15-30 min   | User activity        |
| User Profile   | 30-60 min   | Changes infrequently |
| Product        | 4-6 hours   | Inventory updates    |
| Static Content | 12-24 hours | Rarely changes       |
| Real-time      | 1-5 min     | Frequently updated   |

---

## Error Handling Pattern

```csharp
try
{
    return await _cache.GetOrSetAsync(key, factory, expiry);
}
catch (RedisException ex)
{
    _logger.LogError(ex, "Redis error, using fallback");
    return await factory(); // Fallback to database
}
```

---

## Complete Example

```csharp
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repository,
        ICacheService cache,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    // GET with caching
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _cache.GetOrSetAsync(
            key: $"order:{orderId}",
            factory: async () =>
            {
                _logger.LogInformation("Loading order {OrderId} from DB", orderId);
                return await _repository.GetByIdAsync(orderId);
            },
            expiry: TimeSpan.FromMinutes(10)
        );
    }

    // UPDATE with invalidation
    public async Task<bool> UpdateOrderAsync(Order order)
    {
        var success = await _repository.UpdateAsync(order);

        if (success)
        {
            // Invalidate related caches
            await _cache.InvalidateMultipleAsync(new[]
            {
                $"order:{order.Id}",
                $"user:{order.UserId}:orders"
            });
        }

        return success;
    }

    // CREATE (no caching needed)
    public async Task<Order> CreateOrderAsync(Order order)
    {
        return await _repository.CreateAsync(order);
    }

    // DELETE with invalidation
    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order == null) return false;

        var success = await _repository.DeleteAsync(orderId);

        if (success)
        {
            await _cache.InvalidateAsync($"order:{orderId}");
        }

        return success;
    }
}
```

---

## Testing

```csharp
// Mock ICacheService for unit tests
var mockCache = new Mock<ICacheService>();
mockCache
    .Setup(c => c.GetOrSetAsync(
        It.IsAny<string>(),
        It.IsAny<Func<Task<Order?>>>(),
        It.IsAny<TimeSpan?>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync((string key, Func<Task<Order?>> factory, TimeSpan? expiry, CancellationToken ct)
        => factory());

var service = new OrderService(mockRepository.Object, mockCache.Object, mockLogger.Object);
```

---

## Monitoring

Track these metrics:

- **Cache Hit Rate**: hits / (hits + misses)
- **Cache Miss Rate**: misses / (hits + misses)
- **Average Response Time**: with vs without cache
- **Memory Usage**: Redis memory consumption
- **Eviction Rate**: keys evicted due to memory pressure

Target: **80%+ cache hit rate** for read-heavy workloads

---

## When NOT to Cache

❌ Don't cache:

- Highly volatile data (changes every second)
- Large objects (> 1MB) - consider compression
- Sensitive data (passwords, tokens)
- Data that's cheaper to compute than to cache
- Write-heavy data with rare reads

✅ Do cache:

- Read-heavy data
- Expensive database queries
- API responses
- Session data
- Computed results
- Frequently accessed data

---

## Troubleshooting

### Redis Connection Failed

```csharp
// Check connection string
"redis:6379,abortConnect=false"

// Verify Redis is running
docker ps | grep redis

// Test connection
docker exec -it redis redis-cli ping
```

### High Memory Usage

```csharp
// Set TTL on all keys
await _redis.KeyExpireAsync(key, TimeSpan.FromHours(1));

// Use Redis eviction policy (in docker-compose)
command: redis-server --maxmemory 256mb --maxmemory-policy allkeys-lru
```

### Cache Stampede

```csharp
// Use distributed locking
var lockKey = $"lock:{cacheKey}";
var db = _redis.GetDatabase();
var locked = await db.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(10), When.NotExists);

if (locked)
{
    try
    {
        // Load and cache data
    }
    finally
    {
        await db.KeyDeleteAsync(lockKey);
    }
}
```

---

## Next Steps

1. ✅ Add `builder.Services.AddRedisCaching(builder.Configuration);` to Program.cs
2. ✅ Add Redis connection string to appsettings.json
3. ✅ Inject `ICacheService` or `IRedisRepository` in your services
4. ✅ Start with Cache-Aside pattern
5. ✅ Monitor cache hit rate
6. ✅ Adjust TTLs based on data volatility

**Full Documentation**: See `Redis-Repository-Guide.md`
