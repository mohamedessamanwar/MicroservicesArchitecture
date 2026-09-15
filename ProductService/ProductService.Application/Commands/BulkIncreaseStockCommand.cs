using MediatR;

namespace ProductService.Application.Commands;

public record BulkIncreaseStockCommand(List<BulkProductQuantity> Items) : IRequest<bool>;
