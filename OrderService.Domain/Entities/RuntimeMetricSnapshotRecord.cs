namespace OrderService.Domain.Entities;

public sealed class RuntimeMetricSnapshotRecord
{
    public Guid Id { get; set; }
    public DateTime CapturedAtUtc { get; set; }

    public double CpuUsagePercent { get; set; }
    public double CpuDeltaCpuMs { get; set; }
    public double CpuDeltaWallMs { get; set; }
    public int CpuLogicalProcessorCount { get; set; }

    public double RamWorkingSetMb { get; set; }
    public double RamPrivateMemoryMb { get; set; }
    public double RamManagedHeapMb { get; set; }
    public double RamGcHeapMb { get; set; }
    public double RamGcMemoryLoadMb { get; set; }

    public int GcGen0Collections { get; set; }
    public int GcGen1Collections { get; set; }
    public int GcGen2Collections { get; set; }
    public int GcGen0Delta { get; set; }
    public int GcGen1Delta { get; set; }
    public int GcGen2Delta { get; set; }
    public double GcHeapSizeMb { get; set; }
    public double GcMemoryLoadMb { get; set; }
    public double GcTotalAvailableMemoryMb { get; set; }
    public double GcHighMemoryLoadThresholdMb { get; set; }
    public double GcFragmentedMb { get; set; }

    public int ThreadPoolAvailableWorkerThreads { get; set; }
    public int ThreadPoolMaxWorkerThreads { get; set; }
    public int ThreadPoolMinWorkerThreads { get; set; }
    public int ThreadPoolAvailableIoCompletionThreads { get; set; }
    public int ThreadPoolMaxIoCompletionThreads { get; set; }
    public int ThreadPoolMinIoCompletionThreads { get; set; }
    public int ThreadPoolBusyWorkerThreads { get; set; }
    public int ProcessThreadCount { get; set; }

    public int? SocketTotalConnections { get; set; }

    public int? DbTotalConnections { get; set; }
    public int? DbActiveConnections { get; set; }
    public int? DbIdleConnections { get; set; }
    public int? DbIdleInTransactionConnections { get; set; }
}
