using MediatR;
using Payment.Application.Common;
using Payment.Application.DTOs;

namespace Payment.Application.Commands;

public record CreatePaymentCommand(CreatePaymentDto Dto) : IRequest<CommandResult<PaymentResponseDto>>;