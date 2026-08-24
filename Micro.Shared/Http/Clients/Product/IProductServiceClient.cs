using Micro.Shared.Http.Models;

namespace Micro.Shared.Http.Clients.Product;

public interface IProductServiceClient
{
    Task<ApiResult<object>> DecreaseStockAsync(Guid productId, int amount, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<object>> IncreaseStockAsync(Guid productId, int amount, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<object>> DecreaseStockBulkAsync(List<ProductQuantityDto> items, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<ApiResult<object>> IncreaseStockBulkAsync(List<ProductQuantityDto> items, string? idempotencyKey = null, CancellationToken cancellationToken = default);
}

public record ProductQuantityDto(Guid ProductId, int Quantity);
