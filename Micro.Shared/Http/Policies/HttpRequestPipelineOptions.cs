namespace Micro.Shared.Http.Policies;

public static class HttpRequestPipelineOptions
{
    public static readonly HttpRequestOptionsKey<string> PipelineKey = new("ResiliencePipeline");
}

public static class ResiliencePipelineSelector
{
    public static string Resolve(HttpRequestMessage request)
    {
        if (request.Options.TryGetValue(HttpRequestPipelineOptions.PipelineKey, out var configuredKey) &&
            !string.IsNullOrWhiteSpace(configuredKey))
        {
            return configuredKey;
        }

        return request.Method.Method.ToUpperInvariant() switch
        {
            "GET" => ResiliencePipelineKeys.Read,
            "HEAD" => ResiliencePipelineKeys.Health,
            "OPTIONS" => ResiliencePipelineKeys.Health,
            "PUT" => ResiliencePipelineKeys.Critical,
            _ => ResiliencePipelineKeys.Write,
        };
    }
}