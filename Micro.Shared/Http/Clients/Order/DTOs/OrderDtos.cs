namespace Micro.Shared.Http.Clients.Order.DTOs;

public record UpdateOrderStatusRequest(OrderStatus Status);

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
    Failed = 3,
}