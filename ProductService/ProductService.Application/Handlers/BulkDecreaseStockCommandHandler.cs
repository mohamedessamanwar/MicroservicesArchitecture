using MediatR;
using ProductService.Application.Commands;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class BulkDecreaseStockCommandHandler : IRequestHandler<BulkDecreaseStockCommand, bool>
{
    private readonly IProductRepository _repository;

    public BulkDecreaseStockCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(BulkDecreaseStockCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => (i.ProductId, i.Quantity));
        return await _repository.DecreaseStockBulkAsync(items, cancellationToken);
    }
}
