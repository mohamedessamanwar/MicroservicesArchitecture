using ProductService.Domain.Interfaces;

namespace ProductService.Domain.Entities;

public class Product : IBaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockCount { get; set; }
    
    // Concurrency token for atomic updates
    public uint Version { get; set; }
    
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}
