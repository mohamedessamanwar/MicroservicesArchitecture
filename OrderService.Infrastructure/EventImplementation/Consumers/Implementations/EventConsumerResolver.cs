using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Events;

namespace OrderService.Infrastructure.EventImplementation.Consumers;

public sealed class EventConsumerResolver : IEventConsumerResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventConsumerResolver> _logger;

    public EventConsumerResolver(IServiceProvider serviceProvider, ILogger<EventConsumerResolver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEventConsumer Resolve(string eventType)
    {
        IEventConsumer consumer = eventType switch
        {
            nameof(OrderCreatedEvent) => _serviceProvider.GetRequiredService<OrderCreatedEventConsumer>(),
            _ => throw new InvalidOperationException($"No consumer registered for event type '{eventType}'.")
        };

        _logger.LogInformation(
            "Resolved consumer strategy. EventType={EventType}, Consumer={Consumer}",
            eventType,
            consumer.GetType().Name);

        return consumer;
    }
}

