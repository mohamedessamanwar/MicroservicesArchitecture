using Microsoft.Extensions.Logging;
using Micro.Shared.Clients.Common;
using Micro.Shared.Clients.Order.DTOs;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;

namespace Micro.Shared.Clients.Order;

public sealed class OrderServiceClient : DownstreamApiClientBase, IOrderServiceClient
{
    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<object>> UpdateOrderStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        // Order/payment status synchronization is business-critical and can use a stronger pipeline.
        return PutAsync<UpdateOrderStatusRequest, object>(
            endpoint: $"api/v1/orders/{orderId}/status",
            request: request,
            pipeline: ResiliencePipelineKeys.Critical,
            cancellationToken: cancellationToken);
    }
}