using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.MetricServices;

public sealed class RuntimeMetricSnapshotRepository : IRuntimeMetricSnapshotRepository
{
    private readonly AppDbContext _dbContext;

    public RuntimeMetricSnapshotRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsEnabled => true;

    public async Task<Guid> SaveSnapshotAsync(RuntimeMetricSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var entity = MapToEntity(snapshot);
        _dbContext.RuntimeMetricSnapshots.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<RuntimeMetricSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.RuntimeMetricSnapshots
            .AsNoTracking()
            .OrderByDescending(item => item.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return record == null ? null : MapToModel(record);
    }

    public async Task<IReadOnlyList<RuntimeMetricSnapshot>> GetLatestSnapshotsAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.RuntimeMetricSnapshots
            .AsNoTracking()
            .OrderByDescending(item => item.CapturedAtUtc)
            .Take(Math.Max(1, maxCount))
            .ToListAsync(cancellationToken);

        return records.Select(MapToModel).ToList();
    }

    private static RuntimeMetricSnapshotRecord MapToEntity(RuntimeMetricSnapshot snapshot)
    {
        return new RuntimeMetricSnapshotRecord
        {
            Id = Guid.NewGuid(),
            CapturedAtUtc = snapshot.CapturedAtUtc,
            CpuUsagePercent = snapshot.Cpu.UsagePercent,
            CpuDeltaCpuMs = snapshot.Cpu.DeltaCpuMs,
            CpuDeltaWallMs = snapshot.Cpu.DeltaWallMs,
            CpuLogicalProcessorCount = snapshot.Cpu.LogicalProcessorCount,
            RamWorkingSetMb = snapshot.Ram.WorkingSetMb,
            RamPrivateMemoryMb = snapshot.Ram.PrivateMemoryMb,
            RamManagedHeapMb = snapshot.Ram.ManagedHeapMb,
            RamGcHeapMb = snapshot.Ram.GcHeapMb,
            RamGcMemoryLoadMb = snapshot.Ram.GcMemoryLoadMb,
            GcGen0Collections = snapshot.GarbageCollector.Gen0Collections,
            GcGen1Collections = snapshot.GarbageCollector.Gen1Collections,
            GcGen2Collections = snapshot.GarbageCollector.Gen2Collections,
            GcGen0Delta = snapshot.GarbageCollector.Gen0Delta,
            GcGen1Delta = snapshot.GarbageCollector.Gen1Delta,
            GcGen2Delta = snapshot.GarbageCollector.Gen2Delta,
            GcHeapSizeMb = snapshot.GarbageCollector.MemoryInfo.HeapSizeMb,
            GcMemoryLoadMb = snapshot.GarbageCollector.MemoryInfo.MemoryLoadMb,
            GcTotalAvailableMemoryMb = snapshot.GarbageCollector.MemoryInfo.TotalAvailableMemoryMb,
            GcHighMemoryLoadThresholdMb = snapshot.GarbageCollector.MemoryInfo.HighMemoryLoadThresholdMb,
            GcFragmentedMb = snapshot.GarbageCollector.MemoryInfo.FragmentedMb,
            ThreadPoolAvailableWorkerThreads = snapshot.ThreadPool.AvailableWorkerThreads,
            ThreadPoolMaxWorkerThreads = snapshot.ThreadPool.MaxWorkerThreads,
            ThreadPoolMinWorkerThreads = snapshot.ThreadPool.MinWorkerThreads,
            ThreadPoolAvailableIoCompletionThreads = snapshot.ThreadPool.AvailableIoCompletionThreads,
            ThreadPoolMaxIoCompletionThreads = snapshot.ThreadPool.MaxIoCompletionThreads,
            ThreadPoolMinIoCompletionThreads = snapshot.ThreadPool.MinIoCompletionThreads,
            ThreadPoolBusyWorkerThreads = snapshot.ThreadPool.BusyWorkerThreads,
            ProcessThreadCount = snapshot.ThreadPool.ProcessThreadCount,
            SocketTotalConnections = snapshot.SocketSummary?.TotalConnections,
            DbTotalConnections = snapshot.DatabaseSummary?.TotalConnections,
            DbActiveConnections = snapshot.DatabaseSummary?.ActiveConnections,
            DbIdleConnections = snapshot.DatabaseSummary?.IdleConnections,
            DbIdleInTransactionConnections = snapshot.DatabaseSummary?.IdleInTransactionConnections
        };
    }

    private static RuntimeMetricSnapshot MapToModel(RuntimeMetricSnapshotRecord record)
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
