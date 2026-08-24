using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;
using Micro.Shared.Http.Clients.Common;

namespace Micro.Shared.Http.Clients.Product;

public sealed class ProductServiceClient : DownstreamApiClientBase, IProductServiceClient
{
    public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<object>> DecreaseStockAsync(
        Guid productId,
        int amount,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var options = new OutboundHttpRequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            options.Headers["X-Idempotency-Key"] = idempotencyKey;
        }

        return PostAsync<object, object>(
            endpoint: $"api/v1/products/{productId}/decrease-count?amount={amount}",
            request: new { }, // Empty body
            pipeline: ResiliencePipelineKeys.Write, // Retries with idempotency key
            useIdempotencyKey: true,
            requestOptions: options,
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<object>> IncreaseStockAsync(
        Guid productId,
        int amount,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var options = new OutboundHttpRequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            options.Headers["X-Idempotency-Key"] = idempotencyKey;
        }

        return PostAsync<object, object>(
            endpoint: $"api/v1/products/{productId}/increase-count?amount={amount}",
            request: new { }, // Empty body
            pipeline: ResiliencePipelineKeys.Write, // Retries with idempotency key
            useIdempotencyKey: true,
            requestOptions: options,
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<object>> DecreaseStockBulkAsync(
        List<ProductQuantityDto> items,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var options = new OutboundHttpRequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            options.Headers["X-Idempotency-Key"] = idempotencyKey;
        }

        return PostAsync<List<ProductQuantityDto>, object>(
            endpoint: "api/v1/products/decrease-bulk",
            request: items,
            pipeline: ResiliencePipelineKeys.Write,
            useIdempotencyKey: true,
            requestOptions: options,
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<object>> IncreaseStockBulkAsync(
        List<ProductQuantityDto> items,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var options = new OutboundHttpRequestOptions();
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            options.Headers["X-Idempotency-Key"] = idempotencyKey;
        }

        return PostAsync<List<ProductQuantityDto>, object>(
            endpoint: "api/v1/products/increase-bulk",
            request: items,
            pipeline: ResiliencePipelineKeys.Write,
            useIdempotencyKey: true,
            requestOptions: options,
            cancellationToken: cancellationToken);
    }
}
