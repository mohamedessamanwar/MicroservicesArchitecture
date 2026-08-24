using MediatR;
using ProductService.Application.Commands;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class BulkIncreaseStockCommandHandler : IRequestHandler<BulkIncreaseStockCommand, bool>
{
    private readonly IProductRepository _repository;

    public BulkIncreaseStockCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(BulkIncreaseStockCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => (i.ProductId, i.Quantity));
        return await _repository.IncreaseStockBulkAsync(items, cancellationToken);
    }
}
