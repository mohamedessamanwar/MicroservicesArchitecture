using System.Diagnostics;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.Middleware;

public class GatewayLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;

    public GatewayLoggingMiddleware(RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();
        var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        try
        {
            await _next(context);

            sw.Stop();
            var statusCode = context.Response.StatusCode;

            // Extract YARP proxy features if available
            var proxyFeature = context.GetReverseProxyFeature();
            var routeId = proxyFeature?.Route?.Config?.RouteId ?? "Unknown";
            var clusterId = proxyFeature?.Cluster?.Config?.ClusterId ?? "Unknown";
            var destinationAddress = proxyFeature?.ProxiedDestination?.Model?.Config?.Address ?? "Unknown";

            _logger.LogInformation(
                "GatewayRequest | CorrelationId={CorrelationId} | Method={Method} | Path={Path}{QueryString} | StatusCode={StatusCode} | DurationMs={DurationMs} | RouteId={RouteId} | ClusterId={ClusterId} | Destination={Destination} | ClientIP={ClientIP} | UserAgent={UserAgent}",
                correlationId,
                method,
                path,
                queryString,
                statusCode,
                sw.ElapsedMilliseconds,
                routeId,
                clusterId,
                destinationAddress,
                clientIp,
                userAgent);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var proxyFeature = context.GetReverseProxyFeature();
            var routeId = proxyFeature?.Route?.Config?.RouteId ?? "Unknown";
            var clusterId = proxyFeature?.Cluster?.Config?.ClusterId ?? "Unknown";
            
            _logger.LogError(
                ex,
                "GatewayRequestFailed | CorrelationId={CorrelationId} | Method={Method} | Path={Path}{QueryString} | DurationMs={DurationMs} | RouteId={RouteId} | ClusterId={ClusterId} | FailureReason={FailureReason}",
                correlationId,
                method,
                path,
                queryString,
                sw.ElapsedMilliseconds,
                routeId,
                clusterId,
                ex.Message);
            
            throw; // Re-throw so standard exception handler can process it
        }
    }
}
