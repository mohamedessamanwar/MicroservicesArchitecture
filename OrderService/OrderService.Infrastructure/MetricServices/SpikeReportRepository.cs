using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.MetricServices;

public sealed class SpikeReportRepository : ISpikeReportRepository
{
    private const string ReasonSeparator = " | ";
    private readonly AppDbContext _dbContext;

    public SpikeReportRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsEnabled => true;

    public async Task SaveSpikeReportAsync(SpikeReport report, Guid snapshotId, CancellationToken cancellationToken = default)
    {
        if (snapshotId == Guid.Empty)
        {
            return;
        }

        var entity = new SpikeReportRecord
        {
            Id = Guid.NewGuid(),
            RuntimeMetricSnapshotId = snapshotId,
            DetectedAtUtc = report.DetectedAtUtc,
            CorrelationWindowStartUtc = report.CorrelationWindowStartUtc,
            CorrelationWindowEndUtc = report.CorrelationWindowEndUtc,
            Reasons = string.Join(ReasonSeparator, report.Reasons)
        };

        _dbContext.SpikeReports.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpikeReport>> GetLatestSpikeReportsAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.SpikeReports
            .AsNoTracking()
            .Include(report => report.Snapshot)
            .OrderByDescending(report => report.DetectedAtUtc)
            .Take(Math.Max(1, maxCount))
            .ToListAsync(cancellationToken);

        return records.Select(MapToModel).ToList();
    }

    private static SpikeReport MapToModel(SpikeReportRecord record)
    {
        return new SpikeReport
        {
            DetectedAtUtc = record.DetectedAtUtc,
            Reasons = SplitReasons(record.Reasons),
            Snapshot = RuntimeMetricSnapshotRepositoryAccessor.MapSnapshotToModel(record.Snapshot),
            CorrelationWindowStartUtc = record.CorrelationWindowStartUtc,
            CorrelationWindowEndUtc = record.CorrelationWindowEndUtc,
        };
    }

    private static IReadOnlyList<string> SplitReasons(string reasons)
    {
        return reasons.Split(ReasonSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    private static class RuntimeMetricSnapshotRepositoryAccessor
    {
        public static RuntimeMetricSnapshot MapSnapshotToModel(RuntimeMetricSnapshotRecord record)
        {
            var snapshot = new RuntimeMetricSnapshot
            {
                CapturedAtUtc = record.CapturedAtUtc,
                Cpu = new CpuMetric
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    UsagePercent = record.CpuUsagePercent,
                    DeltaCpuMs = record.CpuDeltaCpuMs,
                    DeltaWallMs = record.CpuDeltaWallMs,
                    LogicalProcessorCount = record.CpuLogicalProcessorCount
                },
                Ram = new RamMetric
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    WorkingSetMb = record.RamWorkingSetMb,
                    PrivateMemoryMb = record.RamPrivateMemoryMb,
                    ManagedHeapMb = record.RamManagedHeapMb,
                    GcHeapMb = record.RamGcHeapMb,
                    GcMemoryLoadMb = record.RamGcMemoryLoadMb
                },
                GarbageCollector = new GarbageCollectorMetric
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    Gen0Collections = record.GcGen0Collections,
                    Gen1Collections = record.GcGen1Collections,
                    Gen2Collections = record.GcGen2Collections,
                    Gen0Delta = record.GcGen0Delta,
                    Gen1Delta = record.GcGen1Delta,
                    Gen2Delta = record.GcGen2Delta,
                    MemoryInfo = new GcMemoryInfoMetric
                    {
                        HeapSizeMb = record.GcHeapSizeMb,
                        MemoryLoadMb = record.GcMemoryLoadMb,
                        TotalAvailableMemoryMb = record.GcTotalAvailableMemoryMb,
                        HighMemoryLoadThresholdMb = record.GcHighMemoryLoadThresholdMb,
                        FragmentedMb = record.GcFragmentedMb
                    }
                },
                ThreadPool = new ThreadPoolMetric
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    AvailableWorkerThreads = record.ThreadPoolAvailableWorkerThreads,
                    MaxWorkerThreads = record.ThreadPoolMaxWorkerThreads,
                    MinWorkerThreads = record.ThreadPoolMinWorkerThreads,
                    AvailableIoCompletionThreads = record.ThreadPoolAvailableIoCompletionThreads,
                    MaxIoCompletionThreads = record.ThreadPoolMaxIoCompletionThreads,
                    MinIoCompletionThreads = record.ThreadPoolMinIoCompletionThreads,
                    BusyWorkerThreads = record.ThreadPoolBusyWorkerThreads,
                    ProcessThreadCount = record.ProcessThreadCount
                }
            };

            if (record.SocketTotalConnections.HasValue)
            {
                snapshot.SocketSummary = new SocketMetricSummary
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    TotalConnections = record.SocketTotalConnections.Value,
                    Connections = Array.Empty<SocketConnectionMetric>(),
                    Groups = Array.Empty<SocketConnectionGroup>()
                };
            }

            if (record.DbTotalConnections.HasValue)
            {
                snapshot.DatabaseSummary = new DatabaseConnectionSummary
                {
                    CapturedAtUtc = record.CapturedAtUtc,
                    TotalConnections = record.DbTotalConnections.Value,
                    ActiveConnections = record.DbActiveConnections ?? 0,
                    IdleConnections = record.DbIdleConnections ?? 0,
                    IdleInTransactionConnections = record.DbIdleInTransactionConnections ?? 0,
                    Entries = Array.Empty<DatabaseConnectionMetric>()
                };
            }

            return snapshot;
        }
    }
}
