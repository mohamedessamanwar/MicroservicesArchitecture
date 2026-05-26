using System.Diagnostics;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class RamMetricService : IRamMetricService
{
    private readonly Process _process;

    public RamMetricService()
    {
        _process = Process.GetCurrentProcess();
    }

    public Task<RamMetric> GetRamMetricAsync(CancellationToken cancellationToken = default)
    {
        _process.Refresh();

        var gcInfo = GC.GetGCMemoryInfo();

        // Working set is the physical RAM currently used by the process.
        // Private memory is memory allocated exclusively to the process (not shared).
        // Managed heap is live managed objects; GC heap includes segments and overhead.
        var metric = new RamMetric
        {
            CapturedAtUtc = DateTime.UtcNow,
            WorkingSetMb = ToMb(_process.WorkingSet64),
            PrivateMemoryMb = ToMb(_process.PrivateMemorySize64),
            ManagedHeapMb = ToMb(GC.GetTotalMemory(false)),
            GcHeapMb = ToMb(gcInfo.HeapSizeBytes),
            GcMemoryLoadMb = ToMb(gcInfo.MemoryLoadBytes)
        };

        return Task.FromResult(metric);
    }

    private static double ToMb(long bytes)
    {
        return Math.Round(bytes / 1024d / 1024d, 2);
    }
}
