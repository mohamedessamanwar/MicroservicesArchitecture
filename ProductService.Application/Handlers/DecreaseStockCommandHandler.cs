using MediatR;
using ProductService.Application.Commands;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class DecreaseStockCommandHandler : IRequestHandler<DecreaseStockCommand, bool>
{
    private readonly IProductRepository _repository;

    public DecreaseStockCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DecreaseStockCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DecreaseStockAsync(request.ProductId, request.Amount, cancellationToken);
    }
}
