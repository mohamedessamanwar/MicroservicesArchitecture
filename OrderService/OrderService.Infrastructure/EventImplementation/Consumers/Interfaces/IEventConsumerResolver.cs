namespace OrderService.Infrastructure.EventImplementation.Consumers;

public interface IEventConsumerResolver
{
    IEventConsumer Resolve(string eventType);
}

