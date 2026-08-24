namespace OrderService.Domain.Entities;

public enum SagaStatus
{
    Pending,
    Executing,
    Completed,
    Failed,
    Compensating,
    Compensated
}

public class Saga
{
    public Guid Id { get; set; }
    
    // The idempotency key from the client to prevent duplicate sagas
    public string CorrelationId { get; set; } = string.Empty;
    
    public string Type { get; set; } = string.Empty;
    public string BusinessId { get; set; } = string.Empty; // e.g., OrderId
    
    public SagaStatus Status { get; set; } = SagaStatus.Pending;
    public string CurrentStep { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<SagaStep> Steps { get; set; } = new List<SagaStep>();
}
