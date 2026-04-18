# Redis Caching Implementation Summary

## 📋 What Was Implemented

### 1. **Core Components**

#### **IRedisRepository** - Low-level Redis operations

- ✅ String operations (Get/Set with JSON serialization)
- ✅ List operations (Push/Pop/Range)
- ✅ Hash operations (Set/Get fields, Get all)
- ✅ Sorted Set operations (Add/Range by rank/score, Get rank/score)
- ✅ Set operations (Add/Remove/Contains members)
- ✅ Key management (Exists/Delete/Expire/TTL)
- ✅ Pattern-based deletion

#### **ICacheService** - High-level caching strategies

- ✅ Cache-Aside (Lazy Loading)
- ✅ Read-Through
- ✅ Write-Through
- ✅ Write-Behind (Write-Back)
- ✅ Cache invalidation (single/multiple/pattern)

#### **RedisCachingExtensions** - Dependency Injection

- ✅ Easy service registration
- ✅ Configurable connection options
- ✅ Connection pooling and retry logic

---

## 🎯 Caching Strategies Used

### **1. Cache-Aside (Lazy Loading)** ⭐ **Recommended for most cases**

**When to Use:**

- Read-heavy workloads
- Data doesn't change frequently
- Can tolerate occasional cache misses

**Pattern:**

```
Application → Check Cache → If Miss → Load from DB → Update Cache → Return
```

**Implementation:**

```csharp
var order = await _cacheService.GetOrSetAsync(
    key: $"order:{orderId}",
    factory: async () => await _repository.GetByIdAsync(orderId),
    expiry: TimeSpan.FromMinutes(10)
);
```

**Pros:**

- ✅ Simple to implement
- ✅ Cache only contains requested data
- ✅ Resilient to cache failures

**Cons:**

- ❌ Cache miss penalty (extra DB call)
- ❌ Possible cache stampede

---

### **2. Read-Through**

**When to Use:**

- Want to hide cache logic from application
- Consistent data access patterns
- Centralized cache management

**Pattern:**

```
Application → Request Data → Cache Layer → (If Miss) Load from DB → Return
```

**Implementation:**

```csharp
var product = await _cacheService.ReadThroughAsync(
    key: $"product:{productId}",
    dataLoader: async () => await _repository.GetByIdAsync(productId),
    expiry: TimeSpan.FromHours(1)
);
```

**Pros:**

- ✅ Cleaner application code
- ✅ Centralized cache logic

**Cons:**

- ❌ Tighter coupling to cache layer

---

### **3. Write-Through**

**When to Use:**

- Data consistency is critical
- Can tolerate write latency
- Read-heavy after writes

**Pattern:**

```
Application → Write to Cache → Write to DB → Both Must Succeed
```

**Implementation:**

```csharp
var success = await _cacheService.WriteThroughAsync(
    key: $"user:{userId}",
    value: user,
    dataWriter: async (u) => await _repository.UpdateAsync(u),
    expiry: TimeSpan.FromMinutes(30)
);
```

**Pros:**

- ✅ Cache always consistent with DB
- ✅ No stale data

**Cons:**

- ❌ Higher write latency
- ❌ More complex error handling

---

### **4. Write-Behind (Write-Back)**

**When to Use:**

- Write-heavy workloads
- Can tolerate eventual consistency
- Need high write throughput

**Pattern:**

```
Application → Write to Cache → Mark as Dirty → Background Job Flushes to DB
```

**Implementation:**

```csharp
await _cacheService.WriteBehindAsync(
    key: $"session:{sessionId}",
    value: sessionData,
    expiry: TimeSpan.FromHours(2)
);

// Background job flushes dirty keys
var dirtyKeys = await _redis.SetMembersAsync("dirty_keys");
foreach (var key in dirtyKeys)
{
    var data = await _redis.GetAsync<SessionData>(key);
    await _repository.SaveAsync(data);
    await _redis.SetRemoveAsync("dirty_keys", key);
}
```

**Pros:**

- ✅ Excellent write performance
- ✅ Reduced database load

**Cons:**

- ❌ Risk of data loss
- ❌ Eventual consistency
- ❌ Requires background job

---

## 🗂️ Redis Data Structures Used

### **1. String (Key-Value)**

**Use Case:** Simple caching, session storage

```csharp
// Cache entire object
await _redis.SetAsync("order:123", orderObject, TimeSpan.FromMinutes(10));
var order = await _redis.GetAsync<Order>("order:123");
```

**When to Use:**

- ✅ Caching entire objects
- ✅ Session data
- ✅ Simple key-value pairs

---

### **2. List (Ordered Collection)**

**Use Case:** Recent items, activity feeds, queues

```csharp
// Add to list
await _redis.ListRightPushAsync("user:1:notifications", notification);

// Get recent 10
var recent = await _redis.ListRangeAsync("user:1:notifications", 0, 9);
```

**When to Use:**

- ✅ Activity feeds
- ✅ Recent items
- ✅ Message queues
- ✅ User notifications

---

### **3. Hash (Dictionary/Object Fields)**

