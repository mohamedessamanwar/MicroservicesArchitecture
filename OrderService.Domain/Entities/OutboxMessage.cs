namespace OrderService.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string ProviderName { get; set; } = default!;
    public string ExchangeName { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public string? HeadersJson { get; set; }
    public string Status { get; set; } = default!;
    public int RetryCount { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? LastError { get; set; }
}
