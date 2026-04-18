namespace OrderService.Domain.Entities;

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime ProcessedOnUtc { get; set; }
}
