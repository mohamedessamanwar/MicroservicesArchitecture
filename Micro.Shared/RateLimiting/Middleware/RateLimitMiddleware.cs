using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Micro.Shared.RateLimiting.Models;

namespace Micro.Shared.RateLimiting.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitOptions _options;

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimiter rateLimiter,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Determine Limit Key
        string limitKey;
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? context.User.FindFirst("sub")?.Value;
                         
            if (!string.IsNullOrEmpty(userId))
            {
                limitKey = $"rl:user:{userId}";
            }
            else
            {
                // Fallback if authenticated but no ID claim found
                limitKey = GetIpKey(context);
            }
        }
        else
        {
            // Anonymous requests are limited by IP
            limitKey = GetIpKey(context);
        }

        // 2. Execute Rate Limit Check
        var result = await _rateLimiter.IsAllowedAsync(limitKey, _options);

        // 3. Set Headers
        context.Response.Headers["X-RateLimit-Limit"] = _options.Capacity.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.RemainingTokens.ToString();

        // 4. Handle Denial
        if (!result.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for key: {LimitKey}", limitKey);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            
            if (result.RetryAfter.HasValue)
            {
                context.Response.Headers["Retry-After"] = result.RetryAfter.Value.TotalSeconds.ToString("F0");
            }

            // Short-circuit pipeline
            await context.Response.WriteAsync("Too many requests. Please try again later.");
            return;
        }

        // 5. Continue Pipeline
        await _next(context);
    }

    private static string GetIpKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"rl:ip:{ip}";
    }
}
