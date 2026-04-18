namespace Micro.Shared.Http.Configuration;

public sealed class DownstreamHttpClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    // Max concurrent TCP connections per downstream host.
    public int MaxConnectionsPerServer { get; set; } = 64;

    // Rotates pooled connections to avoid stale DNS/socket affinity.
    public int PooledConnectionLifetimeSeconds { get; set; } = 300;

    // Closes idle sockets to avoid holding unused connections forever.
    public int PooledConnectionIdleTimeoutSeconds { get; set; } = 120;

    // TCP connection establishment timeout for downstream calls.
    public int ConnectTimeoutSeconds { get; set; } = 10;

    // Global request timeout at HttpClient layer as a final outer guard.
    public int OverallRequestTimeoutSeconds { get; set; } = 100;

    // Shared concurrency limit for this downstream service.
    public int MaxParallelRequests { get; set; } = 128;

    // Queue depth before requests are rejected by shared bulkhead.
    public int MaxQueuedRequests { get; set; } = 256;

    public DownstreamResiliencePipelinesOptions Pipelines { get; set; } = new();
}

public sealed class DownstreamResiliencePipelinesOptions
{
    public ResiliencePipelineSettings Read { get; set; } = new()
    {
        TimeoutSeconds = 10,
        RetryAttempts = 3,
        RetryBaseDelayMilliseconds = 200,
        EnableRetry = true,
        EnableCircuitBreaker = true,
        CircuitBreakerFailuresBeforeBreak = 5,
        CircuitBreakDurationSeconds = 30,
    };

    public ResiliencePipelineSettings Write { get; set; } = new()
    {
        TimeoutSeconds = 12,
        RetryAttempts = 0,
        RetryBaseDelayMilliseconds = 200,
        EnableRetry = false,
        EnableCircuitBreaker = true,
        CircuitBreakerFailuresBeforeBreak = 5,
        CircuitBreakDurationSeconds = 30,
    };

    public ResiliencePipelineSettings Health { get; set; } = new()
    {
        TimeoutSeconds = 2,
        RetryAttempts = 1,
        RetryBaseDelayMilliseconds = 100,
        EnableRetry = true,
        EnableCircuitBreaker = false,
        CircuitBreakerFailuresBeforeBreak = 2,
        CircuitBreakDurationSeconds = 10,
    };

    public ResiliencePipelineSettings Critical { get; set; } = new()
    {
        TimeoutSeconds = 15,
        RetryAttempts = 2,
        RetryBaseDelayMilliseconds = 300,
        EnableRetry = true,
        EnableCircuitBreaker = true,
        CircuitBreakerFailuresBeforeBreak = 3,
        CircuitBreakDurationSeconds = 60,
    };

    public ResiliencePipelineSettings NoRetry { get; set; } = new()
    {
        TimeoutSeconds = 10,
        RetryAttempts = 0,
        RetryBaseDelayMilliseconds = 200,
        EnableRetry = false,
        EnableCircuitBreaker = true,
        CircuitBreakerFailuresBeforeBreak = 5,
        CircuitBreakDurationSeconds = 30,
    };
}

public sealed class ResiliencePipelineSettings
{
    // Per-attempt timeout for operations using this pipeline.
    public int TimeoutSeconds { get; set; } = 10;

    // Number of retry attempts for transient failures.
    public int RetryAttempts { get; set; } = 3;

    // Base delay used by exponential backoff retry.
    public int RetryBaseDelayMilliseconds { get; set; } = 200;

    public bool EnableRetry { get; set; } = true;
    public bool EnableCircuitBreaker { get; set; } = true;

    // Consecutive handled failures before opening the circuit.
    public int CircuitBreakerFailuresBeforeBreak { get; set; } = 5;

    // Duration that the circuit remains open before half-open probe.
    public int CircuitBreakDurationSeconds { get; set; } = 30;

    // True = use client shared bulkhead, false = use override values below.
    public bool UseSharedBulkhead { get; set; } = true;

    public int? MaxParallelRequestsOverride { get; set; }
    public int? MaxQueuedRequestsOverride { get; set; }
}