namespace OrderService.Application.DTOs;

public record OrderItemDto(Guid ProductId, int Quantity);
public record CreateOrderDto(Guid CustomerId, List<OrderItemDto> Items, decimal TotalAmount);