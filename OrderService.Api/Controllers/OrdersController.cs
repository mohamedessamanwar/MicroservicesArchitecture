using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using Micro.Shared.Http.Models;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IMediator mediator,
        ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<OrderResponseDto>>), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var query = new GetOrdersQuery();
        var result = await _mediator.Send(query, ct);

        if (!result.Success)
        {
            return BadRequest(ApiResult<IEnumerable<OrderResponseDto>>.Fail(
                "ORDERS_RETRIEVAL_FAILED",
                result.Message ?? "Failed to retrieve orders",
                400));
        }

        return Ok(ApiResult<IEnumerable<OrderResponseDto>>.Ok(result.Data!));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<OrderResponseDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<OrderResponseDto>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var command = new CreateOrderCommand(dto);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(ApiResult<OrderResponseDto>.Fail(
                "ORDER_CREATION_FAILED",
                result.Message ?? "Failed to create order",
                400,
                result.Errors != null ? new Dictionary<string, object> { ["Errors"] = result.Errors } : null));
        }

        return StatusCode(201, ApiResult<OrderResponseDto>.Ok(result.Data!, 201));
    }

    [HttpPost("{id}/process-payment")]
    [ProducesResponseType(typeof(ApiResult<PaymentProcessResult>), 200)]
    [ProducesResponseType(typeof(ApiResult<PaymentProcessResult>), 400)]
    public async Task<IActionResult> ProcessPayment(
        Guid id,
        CancellationToken ct)
    {
        var command = new ProcessPaymentCommand(id);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(ApiResult<PaymentProcessResult>.Fail(
                "PAYMENT_PROCESSING_FAILED",
                result.Message ?? "Failed to process payment",
                400,
                result.Errors != null ? new Dictionary<string, object> { ["Errors"] = result.Errors } : null));
        }

        return Ok(ApiResult<PaymentProcessResult>.Ok(result.Data!));
    }

    [HttpPost("test-payment-resilience")]
    public async Task<IActionResult> TestPaymentResilience(
        [FromServices] Micro.Shared.Clients.Payment.IPaymentServiceClient paymentServiceClient,
        [FromBody] Micro.Shared.Clients.Payment.DTOs.TestResilienceRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Testing payment resilience with Delay={Delay}ms, StatusCode={StatusCode}",
            request.DelayMilliseconds, request.StatusCode);

        var result = await paymentServiceClient.TestResilienceAsync(request, ct);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode ?? 500, result);
        }

        return Ok(result);
    }
}