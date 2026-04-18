using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class EfPaymentRepository : EfGenericRepository<Core.Entities.Payment>, IPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public EfPaymentRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Core.Entities.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
         .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }
}