namespace OrderService.Domain.Interfaces;

public interface IBaseEntity
{
    Guid Id { get; set; }
    DateTime? Created { get; set; }
    DateTime? Modified { get; set; }
}