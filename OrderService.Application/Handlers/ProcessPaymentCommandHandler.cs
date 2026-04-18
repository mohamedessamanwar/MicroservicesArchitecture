using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Entities;
using Micro.Shared.Clients.Payment;
using Micro.Shared.Clients.Payment.DTOs;
using OrderService.Application.Commands;

namespace OrderService.Application.Handlers;

public class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, CommandResult<PaymentProcessResult>>
{
    private readonly IRepository<Order> _repository;  // Unified repository
    private readonly IPaymentServiceClient _paymentService;  // HTTP client to Payment Service
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;  // Logger
    
    public ProcessPaymentCommandHandler(
        IRepository<Order> repository,
        IPaymentServiceClient paymentService,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _repository = repository;
        _paymentService = paymentService;
        _logger = logger;
    }
    
    public async Task<CommandResult<PaymentProcessResult>> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.OrderId);
            return CommandResult<PaymentProcessResult>.Fail("Order not found");
        }

        _logger.LogInformation(
            "Processing payment for order {OrderId}, amount: {Amount}",
            order.Id, order.TotalAmount);
            
        var paymentRequest = new CreatePaymentRequest(
            OrderId: order.Id,
            Amount: order.TotalAmount);

        var paymentResult = await _paymentService.CreatePaymentAsync(
            paymentRequest,
            cancellationToken);
            
        if (!paymentResult.Success)
        {
            _logger.LogError(
                "Payment creation failed for order {OrderId}: {ErrorCode} - {ErrorMessage}",
                order.Id, paymentResult.ErrorCode, paymentResult.ErrorMessage);

            return CommandResult<PaymentProcessResult>.Fail(
                $"Payment service error: {paymentResult.ErrorMessage}");
        }
        
        var payment = paymentResult.Data!;

        order.Status = OrderStatus.Paid;  // Update order to Paid
        order.Modified = DateTime.UtcNow;  // Update timestamp
        await _repository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} created for order {OrderId}",
            payment.Id, order.Id);
            
        var result = new PaymentProcessResult
        {
            OrderId = order.Id,
            PaymentId = payment.Id,
            PaymentStatus = payment.Status.ToString(),
            Amount = payment.Amount,
            Message = "Payment processing initiated successfully"
        };

        return CommandResult<PaymentProcessResult>.Ok(result);
    }
}