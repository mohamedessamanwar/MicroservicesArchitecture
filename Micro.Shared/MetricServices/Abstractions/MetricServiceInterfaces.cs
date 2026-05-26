using System.Data.Common;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Abstractions;

public interface ICpuMetricService
{
    Task<CpuMetric> GetCpuMetricAsync(CancellationToken cancellationToken = default);
}

public interface IRamMetricService
{
    Task<RamMetric> GetRamMetricAsync(CancellationToken cancellationToken = default);
}

public interface IGarbageCollectorMetricService
{
    Task<GarbageCollectorMetric> GetGarbageCollectorMetricAsync(CancellationToken cancellationToken = default);
}

public interface IThreadPoolMetricService
{
    Task<ThreadPoolMetric> GetThreadPoolMetricAsync(CancellationToken cancellationToken = default);
}

public interface ISocketMetricService
{
    Task<SocketMetricSummary> GetSocketSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseConnectionMetricService
{
    Task<DatabaseConnectionSummary?> GetDatabaseConnectionSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IRuntimeMetricSnapshotService
{
    Task<RuntimeMetricSnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}

public interface IMonitoringReportService
{
    Task<MonitoringReport> BuildReportAsync(CancellationToken cancellationToken = default);
}

public interface IRuntimeMetricSnapshotRepository
{
    bool IsEnabled { get; }
    Task<Guid> SaveSnapshotAsync(RuntimeMetricSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<RuntimeMetricSnapshot?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeMetricSnapshot>> GetLatestSnapshotsAsync(int maxCount, CancellationToken cancellationToken = default);
}

public interface ISpikeReportRepository
{
    bool IsEnabled { get; }
    Task SaveSpikeReportAsync(SpikeReport report, Guid snapshotId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpikeReport>> GetLatestSpikeReportsAsync(int maxCount, CancellationToken cancellationToken = default);
}

public interface IRuntimeSnapshotStore
{
    void Add(RuntimeMetricSnapshot snapshot);
    RuntimeMetricSnapshot? GetLatest();
    IReadOnlyList<RuntimeMetricSnapshot> GetLatestSnapshots(int maxCount);
}

public interface ISpikeReportStore
{
    void Add(SpikeReport report);
    IReadOnlyList<SpikeReport> GetLatestReports(int maxCount);
}

public interface IMonitoringDbConnectionFactory
{
    Task<DbConnectionContext?> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class DbConnectionContext : IAsyncDisposable
{
    private readonly IAsyncDisposable? _scope;

    public DbConnectionContext(DbConnection connection, IAsyncDisposable? scope)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _scope = scope;
    }

    public DbConnection Connection { get; }

    public async ValueTask DisposeAsync()
    {
        if (_scope != null)
        {
            await _scope.DisposeAsync();
            return;
        }

        await Connection.DisposeAsync();
    }
}
