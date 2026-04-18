using OrderService.Domain.Interfaces;

namespace OrderService.Domain.Entities;

public class Order : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}