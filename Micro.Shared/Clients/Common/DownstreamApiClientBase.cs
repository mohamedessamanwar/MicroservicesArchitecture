using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;

namespace Micro.Shared.Clients.Common;

public abstract class DownstreamApiClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    protected DownstreamApiClientBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    protected Task<ApiResult<TResponse>> GetAsync<TResponse>(
        string endpoint,
        string pipeline,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Get, endpoint, null, pipeline, false, cancellationToken);
    }

    protected Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string pipeline,
        bool useIdempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, endpoint, request, pipeline, useIdempotencyKey, cancellationToken);
    }

    protected Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string pipeline,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, endpoint, request, pipeline, false, cancellationToken);
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string endpoint,
        object? body,
        string pipeline,
        bool useIdempotencyKey,
        CancellationToken cancellationToken)
    {
        using var outboundRequest = new HttpRequestMessage(method, endpoint);
        outboundRequest.Options.Set(HttpRequestPipelineOptions.PipelineKey, pipeline);

        if (body != null)
        {
            outboundRequest.Content = JsonContent.Create(body, options: JsonOptions);
        }

        if (method == HttpMethod.Post && useIdempotencyKey)
        {
            outboundRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", Guid.NewGuid().ToString("N"));
        }

        var start = Stopwatch.GetTimestamp();

        try
        {
            using var response = await _httpClient.SendAsync(outboundRequest, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            _logger.LogInformation(
                "Outbound HTTP {Method} {Path} via {Pipeline} returned {StatusCode} in {ElapsedMs}ms",
                method.Method,
                endpoint,
                pipeline,
                (int)response.StatusCode,
                elapsedMs);

            var envelope = await TryReadEnvelopeAsync<TResponse>(response, cancellationToken);

            if (response.IsSuccessStatusCode && envelope?.Success == true)
            {
                return ApiResult<TResponse>.Ok(envelope.Data!, (int)response.StatusCode);
            }

            var errorCode = envelope?.ErrorCode ?? $"HTTP_{(int)response.StatusCode}";
            var errorMessage = envelope?.ErrorMessage ?? envelope?.Message ?? "Downstream request failed";

            return ApiResult<TResponse>.Fail(
                errorCode,
                errorMessage,
                (int)response.StatusCode,
                envelope?.Errors != null
                    ? new Dictionary<string, object> { ["Errors"] = envelope.Errors }
                    : null);
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            _logger.LogError(
                ex,
                "Outbound HTTP {Method} {Path} via {Pipeline} failed after {ElapsedMs}ms",
                method.Method,
                endpoint,
                pipeline,
                elapsedMs);

            return ApiResult<TResponse>.Fail(ex, 500);
        }
    }

    private static async Task<DownstreamApiEnvelope<TResponse>?> TryReadEnvelopeAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<DownstreamApiEnvelope<TResponse>>(JsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed record DownstreamApiEnvelope<T>(
        bool Success,
        T? Data,
        string? ErrorCode,
        string? ErrorMessage,
        string? Message,
        IEnumerable<string>? Errors);
}