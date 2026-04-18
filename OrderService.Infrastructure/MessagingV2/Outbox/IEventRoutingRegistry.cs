namespace OrderService.Infrastructure.MessagingV2.Outbox;

public interface IEventRoutingRegistry
{
    EventRoute Get<TEvent>();
}
