using Payment.Core.Entities;

namespace Payment.Application.DTOs;

public record PaymentResponseDto(Guid Id, Guid OrderId, decimal Amount, PaymentStatus Status);