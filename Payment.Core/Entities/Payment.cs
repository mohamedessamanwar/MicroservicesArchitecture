using Payment.Core.Interfaces;

namespace Payment.Core.Entities;

public class Payment : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}