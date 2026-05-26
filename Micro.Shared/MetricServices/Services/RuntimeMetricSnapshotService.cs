using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;
using Micro.Shared.MetricServices.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Micro.Shared.MetricServices.Services;

public sealed class RuntimeMetricSnapshotService : IRuntimeMetricSnapshotService
{
    private readonly ICpuMetricService _cpuMetricService;
    private readonly IRamMetricService _ramMetricService;
    private readonly IGarbageCollectorMetricService _gcMetricService;
    private readonly IThreadPoolMetricService _threadPoolMetricService;
    private readonly ISocketMetricService _socketMetricService;
    private readonly IDatabaseConnectionMetricService _databaseMetricService;
    private readonly IOptionsMonitor<MetricMonitoringOptions> _options;
    private readonly ILogger<RuntimeMetricSnapshotService> _logger;

    public RuntimeMetricSnapshotService(
        ICpuMetricService cpuMetricService,
        IRamMetricService ramMetricService,
        IGarbageCollectorMetricService gcMetricService,
        IThreadPoolMetricService threadPoolMetricService,
        ISocketMetricService socketMetricService,
        IDatabaseConnectionMetricService databaseMetricService,
        IOptionsMonitor<MetricMonitoringOptions> options,
        ILogger<RuntimeMetricSnapshotService> logger)
    {
        _cpuMetricService = cpuMetricService;
        _ramMetricService = ramMetricService;
        _gcMetricService = gcMetricService;
        _threadPoolMetricService = threadPoolMetricService;
        _socketMetricService = socketMetricService;
        _databaseMetricService = databaseMetricService;
        _options = options;
        _logger = logger;
    }

    public async Task<RuntimeMetricSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await CaptureCoreMetricsAsync(cancellationToken);

        await CaptureOptionalMetricsAsync(snapshot, cancellationToken);

        return snapshot;
    }

    private async Task<RuntimeMetricSnapshot> CaptureCoreMetricsAsync(CancellationToken cancellationToken)
    {
        return new RuntimeMetricSnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            Cpu = await CaptureCpuMetricAsync(cancellationToken),
            Ram = await CaptureRamMetricAsync(cancellationToken),
            GarbageCollector = await CaptureGarbageCollectorMetricAsync(cancellationToken),
            ThreadPool = await CaptureThreadPoolMetricAsync(cancellationToken)
        };
    }

    private async Task CaptureOptionalMetricsAsync(
        RuntimeMetricSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshot.SocketSummary = await CaptureSocketMetricIfEnabledAsync(cancellationToken);
        snapshot.DatabaseSummary = await CaptureDatabaseMetricIfEnabledAsync(cancellationToken);
    }

    private async Task<CpuMetric> CaptureCpuMetricAsync(CancellationToken cancellationToken)
    {
        return await _cpuMetricService.GetCpuMetricAsync(cancellationToken);
    }

    private async Task<RamMetric> CaptureRamMetricAsync(CancellationToken cancellationToken)
    {
        return await _ramMetricService.GetRamMetricAsync(cancellationToken);
    }

    private async Task<GarbageCollectorMetric> CaptureGarbageCollectorMetricAsync(
        CancellationToken cancellationToken)
    {
        return await _gcMetricService.GetGarbageCollectorMetricAsync(cancellationToken);
    }

    private async Task<ThreadPoolMetric> CaptureThreadPoolMetricAsync(
        CancellationToken cancellationToken)
    {
        return await _threadPoolMetricService.GetThreadPoolMetricAsync(cancellationToken);
    }

    private async Task<SocketMetricSummary?> CaptureSocketMetricIfEnabledAsync(
        CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.EnableSocketMetrics)
        {
            return null;
        }

        try
        {
            return await _socketMetricService.GetSocketSummaryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Socket metrics collection failed.");
            return null;
        }
    }

    private async Task<DatabaseConnectionSummary?> CaptureDatabaseMetricIfEnabledAsync(
        CancellationToken cancellationToken)
    {
        if (!_options.CurrentValue.EnableDatabaseConnectionMetrics)
        {
            return null;
        }

        try
        {
            return await _databaseMetricService.GetDatabaseConnectionSummaryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connection metrics collection failed.");
            return null;
        }
    }
}