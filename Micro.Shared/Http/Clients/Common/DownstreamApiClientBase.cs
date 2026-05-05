using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;

namespace Micro.Shared.Http.Clients.Common;

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
        OutboundHttpRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Get, endpoint, null, pipeline, false, requestOptions, cancellationToken);
    }

    protected Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string pipeline,
        bool useIdempotencyKey,
        OutboundHttpRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, endpoint, request, pipeline, useIdempotencyKey, requestOptions, cancellationToken);
    }

    protected Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        string pipeline,
        OutboundHttpRequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, endpoint, request, pipeline, false, requestOptions, cancellationToken);
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string endpoint,
        object? body,
        string pipeline,
        bool useIdempotencyKey,
        OutboundHttpRequestOptions? requestOptions,
        CancellationToken cancellationToken)
    {
        using var outboundRequest = new HttpRequestMessage(method, endpoint);
        outboundRequest.Options.Set(HttpRequestPipelineOptions.PipelineKey, pipeline);

        if (body != null)
        {
            outboundRequest.Content = JsonContent.Create(body, options: JsonOptions);
        }

        ApplyCustomHeaders(outboundRequest, requestOptions);

        if (method == HttpMethod.Post && (useIdempotencyKey || requestOptions?.UseIdempotencyKey == true))
        {
            outboundRequest.Headers.TryAddWithoutValidation("X-Idempotency-Key", Guid.NewGuid().ToString("N"));
        }

        var start = Stopwatch.GetTimestamp();

        try
        {
            using var response = await _httpClient.SendAsync(outboundRequest, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var rawBody = await ReadRawBodyAsync(response, cancellationToken);

            _logger.LogInformation(
                "Outbound HTTP {Method} {Path} via {Pipeline} returned {StatusCode} in {ElapsedMs}ms",
                method.Method,
                endpoint,
                pipeline,
                (int)response.StatusCode,
                elapsedMs);

            if (response.IsSuccessStatusCode)
            {
                if (TryDeserialize<TResponse>(rawBody, out var data))
                {
                    return ApiResult<TResponse>.Ok(data, (int)response.StatusCode, rawBody);
                }

                _logger.LogWarning(
                    "Outbound HTTP {Method} {Path} via {Pipeline} returned {StatusCode} but failed to deserialize response.",
                    method.Method,
                    endpoint,
                    pipeline,
                    (int)response.StatusCode);

                return ApiResult<TResponse>.Fail(
                    OutboundHttpErrorCodes.DeserializationError,
                    "Failed to deserialize downstream response.",
                    (int)response.StatusCode,
                    rawBody: rawBody,
                    transportSuccess: true);
            }

            _logger.LogWarning(
                "Outbound HTTP {Method} {Path} via {Pipeline} returned non-success {StatusCode}.",
                method.Method,
                endpoint,
                pipeline,
                (int)response.StatusCode);

            _logger.LogDebug(
                "Outbound HTTP {Method} {Path} raw body: {RawBody}",
                method.Method,
                endpoint,
                rawBody ?? string.Empty);

            return ApiResult<TResponse>.Fail(
                OutboundHttpErrorCodes.DownstreamError,
                "Downstream request failed.",
                (int)response.StatusCode,
                rawBody: rawBody,
                transportSuccess: true);
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

    private static void ApplyCustomHeaders(HttpRequestMessage request, OutboundHttpRequestOptions? requestOptions)
    {
        if (requestOptions == null)
        {
            return;
        }

        foreach (var header in requestOptions.Headers)
        {
            if (!request.Headers.Contains(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

     
    }

    private static async Task<string?> ReadRawBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content == null)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    private static bool TryDeserialize<TResponse>(string? rawBody, out TResponse data)
    {
        data = default!;

        if (typeof(TResponse) == typeof(string))
        {
            data = (TResponse)(object)(rawBody ?? string.Empty);
            return true;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return true;
        }

        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(rawBody, JsonOptions);
            if (result == null)
            {
                return false;
            }

            data = result;
            return true;
        }
        catch
        {
            return false;
        }
    }
}