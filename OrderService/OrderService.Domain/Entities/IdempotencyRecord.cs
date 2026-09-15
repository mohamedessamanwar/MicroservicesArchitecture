namespace OrderService.Domain.Entities;

public class IdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
