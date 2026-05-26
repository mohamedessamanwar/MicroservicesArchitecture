namespace Micro.Shared.MetricServices.Models;

public sealed class CpuMetric
{
    public DateTime CapturedAtUtc { get; set; }
    public double UsagePercent { get; set; }
    public double DeltaCpuMs { get; set; }
    public double DeltaWallMs { get; set; }
    public int LogicalProcessorCount { get; set; }
}

public sealed class RamMetric
{
    public DateTime CapturedAtUtc { get; set; }
    public double WorkingSetMb { get; set; }
    public double PrivateMemoryMb { get; set; }
    public double ManagedHeapMb { get; set; }
    public double GcHeapMb { get; set; }
    public double GcMemoryLoadMb { get; set; }
}

public sealed class GcMemoryInfoMetric
{
    public double HeapSizeMb { get; set; }
    public double MemoryLoadMb { get; set; }
    public double TotalAvailableMemoryMb { get; set; }
    public double HighMemoryLoadThresholdMb { get; set; }
    public double FragmentedMb { get; set; }
}

public sealed class GarbageCollectorMetric
{
    public DateTime CapturedAtUtc { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public int Gen0Delta { get; set; }
    public int Gen1Delta { get; set; }
    public int Gen2Delta { get; set; }
    public GcMemoryInfoMetric MemoryInfo { get; set; } = new();
}

public sealed class ThreadPoolMetric
{
    public DateTime CapturedAtUtc { get; set; }
    public int AvailableWorkerThreads { get; set; }
    public int MaxWorkerThreads { get; set; }
    public int MinWorkerThreads { get; set; }
    public int AvailableIoCompletionThreads { get; set; }
    public int MaxIoCompletionThreads { get; set; }
    public int MinIoCompletionThreads { get; set; }
    public int BusyWorkerThreads { get; set; }
    public int ProcessThreadCount { get; set; }
}

public sealed class SocketConnectionMetric
{
    public string LocalEndpoint { get; set; } = string.Empty;
    public string RemoteEndpoint { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RemoteIp { get; set; } = string.Empty;
    public int RemotePort { get; set; }
}

public sealed class SocketConnectionGroup
{
    public string RemoteEndpoint { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int ConnectionCount { get; set; }
}

public sealed class SocketMetricSummary
{
    public DateTime CapturedAtUtc { get; set; }
    public int TotalConnections { get; set; }
    public IReadOnlyList<SocketConnectionMetric> Connections { get; set; } = Array.Empty<SocketConnectionMetric>();
    public IReadOnlyList<SocketConnectionGroup> Groups { get; set; } = Array.Empty<SocketConnectionGroup>();
}

public sealed class DatabaseConnectionMetric
{
    public string ApplicationName { get; set; } = string.Empty;
    public string? ClientAddress { get; set; }
    public string State { get; set; } = string.Empty;
    public int ConnectionCount { get; set; }
}

public sealed class DatabaseConnectionSummary
{
    public DateTime CapturedAtUtc { get; set; }
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int IdleInTransactionConnections { get; set; }
    public IReadOnlyList<DatabaseConnectionMetric> Entries { get; set; } = Array.Empty<DatabaseConnectionMetric>();
}

public sealed class RuntimeMetricSnapshot
{
    public DateTime CapturedAtUtc { get; set; }
    public CpuMetric Cpu { get; set; } = new();
    public RamMetric Ram { get; set; } = new();
    public GarbageCollectorMetric GarbageCollector { get; set; } = new();
    public ThreadPoolMetric ThreadPool { get; set; } = new();
    public SocketMetricSummary? SocketSummary { get; set; }
    public DatabaseConnectionSummary? DatabaseSummary { get; set; }
}

public sealed class SpikeReport
{
    public DateTime DetectedAtUtc { get; set; }
    public IReadOnlyList<string> Reasons { get; set; } = Array.Empty<string>();
    public RuntimeMetricSnapshot Snapshot { get; set; } = new();
    public DateTime CorrelationWindowStartUtc { get; set; }
    public DateTime CorrelationWindowEndUtc { get; set; }
}

public sealed class MonitoringReport
{
    public DateTime GeneratedAtUtc { get; set; }
    public RuntimeMetricSnapshot CurrentSnapshot { get; set; } = new();
    public IReadOnlyList<RuntimeMetricSnapshot> RecentSnapshots { get; set; } = Array.Empty<RuntimeMetricSnapshot>();
    public IReadOnlyList<SpikeReport> SpikeReports { get; set; } = Array.Empty<SpikeReport>();
    public DatabaseConnectionSummary? DatabaseSummary { get; set; }
    public SocketMetricSummary? SocketSummary { get; set; }
    public IReadOnlyList<string> PossibleCauses { get; set; } = Array.Empty<string>();
}
