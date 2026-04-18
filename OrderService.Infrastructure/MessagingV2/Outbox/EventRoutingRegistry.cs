using OrderService.Domain.Events;

namespace OrderService.Infrastructure.MessagingV2.Outbox;

/// <summary>
/// Event → provider / exchange / routing key lives in code (not appsettings). Provider endpoints come from configuration.
/// </summary>
public sealed class EventRoutingRegistry : IEventRoutingRegistry
{
    private readonly Dictionary<Type, EventRoute> _routes = new();

    public EventRoutingRegistry()
    {
        Register<OrderCreatedEvent>("BillingBroker", "billing.exchange", "order.created");
    }

    private void Register<TEvent>(string providerName, string exchange, string routingKey)
    {
        _routes[typeof(TEvent)] = new EventRoute
        {
            ProviderName = providerName,
            Exchange = exchange,
            RoutingKey = routingKey
        };
    }

    public EventRoute Get<TEvent>()
    {
        if (_routes.TryGetValue(typeof(TEvent), out var route))
            return route;

        throw new InvalidOperationException($"No route registered for event type {typeof(TEvent).Name}");
    }
}
