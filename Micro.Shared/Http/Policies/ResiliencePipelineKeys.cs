namespace Micro.Shared.Http.Policies;

public static class ResiliencePipelineKeys
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Health = "health";
    public const string Critical = "critical";
    public const string NoRetry = "no-retry";
}