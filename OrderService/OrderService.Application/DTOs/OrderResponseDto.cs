using OrderService.Domain.Entities;

namespace OrderService.Application.DTOs;

public record OrderResponseDto(Guid Id, Guid CustomerId, decimal TotalAmount, OrderStatus Status);