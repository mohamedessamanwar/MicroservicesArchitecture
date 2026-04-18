namespace OrderService.Infrastructure.MessagingV2.Outbox;

public sealed class EventRoute
{
    public string ProviderName { get; init; } = default!;
    public string Exchange { get; init; } = default!;
    public string RoutingKey { get; init; } = default!;
}
