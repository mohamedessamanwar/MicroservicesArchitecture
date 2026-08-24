using Micro.Shared.RateLimiting.Models;

namespace Micro.Shared.RateLimiting;

public interface IRateLimiter
{
    Task<RateLimitResult> IsAllowedAsync(string key, RateLimitOptions options, int cost = 1);
}
