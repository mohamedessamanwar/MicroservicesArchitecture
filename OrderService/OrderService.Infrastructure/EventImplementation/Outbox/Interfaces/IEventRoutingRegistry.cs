namespace OrderService.Infrastructure.EventImplementation.Outbox;

public interface IEventRoutingRegistry
{
    EventRoute Get<TEvent>();
}

