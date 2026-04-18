namespace Micro.Shared.Clients.Payment.DTOs;

public record CreatePaymentRequest(Guid OrderId, decimal Amount);

public record PaymentDto(Guid Id, Guid OrderId, decimal Amount, PaymentStatus Status);

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
}