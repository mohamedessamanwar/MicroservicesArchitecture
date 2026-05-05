namespace Micro.Shared.Http.Models;

public sealed class OutboundHttpRequestOptions
{
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public string? AppIdOverride { get; set; }
    public bool UseIdempotencyKey { get; set; }

    public OutboundHttpRequestOptions AddHeader(string name, string value)
    {
        Headers[name] = value;
        return this;
    }
}
