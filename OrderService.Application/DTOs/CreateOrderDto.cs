namespace OrderService.Application.DTOs;

public record CreateOrderDto(Guid CustomerId, decimal TotalAmount);