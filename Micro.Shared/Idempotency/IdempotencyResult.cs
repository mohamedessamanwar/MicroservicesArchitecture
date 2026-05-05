namespace Micro.Shared.Idempotency;

public class IdempotencyResult
{
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? Body { get; set; }
    public bool IsProcessing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}