using MediatR;

namespace ProductService.Application.Commands;

public record IncreaseStockCommand(Guid ProductId, int Amount) : IRequest<bool>;