**Use Case:** User profiles, product details, partial updates

```csharp
// Cache product as hash
await _redis.HashSetMultipleAsync("product:123", new Dictionary<string, string>
{
    ["name"] = "Laptop",
    ["price"] = "999.99",
    ["stock"] = "50"
});

// Update only stock field
await _redis.HashSetAsync("product:123", "stock", "45");
```

**When to Use:**

- ✅ Objects with many fields
- ✅ Partial updates needed
- ✅ User profiles
- ✅ Product catalogs

**Advantages:**

- Memory efficient for objects
- Can update individual fields
- No need to deserialize entire object

---

### **4. Sorted Set (Ranked Set)**

**Use Case:** Leaderboards, priority queues, time-series

```csharp
// Add player score
await _redis.SortedSetAddAsync("leaderboard", playerId, score);

// Get top 10
var top = await _redis.SortedSetRangeByRankDescendingAsync("leaderboard", 0, 9);

// Get player rank
var rank = await _redis.SortedSetRankAsync("leaderboard", playerId);
```

**When to Use:**

- ✅ Leaderboards
- ✅ Top N queries
- ✅ Priority queues
- ✅ Time-series data (score = timestamp)

---

### **5. Set (Unique Collection)**

**Use Case:** Tags, unique visitors, relationships

```csharp
// Add tags
await _redis.SetAddMultipleAsync("product:123:tags", new[] { "electronics", "laptop" });

// Check if tag exists
var hasTag = await _redis.SetContainsAsync("product:123:tags", "electronics");

// Get all tags
var tags = await _redis.SetMembersAsync("product:123:tags");
```

**When to Use:**

- ✅ Tags/categories
- ✅ Unique visitors tracking
- ✅ Relationships (followers/following)
- ✅ Deduplication

---

## ⏱️ TTL (Time-To-Live) Strategy

### **Recommended TTLs by Data Type**

| Data Type           | TTL           | Reason                         |
| ------------------- | ------------- | ------------------------------ |
| **Session Data**    | 15-30 minutes | User activity based            |
| **User Profile**    | 30-60 minutes | Changes infrequently           |
| **Product Catalog** | 4-6 hours     | Inventory updates periodically |
| **Static Content**  | 12-24 hours   | Rarely changes                 |
| **Real-time Data**  | 1-5 minutes   | Frequently updated             |
| **Search Results**  | 5-10 minutes  | Can be stale                   |
| **API Responses**   | 1-5 minutes   | External data                  |

### **Implementation**

```csharp
// Set TTL on creation
await _redis.SetAsync("key", value, TimeSpan.FromMinutes(10));

// Add TTL to existing key
await _redis.KeyExpireAsync("key", TimeSpan.FromHours(1));

// Check remaining TTL
var ttl = await _redis.KeyTimeToLiveAsync("key");
```

---

## 🔄 Cache Invalidation Patterns

### **1. Single Key Invalidation**

```csharp
// When order is updated
await _cache.InvalidateAsync($"order:{orderId}");
```

**When to Use:**

- ✅ Single entity updated
- ✅ Simple invalidation

---

### **2. Bulk Invalidation**

```csharp
// When user profile is updated
await _cache.InvalidateMultipleAsync(new[]
{
    $"user:{userId}:profile",
    $"user:{userId}:settings",
    $"user:{userId}:orders"
});
```

**When to Use:**

- ✅ Multiple related caches need invalidation
- ✅ Efficient batch operations

---

### **3. Pattern-Based Invalidation** ⚠️ **Use with Caution**

```csharp
// Invalidate all user-related caches
await _cache.InvalidateByPatternAsync($"user:{userId}:*");
```

**When to Use:**

- ⚠️ Only when necessary (expensive operation)
- ⚠️ Scans all keys in Redis
- ✅ User logout/deletion
- ✅ Major data changes

---

### **4. Event-Based Invalidation**

```csharp
public async Task UpdateOrderAsync(Order order)
{
    // Update database
    await _repository.UpdateAsync(order);

    // Invalidate related caches
    await _cache.InvalidateAsync($"order:{order.Id}");
    await _cache.InvalidateAsync($"user:{order.UserId}:orders");
}
```

**When to Use:**

- ✅ Most common pattern
- ✅ Explicit control over invalidation
- ✅ Predictable behavior

---

### **5. Time-Based Invalidation (TTL)**

```csharp
// Volatile data - short TTL
await _redis.SetAsync("flash-sale", data, TimeSpan.FromMinutes(5));

// Stable data - longer TTL
await _redis.SetAsync("product-catalog", data, TimeSpan.FromHours(6));
```

**When to Use:**

- ✅ Data has natural expiration
- ✅ Acceptable to serve slightly stale data
- ✅ Reduces invalidation complexity

---

## 🏗️ Repository Pattern

### **Why Repository Pattern?**

1. **Abstraction**: Hide Redis implementation details
2. **Testability**: Easy to mock for unit tests
3. **Flexibility**: Can swap Redis for another cache
4. **Centralization**: Single place for cache logic

