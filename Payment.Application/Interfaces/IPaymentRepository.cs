namespace Payment.Application.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment.Core.Entities.Payment>
{
    Task<Payment.Core.Entities.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}