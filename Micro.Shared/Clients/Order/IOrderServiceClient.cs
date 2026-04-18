using Micro.Shared.Clients.Order.DTOs;
using Micro.Shared.Http.Models;

namespace Micro.Shared.Clients.Order;

public interface IOrderServiceClient
{
    Task<ApiResult<object>> UpdateOrderStatusAsync(
        Guid orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}