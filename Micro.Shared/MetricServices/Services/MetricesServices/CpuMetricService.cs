using System.Diagnostics;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class CpuMetricService : ICpuMetricService
{
    private readonly Process _process;
    private readonly object _sync = new();
    private TimeSpan _lastCpuTime;
    private long _lastTimestamp;

    public CpuMetricService()
    {
        _process = Process.GetCurrentProcess();
    }

    public Task<CpuMetric> GetCpuMetricAsync(CancellationToken cancellationToken = default)
    {
        _process.Refresh();

        var nowTimestamp = Stopwatch.GetTimestamp();
        var currentCpuTime = _process.TotalProcessorTime;
        double deltaCpuMs;
        double deltaWallMs;
        double usagePercent;

        lock (_sync)
        {
            if (_lastTimestamp == 0)
            {
                _lastTimestamp = nowTimestamp;
                _lastCpuTime = currentCpuTime;

                return Task.FromResult(new CpuMetric
                {
                    CapturedAtUtc = DateTime.UtcNow,
                    UsagePercent = 0,
                    DeltaCpuMs = 0,
                    DeltaWallMs = 0,
                    LogicalProcessorCount = Environment.ProcessorCount
                });
            }

            deltaCpuMs = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
            deltaWallMs = (nowTimestamp - _lastTimestamp) * 1000d / Stopwatch.Frequency;
            _lastTimestamp = nowTimestamp;
            _lastCpuTime = currentCpuTime;
        }

        // CPU% = (delta process CPU time / delta wall time / logical processors) * 100.
        // This normalizes the CPU time for multi-core systems to a 0-100% range.
        usagePercent = deltaWallMs <= 0
            ? 0
            : (deltaCpuMs / (deltaWallMs * Environment.ProcessorCount)) * 100d;

        return Task.FromResult(new CpuMetric
        {
            CapturedAtUtc = DateTime.UtcNow,
            UsagePercent = Math.Round(Math.Max(0, usagePercent), 2),
            DeltaCpuMs = Math.Round(deltaCpuMs, 2),
            DeltaWallMs = Math.Round(deltaWallMs, 2),
            LogicalProcessorCount = Environment.ProcessorCount
        });
    }
}
