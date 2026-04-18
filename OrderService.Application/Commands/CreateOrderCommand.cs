using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;

namespace OrderService.Application.Commands;

public record CreateOrderCommand(CreateOrderDto Dto) : IRequest<CommandResult<OrderResponseDto>>;