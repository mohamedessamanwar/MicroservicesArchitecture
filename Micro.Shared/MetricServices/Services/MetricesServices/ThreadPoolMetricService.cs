using System.Diagnostics;
using Micro.Shared.MetricServices.Abstractions;
using Micro.Shared.MetricServices.Models;

namespace Micro.Shared.MetricServices.Services.MetricesServices;

public sealed class ThreadPoolMetricService : IThreadPoolMetricService
{
    public Task<ThreadPoolMetric> GetThreadPoolMetricAsync(CancellationToken cancellationToken = default)
    {
        ThreadPool.GetAvailableThreads(out var availableWorkers, out var availableIo);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIo);
        ThreadPool.GetMinThreads(out var minWorkers, out var minIo);

        // Worker threads execute user code; IO completion port threads serve async IO callbacks.
        // Busy worker threads are MaxWorkerThreads - AvailableWorkerThreads.
        var metric = new ThreadPoolMetric
        {
            CapturedAtUtc = DateTime.UtcNow,
            AvailableWorkerThreads = availableWorkers,
            MaxWorkerThreads = maxWorkers,
            MinWorkerThreads = minWorkers,
            AvailableIoCompletionThreads = availableIo,
            MaxIoCompletionThreads = maxIo,
            MinIoCompletionThreads = minIo,
            BusyWorkerThreads = Math.Max(0, maxWorkers - availableWorkers),
            ProcessThreadCount = Process.GetCurrentProcess().Threads.Count
        };

        return Task.FromResult(metric);
    }
}
