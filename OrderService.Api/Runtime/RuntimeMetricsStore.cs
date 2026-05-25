using System.Collections.Concurrent;

namespace OrderService.Api.Runtime;

/// <summary>
/// In-memory, per-process request/latency/exception counters for lightweight diagnostics (not a replacement for Prometheus).
/// </summary>
public sealed class RuntimeMetricsStore
{
    private long _totalRequests;
    private long _activeRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _totalExceptions;
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();
    private readonly ConcurrentDictionary<string, long> _exceptionsByType = new();
    private readonly object _latencyLock = new();
    private readonly double[] _latencyBuffer;
    private int _latencyCount;
    private int _latencyWriteIndex;
    private const int LatencyCapacity = 2000;

    private DateTime? _lastExceptionUtc;
    private string _lastExceptionType = string.Empty;
    private string _lastExceptionMessage = string.Empty;

    public RuntimeMetricsStore()
    {
        _latencyBuffer = new double[LatencyCapacity];
    }

    public void IncrementTotalRequests() => Interlocked.Increment(ref _totalRequests);

    public void IncrementActive() => Interlocked.Increment(ref _activeRequests);

    public void DecrementActive() => Interlocked.Decrement(ref _activeRequests);

    public void RecordCompletion(int statusCode, double elapsedMs, bool success)
    {
        if (success)
        {
            Interlocked.Increment(ref _successfulRequests);
        }
        else
        {
            Interlocked.Increment(ref _failedRequests);
        }

        _statusCodes.AddOrUpdate(statusCode, 1, (_, v) => v + 1);
        RecordLatency(elapsedMs);
    }

    public void RecordException(Exception ex, string sanitizedMessage)
    {
        Interlocked.Increment(ref _totalExceptions);
        _lastExceptionUtc = DateTime.UtcNow;
        _lastExceptionType = ex.GetType().Name;
        _lastExceptionMessage = sanitizedMessage;
        var key = ex.GetType().FullName ?? ex.GetType().Name;
        _exceptionsByType.AddOrUpdate(key, 1, (_, v) => v + 1);
    }

    private void RecordLatency(double elapsedMs)
    {
        lock (_latencyLock)
        {
            _latencyBuffer[_latencyWriteIndex] = elapsedMs;
            _latencyWriteIndex = (_latencyWriteIndex + 1) % LatencyCapacity;
            if (_latencyCount < LatencyCapacity)
            {
                _latencyCount++;
            }
        }
    }

    public void ResetVolatile()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _activeRequests, 0);
        Interlocked.Exchange(ref _successfulRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _totalExceptions, 0);
        _statusCodes.Clear();
        _exceptionsByType.Clear();
        lock (_latencyLock)
        {
            _latencyCount = 0;
            _latencyWriteIndex = 0;
            Array.Clear(_latencyBuffer, 0, _latencyBuffer.Length);
        }

        _lastExceptionUtc = null;
        _lastExceptionType = string.Empty;
        _lastExceptionMessage = string.Empty;
    }

    public long TotalRequests => Interlocked.Read(ref _totalRequests);
    public long ActiveRequests => Interlocked.Read(ref _activeRequests);
    public long SuccessfulRequests => Interlocked.Read(ref _successfulRequests);
    public long FailedRequests => Interlocked.Read(ref _failedRequests);
    public long TotalExceptions => Interlocked.Read(ref _totalExceptions);
    public IReadOnlyDictionary<int, long> StatusCodeCounts => _statusCodes;
    public IReadOnlyDictionary<string, long> ExceptionsByType => _exceptionsByType;
    public DateTime? LastExceptionTimestampUtc => _lastExceptionUtc;
    public string LastExceptionType => _lastExceptionType;
    public string LastExceptionMessage => _lastExceptionMessage;

    public (long Count, double Avg, double Min, double Max, double P50, double P95, double P99) GetLatencySnapshot()
    {
        lock (_latencyLock)
        {
            if (_latencyCount == 0)
            {
                return (0, 0, 0, 0, 0, 0, 0);
            }

            var slice = new double[_latencyCount];
            if (_latencyCount < LatencyCapacity)
            {
                Array.Copy(_latencyBuffer, slice, _latencyCount);
            }
            else
            {
                var start = _latencyWriteIndex;
                Array.Copy(_latencyBuffer, start, slice, 0, LatencyCapacity - start);
                Array.Copy(_latencyBuffer, 0, slice, LatencyCapacity - start, start);
            }

            Array.Sort(slice);
            double P(int pct) => slice[Math.Clamp((int)Math.Ceiling(pct / 100.0 * slice.Length) - 1, 0, slice.Length - 1)];
            var avg = slice.Average();
            return (slice.Length, avg, slice[0], slice[^1], P(50), P(95), P(99));
        }
    }
}
