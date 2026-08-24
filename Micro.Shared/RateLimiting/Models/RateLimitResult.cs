namespace Micro.Shared.RateLimiting.Models;

public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public long RemainingTokens { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}
