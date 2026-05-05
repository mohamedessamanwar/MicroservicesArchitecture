namespace Micro.Shared.Http.Configuration;

public sealed class OutboundCallerIdentityOptions
{
    public string AppId { get; set; } = string.Empty;
    public string? SharedSecret { get; set; }
    public string AppIdHeaderName { get; set; } = "X-App-Id";
    public string SignatureHeaderName { get; set; } = "X-App-Signature";
    public string TimestampHeaderName { get; set; } = "X-App-Timestamp";
    public bool EnableSignature { get; set; } = false;
}
