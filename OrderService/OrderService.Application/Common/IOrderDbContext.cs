using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderService.Domain.Entities;

namespace OrderService.Application.Common;

public interface IOrderDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderDetail> OrderDetails { get; }
    DbSet<Saga> Sagas { get; }
    DbSet<SagaStep> SagaSteps { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
