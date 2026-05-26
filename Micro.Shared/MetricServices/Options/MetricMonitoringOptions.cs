namespace Micro.Shared.MetricServices.Options;

public sealed class MetricMonitoringOptions
{
    public bool Enabled { get; set; } = true;
    public int SnapshotIntervalSeconds { get; set; } = 15;
    public int SnapshotRetentionMinutes { get; set; } = 30;
    public int MaxSnapshots { get; set; } = 500;
    public int RequestRetentionMinutes { get; set; } = 15;
    public int MaxStoredRequests { get; set; } = 2000;
    public int SlowRequestThresholdMs { get; set; } = 1000;
    public int SpikeCorrelationWindowSeconds { get; set; } = 60;
    public bool IncludeQueryString { get; set; }
    public bool EnableSocketMetrics { get; set; } = true;
    public bool EnableDatabaseConnectionMetrics { get; set; } = true;
    public double CpuSpikeThresholdPercent { get; set; } = 80;
    public double WorkingSetSpikeThresholdMb { get; set; } = 1024;
    public double ManagedHeapSpikeThresholdMb { get; set; } = 512;
    public int Gen2CollectionDeltaThreshold { get; set; } = 3;
    public int BusyWorkerThreadThreshold { get; set; } = 100;

    public string? DatabaseConnectionStringName { get; set; }
    public string? DatabaseConnectionString { get; set; }
}
