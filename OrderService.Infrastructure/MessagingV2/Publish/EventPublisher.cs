using System.Text.Json;
using OrderService.Application.Interfaces;
using OrderService.Domain.Events;
using OrderService.Infrastructure.MessagingV2.Outbox;

namespace OrderService.Infrastructure.MessagingV2.Publish;

public class EventPublisher : IEventPublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IEventRoutingRegistry _routingRegistry;

    public EventPublisher(IOutboxStore outboxStore, IEventRoutingRegistry routingRegistry)
    {
        _outboxStore = outboxStore;
        _routingRegistry = routingRegistry;
    }

    public async Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        var route = _routingRegistry.Get<TEvent>();

        var message = new OrderService.Domain.Entities.OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            EventType = @event!.GetType().Name,
            Payload = JsonSerializer.Serialize(@event),
            ProviderName = route.ProviderName,
            ExchangeName = route.Exchange,
            RoutingKey = route.RoutingKey,
            Status = MessagingStatusConstants.Pending,
            OccurredOnUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        await _outboxStore.AddAsync(message, ct);
        await _outboxStore.SaveChangesAsync(ct);
    }
}
