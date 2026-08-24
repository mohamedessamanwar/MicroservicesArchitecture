using MediatR;
using ProductService.Application.Commands;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class IncreaseStockCommandHandler : IRequestHandler<IncreaseStockCommand, bool>
{
    private readonly IProductRepository _repository;

    public IncreaseStockCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(IncreaseStockCommand request, CancellationToken cancellationToken)
    {
        return await _repository.IncreaseStockAsync(request.ProductId, request.Amount, cancellationToken);
    }
}
