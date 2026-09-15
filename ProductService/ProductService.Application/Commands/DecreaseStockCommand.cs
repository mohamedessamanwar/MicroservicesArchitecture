using MediatR;

namespace ProductService.Application.Commands;

public record DecreaseStockCommand(Guid ProductId, int Amount) : IRequest<bool>;
