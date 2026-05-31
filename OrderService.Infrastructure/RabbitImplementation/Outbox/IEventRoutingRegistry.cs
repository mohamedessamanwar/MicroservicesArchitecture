namespace OrderService.Infrastructure.RabbitImplementation.Outbox;

public interface IEventRoutingRegistry
{
    EventRoute Get<TEvent>();
}

