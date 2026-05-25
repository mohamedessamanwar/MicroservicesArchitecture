using System.Diagnostics;

namespace OrderService.Api.Runtime;

public sealed class RuntimeRequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RuntimeMetricsStore _store;
    private readonly ILogger<RuntimeRequestMetricsMiddleware> _logger;

    public RuntimeRequestMetricsMiddleware(RequestDelegate next, RuntimeMetricsStore store, ILogger<RuntimeRequestMetricsMiddleware> logger)
    {
        _next = next;
        _store = store;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _store.IncrementTotalRequests();
        _store.IncrementActive();
        var sw = Stopwatch.StartNew();
        var errored = false;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            errored = true;
            _logger.LogDebug("Request cancelled: {Path}", context.Request.Path);
            throw;
        }
        catch (Exception ex)
        {
            errored = true;
            _store.RecordException(ex, Sanitize(ex.Message));
            _logger.LogDebug(ex, "Exception recorded for runtime metrics");
            throw;
        }
        finally
        {
            sw.Stop();
            _store.DecrementActive();
            var code = context.Response?.StatusCode ?? 0;
            if (errored && code < 400)
            {
                code = 500;
            }

            var success = code is >= 200 and <= 399;
            _store.RecordCompletion(code, sw.Elapsed.TotalMilliseconds, success);
        }
    }

    private static string Sanitize(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length > 500 ? message[..500] + "…" : message;
    }
}
