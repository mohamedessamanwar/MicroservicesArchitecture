using Microsoft.AspNetCore.Mvc;
using MediatR;
using Payment.Application.Commands;
using Payment.Application.DTOs;
using Payment.Application.Common;
using Payment.Core.Entities;
using Micro.Shared.Http.Idempotency;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [Idempotent(ExpirationHours = 1)]
    [ProducesResponseType(typeof(ApiResult<PaymentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResult<PaymentResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
    {
        var command = new CreatePaymentCommand(dto);
        var result = await _mediator.Send(command);

        var apiResult = new ApiResult<PaymentResponseDto>
        {
            Success = result.Success,
            Message = result.Message,
            Data = result.Data,
            Errors = result.Errors
        };

        if (!result.Success)
        {
            return BadRequest(apiResult);
        }

        return CreatedAtAction(nameof(Create), apiResult);
    }

    [HttpPost("test-resilience")]
    [ProducesResponseType(typeof(ApiResult<PaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<PaymentResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestResilience([FromBody] Micro.Shared.Clients.Payment.DTOs.TestResilienceRequest request)
    {
        _logger.LogInformation("TestResilience called with Delay={Delay}ms, StatusCode={StatusCode}",
            request.DelayMilliseconds, request.StatusCode);

        if (request.DelayMilliseconds > 0)
        {
            await Task.Delay(request.DelayMilliseconds);
        }

        if (request.StatusCode >= 400)
        {
            return StatusCode(request.StatusCode, new ApiResult<PaymentResponseDto>
            {
                Success = false,
                Message = $"Simulated failure with status code {request.StatusCode}"
            });
        }

        return Ok(new ApiResult<PaymentResponseDto>
        {
            Success = true,
            Message = "Resilience test successful",
            Data = new PaymentResponseDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100,
                PaymentStatus.Completed)
        });
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResult<PaymentStatusUpdateResult>), 200)]
    [ProducesResponseType(typeof(ApiResult<PaymentStatusUpdateResult>), 400)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] PaymentStatus status, CancellationToken ct)
    {
        var command = new UpdatePaymentStatusCommand(id, status);
        var result = await _mediator.Send(command, ct);

        var apiResult = new ApiResult<PaymentStatusUpdateResult>
        {
            Success = result.Success,
            Message = result.Message,
            Data = result.Data,
            Errors = result.Errors
        };

        if (!result.Success)
        {
            return BadRequest(apiResult);
        }

        if (result.Data?.OrderSynced == false)
        {
            _logger.LogWarning("Payment {PaymentId} updated but order sync failed: {Error}", id, result.Data.SyncError);
            apiResult.Message = $"Payment updated, but order synchronization failed: {result.Data.SyncError}";
        }

        return Ok(apiResult);
    }
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}