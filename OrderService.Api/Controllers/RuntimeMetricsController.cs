using Micro.Shared.Http.Models;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Runtime;

namespace OrderService.Api.Controllers;

/// <summary>
/// Process and ASP.NET Core runtime diagnostics (per instance only).
/// TODO: Require authenticated admin users (e.g. [Authorize(Policy = "AdminOnly")]) before any production exposure.
/// </summary>
[ApiController]
[Route("api/order/runtime")]
public sealed class RuntimeMetricsController : ControllerBase
{
    private readonly IRuntimeMetricsService _metrics;
    private readonly RuntimeMetricsStore _store;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RuntimeMetricsController> _logger;

    public RuntimeMetricsController(
        IRuntimeMetricsService metrics,
        RuntimeMetricsStore store,
        IWebHostEnvironment environment,
        ILogger<RuntimeMetricsController> logger)
    {
        _metrics = metrics;
        _store = store;
        _environment = environment;
        _logger = logger;
    }

    private IActionResult? Guard()
    {
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            return StatusCode(
                403,
                ApiResult<object>.Fail(
                    "RUNTIME_METRICS_FORBIDDEN",
                    "Runtime metrics are only enabled in Development or Staging.",
                    403));
        }

        return null;
    }

    [HttpGet("summary")]
    public IActionResult Summary()
    {
        var d = Guard();
        return d ?? Ok(ApiResult<RuntimeSummaryDto>.Ok(_metrics.GetSummary()));
    }

    [HttpGet("metrics")]
    public IActionResult Metrics()
    {
        var d = Guard();
        return d ?? Ok(ApiResult<RuntimeMetricsDto>.Ok(_metrics.GetFullMetrics(_store)));
    }

    [HttpGet("process")]
    public IActionResult Process() => Guard() ?? Ok(ApiResult<ProcessMetricsDto>.Ok(_metrics.GetProcess()));

    [HttpGet("memory")]
    public IActionResult Memory() => Guard() ?? Ok(ApiResult<MemoryMetricsDto>.Ok(_metrics.GetMemory()));

    [HttpGet("gc")]
    public IActionResult Gc() => Guard() ?? Ok(ApiResult<GcMetricsDto>.Ok(_metrics.GetGc()));

    [HttpGet("threadpool")]
    public IActionResult ThreadPool() => Guard() ?? Ok(ApiResult<ThreadPoolMetricsDto>.Ok(_metrics.GetThreadPool()));

    [HttpGet("cpu")]
    public IActionResult Cpu() => Guard() ?? Ok(ApiResult<CpuMetricsDto>.Ok(_metrics.GetCpu()));

    [HttpGet("requests")]
    public IActionResult Requests() => Guard() ?? Ok(ApiResult<RequestMetricsDto>.Ok(_metrics.GetRequests(_store)));

    [HttpGet("exceptions")]
    public IActionResult Exceptions() => Guard() ?? Ok(ApiResult<ExceptionMetricsDto>.Ok(_metrics.GetExceptions(_store)));

    [HttpGet("latency")]
    public IActionResult Latency() => Guard() ?? Ok(ApiResult<LatencyMetricsDto>.Ok(_metrics.GetLatency(_store)));

    [HttpGet("allocations")]
    public IActionResult Allocations() => Guard() ?? Ok(ApiResult<AllocationMetricsDto>.Ok(_metrics.GetAllocations()));

    [HttpGet("health-risk")]
    public IActionResult HealthRisk() => Guard() ?? Ok(ApiResult<HealthRiskDto>.Ok(_metrics.GetHealthRisk(_store)));

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        var d = Guard();
        if (d is not null)
        {
            return d;
        }

        _store.ResetVolatile();
        _logger.LogWarning("RuntimeMetricsStore counters were reset via POST /api/order/runtime/reset");
        return Ok(ApiResult<object>.Ok(new { reset = true, warning = "Only in-memory counters were cleared; process/GC counters are unchanged." }));
    }
}
