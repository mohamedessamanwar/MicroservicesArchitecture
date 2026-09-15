using System.Text.Json;

namespace OrderService.Infrastructure.EventImplementation.Consumers;

public interface IEventConsumer
{
    string EventType { get; }

    Task ConsumeAsync(JsonElement message, CancellationToken cancellationToken = default);
}

