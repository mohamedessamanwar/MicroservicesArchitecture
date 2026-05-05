using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Micro.Shared.Http.Configuration;

namespace Micro.Shared.Http.Handlers;

public class HeaderPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly OutboundCallerIdentityOptions _callerIdentity;

    public HeaderPropagationHandler(
        IHttpContextAccessor httpContextAccessor,
        OutboundCallerIdentityOptions callerIdentity)
    {
        _httpContextAccessor = httpContextAccessor;
        _callerIdentity = callerIdentity;
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
        }

        // Add default Application Identity header
        AddAppIdentityHeaders(request);

        return await base.SendAsync(request, cancellationToken);
    }

    private static void PropagateHeader(HttpContext context, HttpRequestMessage request, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var values) && !request.Headers.Contains(headerName))
        {
            request.Headers.Add(headerName, values.ToArray());
        }
    }

    private void AddAppIdentityHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_callerIdentity.AppId) &&
            !request.Headers.Contains(_callerIdentity.AppIdHeaderName))
        {
            request.Headers.Add(_callerIdentity.AppIdHeaderName, _callerIdentity.AppId);
        }

        if (!_callerIdentity.EnableSignature || string.IsNullOrWhiteSpace(_callerIdentity.SharedSecret))
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        var payload = $"{_callerIdentity.AppId}\n{request.Method.Method}\n{path}\n{timestamp}";
        var signature = ComputeSignature(_callerIdentity.SharedSecret, payload);

        if (!request.Headers.Contains(_callerIdentity.TimestampHeaderName))
        {
            request.Headers.Add(_callerIdentity.TimestampHeaderName, timestamp);
        }

        if (!request.Headers.Contains(_callerIdentity.SignatureHeaderName))
        {
            request.Headers.Add(_callerIdentity.SignatureHeaderName, signature);
        }
    }

    private static string ComputeSignature(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
