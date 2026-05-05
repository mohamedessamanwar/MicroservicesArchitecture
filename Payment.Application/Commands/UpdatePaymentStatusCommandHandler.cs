using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common;
using Payment.Application.Interfaces;
using Payment.Core.Entities;
using Micro.Shared.Http.Clients.Order;
using Micro.Shared.Http.Clients.Order.DTOs;

namespace Payment.Application.Commands;

public class UpdatePaymentStatusCommandHandler
    : IRequestHandler<UpdatePaymentStatusCommand, CommandResult<PaymentStatusUpdateResult>>
{
    private readonly IPaymentRepository _paymentRepository;  // Repository for payments
    private readonly IOrderServiceClient _orderService;  // HTTP client to Order Service
    private readonly ILogger<UpdatePaymentStatusCommandHandler> _logger;  // Logger
    public UpdatePaymentStatusCommandHandler(
        IPaymentRepository paymentRepository,
        IOrderServiceClient orderService,
        ILogger<UpdatePaymentStatusCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _orderService = orderService;
        _logger = logger;
    }
    public async Task<CommandResult<PaymentStatusUpdateResult>> Handle(
        UpdatePaymentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found", request.PaymentId);
            return CommandResult<PaymentStatusUpdateResult>.Fail(new[] { "Payment not found" }, "Payment not found");
        }
        var oldStatus = payment.Status;
        payment.Status = request.Status;
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} status updated: {OldStatus} -> {NewStatus}",
            payment.Id, oldStatus, payment.Status);
        bool orderSynced = false;
        string? syncError = null;

        if (payment.Status == PaymentStatus.Completed ||
            payment.Status == PaymentStatus.Failed)
        {
            _logger.LogInformation(
                "Synchronizing order {OrderId} status based on payment status",
                payment.OrderId);
            var orderStatus = payment.Status == PaymentStatus.Completed
                ? OrderStatus.Paid     // Payment completed -> Order paid
                : OrderStatus.Failed;  // Payment failed -> Order failed

            var orderRequest = new UpdateOrderStatusRequest(orderStatus);
            var orderResult = await _orderService.UpdateOrderStatusAsync(
                payment.OrderId,
                orderRequest,
                cancellationToken);

            if (!orderResult.Success)
            {
                _logger.LogWarning(
                    "Failed to synchronize order {OrderId}: {ErrorCode} - {ErrorMessage}",
                    payment.OrderId, orderResult.ErrorCode, orderResult.ErrorMessage);

                syncError = $"{orderResult.ErrorCode}: {orderResult.ErrorMessage}";
            }
            else
            {
                orderSynced = true;
                _logger.LogInformation(
                    "Successfully synchronized order {OrderId} status to {OrderStatus}",
                    payment.OrderId, orderStatus);
            }
        }
        var result = new PaymentStatusUpdateResult
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            OldStatus = oldStatus.ToString(),
            NewStatus = payment.Status.ToString(),
            OrderSynced = orderSynced,
            SyncError = syncError
        };

        return CommandResult<PaymentStatusUpdateResult>.Ok(result, "Payment status updated successfully");
    }
}