### **Architecture**

```
Controller/Handler
    ↓
Service Layer (CachedOrderService)
    ↓
ICacheService (High-level strategies)
    ↓
IRedisRepository (Low-level Redis operations)
    ↓
StackExchange.Redis (Redis client)
```

### **Benefits**

- ✅ Clean separation of concerns
- ✅ Easy to test (mock interfaces)
- ✅ Consistent caching across services
- ✅ Centralized error handling
- ✅ Logging and monitoring

---

## 📊 Monitoring & Best Practices

### **Key Metrics to Track**

1. **Cache Hit Rate**: `hits / (hits + misses)`

   - Target: **80%+** for read-heavy workloads

2. **Cache Miss Rate**: `misses / (hits + misses)`

   - Monitor for cache stampede

3. **Average Response Time**

   - With cache vs without cache

4. **Memory Usage**

   - Redis memory consumption
   - Set max memory and eviction policy

5. **Eviction Rate**
   - Keys evicted due to memory pressure

### **Best Practices**

#### **1. Key Naming**

```csharp
✅ "entity:id"              → "order:123"
✅ "entity:id:field"        → "user:456:profile"
✅ "entity:id:collection"   → "user:456:orders"
❌ "order_123"              → Use colons, not underscores
```

#### **2. Error Handling**

```csharp
try
{
    return await _cache.GetOrSetAsync(key, factory, expiry);
}
catch (RedisException ex)
{
    _logger.LogError(ex, "Redis error, using fallback");
    return await factory(); // Always have fallback
}
```

#### **3. Avoid Cache Stampede**

```csharp
// Use distributed locking for expensive operations
var lockKey = $"lock:{cacheKey}";
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

#### **4. Set Appropriate TTLs**

```csharp
// Based on data volatility
- Volatile data: Short TTL (1-5 min)
- Stable data: Long TTL (1-6 hours)
- Static data: Very long TTL (12-24 hours)
```

#### **5. Invalidate on Updates**

```csharp
// Always invalidate after updates
await _repository.UpdateAsync(entity);
await _cache.InvalidateAsync($"entity:{entity.Id}");
```

---

## 🚀 How to Use

### **1. Register Services**

In `Program.cs` or `DependencyInjection.cs`:

```csharp
using Micro.Shared.Caching;

builder.Services.AddRedisCaching(builder.Configuration);
```

### **2. Add Connection String**

In `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "redis:6379,abortConnect=false"
  }
}
```

### **3. Inject in Your Service**

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

### **4. Use Caching**

```csharp
// Simple cache-aside
var order = await _cache.GetOrSetAsync(
    $"order:{orderId}",
    async () => await _db.GetOrderAsync(orderId),
    TimeSpan.FromMinutes(10)
);

// Update with invalidation
await _db.UpdateOrderAsync(order);
await _cache.InvalidateAsync($"order:{order.Id}");
```

---

## 📁 Files Created

1. **Micro.Shared/Caching/IRedisRepository.cs** - Redis operations interface
2. **Micro.Shared/Caching/RedisRepository.cs** - Redis operations implementation
3. **Micro.Shared/Caching/ICacheService.cs** - Caching strategies interface
4. **Micro.Shared/Caching/CacheService.cs** - Caching strategies implementation
5. **Micro.Shared/Caching/RedisCachingExtensions.cs** - DI registration
6. **OrderService.Application/Services/CachedOrderService.cs** - Example usage
7. **docs/Redis-Repository-Guide.md** - Comprehensive documentation
8. **docs/Redis-Quick-Reference.md** - Quick reference guide
9. **docs/Redis-Implementation-Summary.md** - This file

---

## ✅ Next Steps

1. ✅ Restore NuGet packages: `dotnet restore`
2. ✅ Add Redis caching to your services
3. ✅ Start with Cache-Aside pattern
4. ✅ Monitor cache hit rate
5. ✅ Adjust TTLs based on your data
6. ✅ Implement proper invalidation on updates

---

## 🎓 Summary

**What You Got:**

- ✅ Complete Redis repository with all data structures
- ✅ 4 caching strategies (Cache-Aside, Read-Through, Write-Through, Write-Behind)
- ✅ TTL management and cache invalidation
- ✅ Repository pattern for clean architecture
- ✅ Comprehensive documentation and examples
- ✅ Production-ready error handling and logging
- ✅ Docker Compose already configured with Redis

**Recommended Approach:**

1. Start with **Cache-Aside** (simplest, most common)
2. Use **String** data structure for most caching
3. Use **Hash** for objects with many fields
4. Use **Sorted Set** for leaderboards/rankings
5. Set appropriate **TTLs** based on data volatility
6. Always **invalidate** on updates
7. Monitor **cache hit rate** (target 80%+)

**Remember:**

- Cache is a performance optimization, not a replacement for database
- Always have fallback to database if cache fails
- Don't cache sensitive data (passwords, tokens)
- Monitor memory usage
- Set max memory and eviction policy in production

---

**Happy Caching! 🚀**
