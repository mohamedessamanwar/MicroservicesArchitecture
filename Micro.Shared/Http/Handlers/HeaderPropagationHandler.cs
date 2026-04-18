using Microsoft.AspNetCore.Http;

namespace Micro.Shared.Http.Handlers;

public class HeaderPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _appId;

    public HeaderPropagationHandler(
        IHttpContextAccessor httpContextAccessor, 
        string appId)
    {
        _httpContextAccessor = httpContextAccessor;
        _appId = appId;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;

        // Propagate headers from incoming request if context exists
        if (context != null)
        {
            PropagateHeader(context, request, "Authorization");
            PropagateHeader(context, request, "X-Correlation-Id");
            PropagateHeader(context, request, "X-Country");
            PropagateHeader(context, request, "traceparent");
            PropagateHeader(context, request, "tracestate");
            PropagateHeader(context, request, "baggage");
        }

        // Add default Application Identity header
        if (!request.Headers.Contains("X-App-Id"))
        {
            request.Headers.Add("X-App-Id", _appId);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static void PropagateHeader(HttpContext context, HttpRequestMessage request, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var values) && !request.Headers.Contains(headerName))
        {
            request.Headers.Add(headerName, values.ToArray());
        }
    }
}
