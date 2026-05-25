using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace OrderService.Api.Runtime;

public interface IRuntimeMetricsService
{
    RuntimeSummaryDto GetSummary();
    RuntimeMetricsDto GetFullMetrics(RuntimeMetricsStore store);
    ProcessMetricsDto GetProcess();
    MemoryMetricsDto GetMemory();
    GcMetricsDto GetGc();
    ThreadPoolMetricsDto GetThreadPool();
    CpuMetricsDto GetCpu();
    RequestMetricsDto GetRequests(RuntimeMetricsStore store);
    ExceptionMetricsDto GetExceptions(RuntimeMetricsStore store);
    LatencyMetricsDto GetLatency(RuntimeMetricsStore store);
    AllocationMetricsDto GetAllocations();
    RuntimeInfoDto GetRuntimeInfo();
    HealthRiskDto GetHealthRisk(RuntimeMetricsStore store);
}

public sealed class RuntimeMetricsService : IRuntimeMetricsService
{
    private readonly IWebHostEnvironment _environment;
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly object _cpuLock = new();
    private TimeSpan _lastCpuSample;
    private DateTime _lastCpuUtc;

    public RuntimeMetricsService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _lastCpuSample = _process.TotalProcessorTime;
        _lastCpuUtc = DateTime.UtcNow;
    }

    public RuntimeSummaryDto GetSummary()
    {
        var now = DateTime.UtcNow;
        var startUtc = _process.StartTime.ToUniversalTime();
        return new RuntimeSummaryDto
        {
            ProcessId = _process.Id,
            MachineName = Environment.MachineName,
            StartTimeUtc = startUtc,
            UptimeSeconds = (now - startUtc).TotalSeconds,
            Framework = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            TimestampUtc = now
        };
    }

    public RuntimeMetricsDto GetFullMetrics(RuntimeMetricsStore store) =>
        new()
        {
            Process = GetProcess(),
            Memory = GetMemory(),
            Gc = GetGc(),
            ThreadPool = GetThreadPool(),
            Cpu = GetCpu(),
            Requests = GetRequests(store),
            Exceptions = GetExceptions(store),
            Latency = GetLatency(store),
            Allocations = GetAllocations(),
            Runtime = GetRuntimeInfo(),
            HealthRisk = GetHealthRisk(store),
            TimestampUtc = DateTime.UtcNow
        };

    public ProcessMetricsDto GetProcess()
    {
        _process.Refresh();
        var now = DateTime.UtcNow;
        var startUtc = _process.StartTime.ToUniversalTime();
        return new ProcessMetricsDto
        {
            ProcessId = _process.Id,
            ProcessName = _process.ProcessName,
            MachineName = Environment.MachineName,
            StartTimeUtc = startUtc,
            UptimeSeconds = (now - startUtc).TotalSeconds,
            ThreadCount = _process.Threads.Count,
            HandleCount = _process.HandleCount,
            CpuTotalTimeMs = _process.TotalProcessorTime.TotalMilliseconds,
            UserProcessorTimeMs = _process.UserProcessorTime.TotalMilliseconds,
            PrivilegedProcessorTimeMs = _process.PrivilegedProcessorTime.TotalMilliseconds
        };
    }

    public MemoryMetricsDto GetMemory()
    {
        _process.Refresh();
        var gc = GC.GetGCMemoryInfo();
        return new MemoryMetricsDto
        {
            WorkingSetMb = RoundMb(_process.WorkingSet64),
            PrivateMemoryMb = RoundMb(_process.PrivateMemorySize64),
            VirtualMemoryMb = RoundMb(_process.VirtualMemorySize64),
            ManagedHeapMb = RoundMb(GC.GetTotalMemory(false)),
            HeapSizeMb = RoundMb(gc.HeapSizeBytes),
            FragmentedMb = RoundMb(gc.FragmentedBytes),
            MemoryLoadMb = RoundMb(gc.MemoryLoadBytes),
            AvailableMemoryMb = RoundMb(gc.TotalAvailableMemoryBytes),
            TotalCommittedBytesMb = RoundMb(gc.TotalCommittedBytes),
            PinnedObjectsCount = gc.PinnedObjectsCount,
            FinalizationPendingCount = 0
        };
    }

    public GcMetricsDto GetGc()
    {
        var gc = GC.GetGCMemoryInfo();
        return new GcMetricsDto
        {
            IsServerGc = GCSettings.IsServerGC,
            LatencyMode = GCSettings.LatencyMode.ToString(),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            PauseTimePercentage = gc.PauseTimePercentage,
            HeapSizeMb = RoundMb(gc.HeapSizeBytes),
            FragmentedMb = RoundMb(gc.FragmentedBytes),
            MemoryLoadMb = RoundMb(gc.MemoryLoadBytes),
            HighMemoryLoadThresholdMb = RoundMb(gc.HighMemoryLoadThresholdBytes),
            TotalAvailableMemoryMb = RoundMb(gc.TotalAvailableMemoryBytes),
            Compacted = gc.Compacted,
            Concurrent = gc.Concurrent,
            Generation = gc.Generation,
            Index = gc.Index
        };
    }

    public ThreadPoolMetricsDto GetThreadPool()
    {
        ThreadPool.GetAvailableThreads(out var availW, out var availIo);
        ThreadPool.GetMaxThreads(out var maxW, out var maxIo);
        ThreadPool.GetMinThreads(out var minW, out var minIo);
        return new ThreadPoolMetricsDto
        {
            UsedWorkerThreads = maxW - availW,
            AvailableWorkerThreads = availW,
            MaxWorkerThreads = maxW,
            MinWorkerThreads = minW,
            UsedCompletionPortThreads = maxIo - availIo,
            AvailableCompletionPortThreads = availIo,
            MaxCompletionPortThreads = maxIo,
            MinCompletionPortThreads = minIo,
            PendingWorkItemCount = ThreadPool.PendingWorkItemCount,
            CompletedWorkItemCount = ThreadPool.CompletedWorkItemCount,
            ThreadCount = Process.GetCurrentProcess().Threads.Count
        };
    }

    public CpuMetricsDto GetCpu()
    {
        _process.Refresh();
        lock (_cpuLock)
        {
            var now = DateTime.UtcNow;
            var cpu = _process.TotalProcessorTime;
            var wallMs = (now - _lastCpuUtc).TotalMilliseconds;
            var deltaCpuMs = (cpu - _lastCpuSample).TotalMilliseconds;
            _lastCpuSample = cpu;
            _lastCpuUtc = now;

            double? pct = null;
            var note = "CPU percentage requires at least two samples.";
            if (wallMs > 50 && Environment.ProcessorCount > 0)
            {
                pct = Math.Clamp(deltaCpuMs / (wallMs * Environment.ProcessorCount) * 100.0, 0, 100);
                note = "Approximate process CPU share between samples (local to this instance).";
            }

            return new CpuMetricsDto
            {
                ProcessorCount = Environment.ProcessorCount,
                TotalProcessorTimeMs = cpu.TotalMilliseconds,
                UserProcessorTimeMs = _process.UserProcessorTime.TotalMilliseconds,
                PrivilegedProcessorTimeMs = _process.PrivilegedProcessorTime.TotalMilliseconds,
                CpuUsagePercent = pct,
                Note = note
            };
        }
    }

    public RequestMetricsDto GetRequests(RuntimeMetricsStore store)
    {
        var uptimeMin = Math.Max((DateTime.UtcNow - _process.StartTime.ToUniversalTime()).TotalMinutes, 0.01);
        var rpm = store.TotalRequests / uptimeMin;
        return new RequestMetricsDto
        {
            TotalRequests = store.TotalRequests,
            ActiveRequests = store.ActiveRequests,
            SuccessfulRequests = store.SuccessfulRequests,
            FailedRequests = store.FailedRequests,
            StatusCodeCounts = new Dictionary<int, long>(store.StatusCodeCounts),
            RequestsPerMinute = Math.Round(rpm, 2)
        };
    }

    public ExceptionMetricsDto GetExceptions(RuntimeMetricsStore store) =>
        new()
        {
            TotalExceptions = store.TotalExceptions,
            LastExceptionTimestampUtc = store.LastExceptionTimestampUtc,
            LastExceptionType = store.LastExceptionType,
            LastExceptionMessage = store.LastExceptionMessage,
            ExceptionsByType = new Dictionary<string, long>(store.ExceptionsByType)
        };

    public LatencyMetricsDto GetLatency(RuntimeMetricsStore store)
    {
        var (count, avg, min, max, p50, p95, p99) = store.GetLatencySnapshot();
        return new LatencyMetricsDto
        {
            TotalMeasuredRequests = count,
            AverageLatencyMs = Math.Round(avg, 3),
            MinLatencyMs = Math.Round(min, 3),
            MaxLatencyMs = Math.Round(max, 3),
            P50LatencyMs = Math.Round(p50, 3),
            P95LatencyMs = Math.Round(p95, 3),
            P99LatencyMs = Math.Round(p99, 3),
            RollingWindowSize = 2000
        };
    }

    public AllocationMetricsDto GetAllocations() =>
        new()
        {
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false),
            TotalAllocatedMb = RoundMb(GC.GetTotalAllocatedBytes(false)),
            ManagedHeapMb = RoundMb(GC.GetTotalMemory(false)),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };

    public RuntimeInfoDto GetRuntimeInfo() =>
        new()
        {
            Framework = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            Is64BitProcess = Environment.Is64BitProcess,
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier
        };

    public HealthRiskDto GetHealthRisk(RuntimeMetricsStore store)
    {
        var warnings = new List<string>();
        var recs = new List<string>();
        var gc = GC.GetGCMemoryInfo();
        var memLoadRatio = gc.TotalAvailableMemoryBytes > 0 ? (double)gc.MemoryLoadBytes / gc.TotalAvailableMemoryBytes : 0;

        if (memLoadRatio > 0.9)
        {
            warnings.Add("Memory load is critically high.");
        }
        else if (memLoadRatio > 0.8)
        {
            warnings.Add("Memory load is elevated.");
        }

        if (RoundMb(gc.FragmentedBytes) > 500)
        {
            warnings.Add("GC fragmentation is high.");
        }

        var (_, _, _, _, _, p95, _) = store.GetLatencySnapshot();
        if (p95 > 3000)
        {
            warnings.Add("p95 latency is very high.");
        }
        else if (p95 > 1000)
        {
            warnings.Add("p95 latency is elevated.");
        }

        if (store.TotalExceptions > 0)
        {
            warnings.Add("Unhandled exceptions have been observed on this instance.");
        }

        if (!GCSettings.IsServerGC && !_environment.IsDevelopment())
        {
            warnings.Add("Server GC is disabled while not in Development.");
            recs.Add("Prefer Server GC for ASP.NET Core throughput in production.");
        }

        var cpu = GetCpu();
        if (cpu.CpuUsagePercent is > 90)
        {
            warnings.Add("CPU usage estimate is critically high.");
        }
        else if (cpu.CpuUsagePercent is > 80)
        {
            warnings.Add("CPU usage estimate is high.");
        }

        ThreadPool.GetAvailableThreads(out var aw, out _);
        ThreadPool.GetMaxThreads(out var mw, out _);
        var used = mw - aw;
        if (mw > 0 && used / (double)mw > 0.7)
        {
            warnings.Add("ThreadPool worker utilization is high.");
        }

        if (store.ActiveRequests > 200)
        {
            warnings.Add("Many concurrent active requests (local counter).");
        }

        var status = warnings.Count == 0 ? "Healthy" : warnings.Count <= 2 ? "Warning" : "Critical";
        if (warnings.Count > 0)
        {
            recs.Add("Export metrics to Prometheus/OpenTelemetry for cluster-wide visibility.");
        }

        return new HealthRiskDto { Status = status, Warnings = warnings, Recommendations = recs };
    }

    private static double RoundMb(long bytes) => Math.Round(bytes / 1024d / 1024d, 3);
}
