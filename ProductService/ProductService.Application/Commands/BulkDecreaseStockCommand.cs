using MediatR;

namespace ProductService.Application.Commands;

public record BulkProductQuantity(Guid ProductId, int Quantity);

public record BulkDecreaseStockCommand(List<BulkProductQuantity> Items) : IRequest<bool>;
