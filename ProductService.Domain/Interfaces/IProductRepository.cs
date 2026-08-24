using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> DecreaseStockAsync(Guid id, int amount, CancellationToken cancellationToken = default);
    Task<bool> IncreaseStockAsync(Guid id, int amount, CancellationToken cancellationToken = default);
    Task<bool> DecreaseStockBulkAsync(IEnumerable<(Guid ProductId, int Amount)> items, CancellationToken cancellationToken = default);
    Task<bool> IncreaseStockBulkAsync(IEnumerable<(Guid ProductId, int Amount)> items, CancellationToken cancellationToken = default);
}
