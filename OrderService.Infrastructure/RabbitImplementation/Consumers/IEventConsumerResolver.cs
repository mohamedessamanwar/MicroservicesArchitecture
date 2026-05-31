namespace OrderService.Infrastructure.RabbitImplementation.Consumers;

public interface IEventConsumerResolver
{
    IEventConsumer Resolve(string eventType);
}

