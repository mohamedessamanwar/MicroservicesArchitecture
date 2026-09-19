using MediatR;

namespace ProductService.Application.Commands;

public record AddProductCommand(string Name, decimal Price, int StockCount) : IRequest<Guid>;
