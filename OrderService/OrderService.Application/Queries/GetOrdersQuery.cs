using MediatR;
using OrderService.Application.Common;
using OrderService.Application.DTOs;

namespace OrderService.Application.Queries;

public record GetOrdersQuery : IRequest<CommandResult<IEnumerable<OrderResponseDto>>>;
