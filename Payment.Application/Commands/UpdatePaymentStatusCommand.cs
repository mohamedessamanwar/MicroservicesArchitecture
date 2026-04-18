using MediatR;
using Payment.Application.Common;
using Payment.Core.Entities;

namespace Payment.Application.Commands;

public record UpdatePaymentStatusCommand(
    Guid PaymentId,
    PaymentStatus Status) : IRequest<CommandResult<PaymentStatusUpdateResult>>;
public class PaymentStatusUpdateResult
{
    public Guid PaymentId { get; init; }     // Updated payment ID
    public Guid OrderId { get; init; }    // Associated order ID
    public string OldStatus { get; init; } = string.Empty;  // Previous status
    public string NewStatus { get; init; } = string.Empty;  // New status
    public bool OrderSynced { get; init; } // Whether order was successfully synced
    public string? SyncError { get; init; }  // Error message if sync failed
}