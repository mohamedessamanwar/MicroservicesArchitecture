using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;
using Micro.Shared.Http.Clients.Common;
using Micro.Shared.Http.Clients.Order.DTOs;

namespace Micro.Shared.Http.Clients.Order;

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