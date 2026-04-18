using MediatR;
using OrderService.Application.Common;

namespace OrderService.Application.Commands;

public record ProcessPaymentCommand(Guid OrderId) : IRequest<CommandResult<PaymentProcessResult>>;

public class PaymentProcessResult
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Message { get; init; } = string.Empty;
}