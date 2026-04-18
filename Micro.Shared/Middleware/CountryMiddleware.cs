using Microsoft.AspNetCore.Http;
using Micro.Shared.Persistence;

namespace Micro.Shared.Middleware;

public class CountryMiddleware
{
    private readonly RequestDelegate _next;

    public CountryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        // Reads country from header: "X-Country"
        if (context.Request.Headers.TryGetValue("X-Country", out var countryHeader))
        {
            requestContext.Country = countryHeader.ToString();
        }
        else
        {
            // Default to "Egypt"
            requestContext.Country = "Egypt";
        }

        await _next(context);
    }
}
