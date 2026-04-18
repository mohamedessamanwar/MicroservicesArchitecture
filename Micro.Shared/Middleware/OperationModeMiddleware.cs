using Microsoft.AspNetCore.Http;
using Micro.Shared.Persistence;

namespace Micro.Shared.Middleware;

public class OperationModeMiddleware
{
    private readonly RequestDelegate _next;

    public OperationModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        // Detects HTTP method
        // Logic: GET / HEAD / OPTIONS → Read, Others → Write
        var method = context.Request.Method.ToUpperInvariant();
        
        requestContext.OperationMode = method switch
        {
            "GET" or "HEAD" or "OPTIONS" => OperationMode.Read,
            _ => OperationMode.Write
        };

        await _next(context);
    }
}
