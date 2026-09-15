using OrderService.Application.Common;
using OrderService.Application.DTOs;

namespace OrderService.Application.UseCases;

public interface ICreateOrderUseCase
{
    Task<CommandResult<OrderResponseDto>> ExecuteAsync(CreateOrderDto dto, string idempotencyKey, CancellationToken cancellationToken);
}
