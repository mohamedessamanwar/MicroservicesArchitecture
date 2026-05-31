using Microsoft.Extensions.Logging;
using Micro.Shared.Persistence;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;

namespace OrderService.Infrastructure.RabbitImplementation.Outbox;

public sealed class EventPublisher : IEventPublisher
{
    private readonly IOutboxStore _outboxStore;
    private readonly IEventRoutingRegistry _routingRegistry;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(
        IOutboxStore outboxStore,
        IEventRoutingRegistry routingRegistry,
        IRequestContext requestContext,
        ILogger<EventPublisher> logger)
    {
        _outboxStore = outboxStore;
        _routingRegistry = routingRegistry;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var route = _routingRegistry.Get<TEvent>();
        var messageId = @event is BaseEvent baseEvent ? baseEvent.Id : Guid.NewGuid();
        var eventType = @event is BaseEvent typedEvent ? typedEvent.EventType : typeof(TEvent).Name;
        var payload = System.Text.Json.JsonSerializer.Serialize(@event);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            EventType = eventType,
            Payload = payload,
            ProviderName = route.ProviderName,
            ExchangeName = route.Exchange,
            RoutingKey = route.RoutingKey,
            HeadersJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                Country = _requestContext.Country,
                OperationMode = _requestContext.OperationMode.ToString()
            }),
            Status = MessagingStatusConstants.Pending,
            RetryCount = 0,
            OccurredOnUtc = DateTime.UtcNow
        };

        await _outboxStore.AddAsync(outboxMessage, cancellationToken);

        _logger.LogInformation(
            "Outbox message queued. Country={Country}, EventType={EventType}, MessageId={MessageId}, Provider={Provider}, Exchange={Exchange}, RoutingKey={RoutingKey}",
            _requestContext.Country,
            eventType,
            messageId,
            route.ProviderName,
            route.Exchange,
            route.RoutingKey);
    }
}
