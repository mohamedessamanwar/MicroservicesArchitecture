namespace Payment.Application.DTOs;

public record CreatePaymentDto(Guid OrderId, decimal Amount);