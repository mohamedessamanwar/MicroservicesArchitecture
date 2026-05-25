namespace OrderService.Api.Runtime;

public sealed class RuntimeSummaryDto
{
    public string Status { get; init; } = "OK";
    public int ProcessId { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public DateTime StartTimeUtc { get; init; }
    public double UptimeSeconds { get; init; }
    public string Framework { get; init; } = string.Empty;
    public string OS { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; }
}

public sealed class ProcessMetricsDto
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public DateTime StartTimeUtc { get; init; }
    public double UptimeSeconds { get; init; }
    public int ThreadCount { get; init; }
    public int HandleCount { get; init; }
    public double CpuTotalTimeMs { get; init; }
    public double UserProcessorTimeMs { get; init; }
    public double PrivilegedProcessorTimeMs { get; init; }
}

public sealed class MemoryMetricsDto
{
    public double WorkingSetMb { get; init; }
    public double PrivateMemoryMb { get; init; }
    public double VirtualMemoryMb { get; init; }
    public double ManagedHeapMb { get; init; }
    public double HeapSizeMb { get; init; }
    public double FragmentedMb { get; init; }
    public double MemoryLoadMb { get; init; }
    public double AvailableMemoryMb { get; init; }
    public double TotalCommittedBytesMb { get; init; }
    public long PinnedObjectsCount { get; init; }
    public long FinalizationPendingCount { get; init; }
}

public sealed class GcMetricsDto
{
    public bool IsServerGc { get; init; }
    public string LatencyMode { get; init; } = string.Empty;
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public double PauseTimePercentage { get; init; }
    public double HeapSizeMb { get; init; }
    public double FragmentedMb { get; init; }
    public double MemoryLoadMb { get; init; }
    public double HighMemoryLoadThresholdMb { get; init; }
    public double TotalAvailableMemoryMb { get; init; }
    public bool Compacted { get; init; }
    public bool Concurrent { get; init; }
    public int Generation { get; init; }
    public long Index { get; init; }
}

public sealed class ThreadPoolMetricsDto
{
    public int UsedWorkerThreads { get; init; }
    public int AvailableWorkerThreads { get; init; }
    public int MaxWorkerThreads { get; init; }
    public int MinWorkerThreads { get; init; }
    public int UsedCompletionPortThreads { get; init; }
    public int AvailableCompletionPortThreads { get; init; }
    public int MaxCompletionPortThreads { get; init; }
    public int MinCompletionPortThreads { get; init; }
    public long? PendingWorkItemCount { get; init; }
    public long? CompletedWorkItemCount { get; init; }
    public int? ThreadCount { get; init; }
}

public sealed class CpuMetricsDto
{
    public int ProcessorCount { get; init; }
    public double TotalProcessorTimeMs { get; init; }
    public double UserProcessorTimeMs { get; init; }
    public double PrivilegedProcessorTimeMs { get; init; }
    public double? CpuUsagePercent { get; init; }
    public string Note { get; init; } = string.Empty;
}

public sealed class RequestMetricsDto
{
    public long TotalRequests { get; init; }
    public long ActiveRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public Dictionary<int, long> StatusCodeCounts { get; init; } = new();
    public double? RequestsPerMinute { get; init; }
}

public sealed class ExceptionMetricsDto
{
    public long TotalExceptions { get; init; }
    public DateTime? LastExceptionTimestampUtc { get; init; }
    public string LastExceptionType { get; init; } = string.Empty;
    public string LastExceptionMessage { get; init; } = string.Empty;
    public Dictionary<string, long> ExceptionsByType { get; init; } = new();
}

public sealed class LatencyMetricsDto
{
    public long TotalMeasuredRequests { get; init; }
    public double AverageLatencyMs { get; init; }
    public double MinLatencyMs { get; init; }
    public double MaxLatencyMs { get; init; }
    public double P50LatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public double P99LatencyMs { get; init; }
    public int RollingWindowSize { get; init; }
}

public sealed class AllocationMetricsDto
{
    public long TotalAllocatedBytes { get; init; }
    public double TotalAllocatedMb { get; init; }
    public double ManagedHeapMb { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}

public sealed class RuntimeInfoDto
{
    public string Framework { get; init; } = string.Empty;
    public string OS { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public string OSArchitecture { get; init; } = string.Empty;
    public int ProcessorCount { get; init; }
    public bool Is64BitProcess { get; init; }
    public string RuntimeIdentifier { get; init; } = string.Empty;
}

public sealed class HealthRiskDto
{
    public string Status { get; init; } = "Healthy";
    public List<string> Warnings { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

public sealed class RuntimeMetricsDto
{
    public string Status { get; init; } = "OK";
    public ProcessMetricsDto Process { get; init; } = new();
    public MemoryMetricsDto Memory { get; init; } = new();
    public GcMetricsDto Gc { get; init; } = new();
    public ThreadPoolMetricsDto ThreadPool { get; init; } = new();
    public CpuMetricsDto Cpu { get; init; } = new();
    public RequestMetricsDto Requests { get; init; } = new();
    public ExceptionMetricsDto Exceptions { get; init; } = new();
    public LatencyMetricsDto Latency { get; init; } = new();
    public AllocationMetricsDto Allocations { get; init; } = new();
    public RuntimeInfoDto Runtime { get; init; } = new();
    public HealthRiskDto HealthRisk { get; init; } = new();
    public DateTime TimestampUtc { get; init; }
}
