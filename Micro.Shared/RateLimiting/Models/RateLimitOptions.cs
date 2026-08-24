namespace Micro.Shared.RateLimiting.Models;

public class RateLimitOptions
{
    public int Capacity { get; set; } = 100;
    public int TokensPerPeriod { get; set; } = 10;
    public int PeriodSeconds { get; set; } = 1;
    public int TtlSeconds { get; set; } = 60;
    public string[] KnownProxies { get; set; } = Array.Empty<string>();
}
