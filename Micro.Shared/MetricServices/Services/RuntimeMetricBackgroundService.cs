using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;
using Micro.Shared.MetricServices.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Micro.Shared.MetricServices.Services;

public sealed class RuntimeMetricBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<MetricMonitoringOptions> _options;
    private readonly ILogger<RuntimeMetricBackgroundService> _logger;

    public RuntimeMetricBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<MetricMonitoringOptions> options,
        ILogger<RuntimeMetricBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled())
        {
            return;
        }

        using var timer = CreateTimer();

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunMetricCycleAsync(stoppingToken);
        }
    }

    private async Task RunMetricCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var snapshot = await CaptureSnapshotAsync(services, cancellationToken);
            var snapshotId = await SaveSnapshotAsync(services, snapshot, cancellationToken);
            var spikeReport = CreateSpikeReport(snapshot);

            await SaveSpikeReportAsync(services, spikeReport, snapshotId, cancellationToken);
            LogSpikeReportIfExists(spikeReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing runtime metrics in background service.");
        }
    }

    private static async Task<RuntimeMetricSnapshot> CaptureSnapshotAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var snapshotService = services.GetRequiredService<IRuntimeMetricSnapshotService>();

        return await snapshotService.CaptureAsync(cancellationToken);
    }

    private static async Task<Guid> SaveSnapshotAsync(
        IServiceProvider services,
        RuntimeMetricSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var repository = services.GetService<IRuntimeMetricSnapshotRepository>();
        if (repository is null || !repository.IsEnabled)
        {
            return Guid.Empty;
        }

        return await repository.SaveSnapshotAsync(snapshot, cancellationToken);
    }

    private static async Task SaveSpikeReportAsync(
        IServiceProvider services,
        SpikeReport? spikeReport,
        Guid snapshotId,
        CancellationToken cancellationToken)
    {
        if (spikeReport is null || snapshotId == Guid.Empty)
        {
            return;
        }

        var repository = services.GetService<ISpikeReportRepository>();
        if (repository is null || !repository.IsEnabled)
        {
            return;
        }

        await repository.SaveSpikeReportAsync(spikeReport, snapshotId, cancellationToken);
    }

    private SpikeReport? CreateSpikeReport(RuntimeMetricSnapshot snapshot)
    {
        var reasons = GetSpikeReasons(snapshot);

        if (reasons.Count == 0)
        {
            return null;
        }

        var correlationWindow = GetCorrelationWindow();

        return new SpikeReport
        {
            DetectedAtUtc = snapshot.CapturedAtUtc,
            Snapshot = snapshot,
            Reasons = reasons,
            CorrelationWindowStartUtc = snapshot.CapturedAtUtc - correlationWindow,
            CorrelationWindowEndUtc = snapshot.CapturedAtUtc + correlationWindow
        };
    }

    private List<string> GetSpikeReasons(RuntimeMetricSnapshot snapshot)
    {
        var options = _options.CurrentValue;
        var reasons = new List<string>();

        AddCpuReason(snapshot, options, reasons);
        AddWorkingSetReason(snapshot, options, reasons);
        AddManagedHeapReason(snapshot, options, reasons);
        AddGen2Reason(snapshot, options, reasons);
        AddThreadPoolReason(snapshot, options, reasons);

        return reasons;
    }

    private static void AddCpuReason(
        RuntimeMetricSnapshot snapshot,
        MetricMonitoringOptions options,
        List<string> reasons)
    {
        if (snapshot.Cpu?.UsagePercent >= options.CpuSpikeThresholdPercent)
        {
            reasons.Add($"CPU usage {snapshot.Cpu.UsagePercent:0.##}%");
        }
    }

    private static void AddWorkingSetReason(
        RuntimeMetricSnapshot snapshot,
        MetricMonitoringOptions options,
        List<string> reasons)
    {
        if (snapshot.Ram?.WorkingSetMb >= options.WorkingSetSpikeThresholdMb)
        {
            reasons.Add($"Working set {snapshot.Ram.WorkingSetMb:0.##} MB");
        }
    }

    private static void AddManagedHeapReason(
        RuntimeMetricSnapshot snapshot,
        MetricMonitoringOptions options,
        List<string> reasons)
    {
        if (snapshot.Ram?.ManagedHeapMb >= options.ManagedHeapSpikeThresholdMb)
        {
            reasons.Add($"Managed heap {snapshot.Ram.ManagedHeapMb:0.##} MB");
        }
    }

    private static void AddGen2Reason(
        RuntimeMetricSnapshot snapshot,
        MetricMonitoringOptions options,
        List<string> reasons)
    {
        if (snapshot.GarbageCollector?.Gen2Delta >= options.Gen2CollectionDeltaThreshold)
        {
            reasons.Add($"Gen2 collections delta {snapshot.GarbageCollector.Gen2Delta}");
        }
    }

    private static void AddThreadPoolReason(
        RuntimeMetricSnapshot snapshot,
        MetricMonitoringOptions options,
        List<string> reasons)
    {
        if (snapshot.ThreadPool?.BusyWorkerThreads >= options.BusyWorkerThreadThreshold)
        {
            reasons.Add($"Busy worker threads {snapshot.ThreadPool.BusyWorkerThreads}");
        }
    }

    private void LogSpikeReportIfExists(SpikeReport? spikeReport)
    {
        if (spikeReport is null)
        {
            return;
        }

        _logger.LogWarning(
            "Runtime spike detected. Reasons: {Reasons}",
            string.Join(", ", spikeReport.Reasons));
    }

    private bool IsEnabled()
    {
        return _options.CurrentValue.Enabled;
    }

    private PeriodicTimer CreateTimer()
    {
        var intervalSeconds = Math.Max(1, _options.CurrentValue.SnapshotIntervalSeconds);
        return new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
    }

    private TimeSpan GetCorrelationWindow()
    {
        var seconds = Math.Max(1, _options.CurrentValue.SpikeCorrelationWindowSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}