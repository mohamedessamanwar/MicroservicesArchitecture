using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class GarbageCollectorMetricService : IGarbageCollectorMetricService
{
    private readonly object _sync = new();
    private int _lastGen0;
    private int _lastGen1;
    private int _lastGen2;

    public Task<GarbageCollectorMetric> GetGarbageCollectorMetricAsync(CancellationToken cancellationToken = default)
    {
        var currentGen0 = GC.CollectionCount(0);
        var currentGen1 = GC.CollectionCount(1);
        var currentGen2 = GC.CollectionCount(2);
        var gcInfo = GC.GetGCMemoryInfo();

        int deltaGen0;
        int deltaGen1;
        int deltaGen2;

        lock (_sync)
        {
            deltaGen0 = currentGen0 - _lastGen0;
            deltaGen1 = currentGen1 - _lastGen1;
            deltaGen2 = currentGen2 - _lastGen2;

            _lastGen0 = currentGen0;
            _lastGen1 = currentGen1;
            _lastGen2 = currentGen2;
        }

        // Frequent Gen0 increases indicate high allocation rates.
        // Gen2 increases can indicate memory pressure; if RAM keeps rising after Gen2, a leak is possible.
        var metric = new GarbageCollectorMetric
        {
            CapturedAtUtc = DateTime.UtcNow,
            Gen0Collections = currentGen0,
            Gen1Collections = currentGen1,
            Gen2Collections = currentGen2,
            Gen0Delta = Math.Max(0, deltaGen0),
            Gen1Delta = Math.Max(0, deltaGen1),
            Gen2Delta = Math.Max(0, deltaGen2),
            MemoryInfo = new GcMemoryInfoMetric
            {
                HeapSizeMb = ToMb(gcInfo.HeapSizeBytes),
                MemoryLoadMb = ToMb(gcInfo.MemoryLoadBytes),
                TotalAvailableMemoryMb = ToMb(gcInfo.TotalAvailableMemoryBytes),
                HighMemoryLoadThresholdMb = ToMb(gcInfo.HighMemoryLoadThresholdBytes),
                FragmentedMb = ToMb(gcInfo.FragmentedBytes)
            }
        };

        return Task.FromResult(metric);
    }

    private static double ToMb(long bytes)
    {
        return Math.Round(bytes / 1024d / 1024d, 2);
    }
}
