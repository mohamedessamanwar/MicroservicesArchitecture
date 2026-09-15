using OrderService.Domain.Interfaces;

namespace OrderService.Domain.Entities;

public class OrderDetail : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}
