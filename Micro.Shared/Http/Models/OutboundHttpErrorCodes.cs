namespace Micro.Shared.Http.Models;

public static class OutboundHttpErrorCodes
{
    public const string TransportError = "TRANSPORT_ERROR";
    public const string DeserializationError = "DESERIALIZATION_ERROR";
    public const string DownstreamError = "DOWNSTREAM_HTTP_ERROR";
}
