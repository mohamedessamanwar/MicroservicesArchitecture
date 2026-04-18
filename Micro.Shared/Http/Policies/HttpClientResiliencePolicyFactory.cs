using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Configuration;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Micro.Shared.Http.Policies;

internal static class HttpClientResiliencePolicyFactory
{
    private static readonly ConcurrentDictionary<string, IAsyncPolicy<HttpResponseMessage>> Cache = new();

    public static IAsyncPolicy<HttpResponseMessage> GetOrCreate(
        string clientName,
        string pipelineKey,
        DownstreamHttpClientOptions options,
        ILogger logger)
    {
        var cacheKey = $"{clientName}:{pipelineKey}";
        return Cache.GetOrAdd(cacheKey, _ => BuildPolicy(clientName, pipelineKey, options, logger));
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildPolicy(
        string clientName,
        string pipelineKey,
        DownstreamHttpClientOptions options,
        ILogger logger)
    {
        var pipelineSettings = pipelineKey switch
        {
            ResiliencePipelineKeys.Read => options.Pipelines.Read,
            ResiliencePipelineKeys.Health => options.Pipelines.Health,
            ResiliencePipelineKeys.Critical => options.Pipelines.Critical,
            ResiliencePipelineKeys.NoRetry => options.Pipelines.NoRetry,
            _ => options.Pipelines.Write,
        };

        var policies = new List<IAsyncPolicy<HttpResponseMessage>>
        {
            BuildBulkheadPolicy(options, pipelineSettings),
        };

        if (pipelineSettings.EnableCircuitBreaker)
        {
            policies.Add(BuildCircuitBreakerPolicy(clientName, pipelineKey, pipelineSettings, logger));
        }

        if (pipelineSettings.EnableRetry && pipelineSettings.RetryAttempts > 0)
        {
            policies.Add(BuildRetryPolicy(clientName, pipelineKey, pipelineSettings, logger));
        }

        policies.Add(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(pipelineSettings.TimeoutSeconds)));

        return Policy.WrapAsync(policies.ToArray());
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildBulkheadPolicy(
        DownstreamHttpClientOptions options,
        ResiliencePipelineSettings pipelineSettings)
    {
        var maxParallel = pipelineSettings.UseSharedBulkhead
            ? options.MaxParallelRequests
            : pipelineSettings.MaxParallelRequestsOverride ?? options.MaxParallelRequests;

        var maxQueue = pipelineSettings.UseSharedBulkhead
            ? options.MaxQueuedRequests
            : pipelineSettings.MaxQueuedRequestsOverride ?? options.MaxQueuedRequests;

        return Policy.BulkheadAsync<HttpResponseMessage>(maxParallel, maxQueue);
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy(
        string clientName,
        string pipelineKey,
        ResiliencePipelineSettings settings,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .OrResult(response => response.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                retryCount: settings.RetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(settings.RetryBaseDelayMilliseconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, delay, retryAttempt, _) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "unknown";
                    logger.LogWarning(
                        "Outbound retry {RetryAttempt}/{RetryAttempts} for {ClientName} pipeline {PipelineKey}. Reason={Reason}, DelayMs={DelayMs}",
                        retryAttempt,
                        settings.RetryAttempts,
                        clientName,
                        pipelineKey,
                        reason,
                        delay.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(
        string clientName,
        string pipelineKey,
        ResiliencePipelineSettings settings,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .OrResult(response => response.StatusCode == (HttpStatusCode)429)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: settings.CircuitBreakerFailuresBeforeBreak,
                durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreakDurationSeconds),
                onBreak: (outcome, breakDelay) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "unknown";
                    logger.LogError(
                        "Circuit opened for {ClientName} pipeline {PipelineKey}. BreakSeconds={BreakSeconds}, Reason={Reason}",
                        clientName,
                        pipelineKey,
                        breakDelay.TotalSeconds,
                        reason);
                },
                onReset: () => logger.LogInformation(
                    "Circuit reset for {ClientName} pipeline {PipelineKey}",
                    clientName,
                    pipelineKey),
                onHalfOpen: () => logger.LogInformation(
                    "Circuit half-open for {ClientName} pipeline {PipelineKey}",
                    clientName,
                    pipelineKey));
    }
}