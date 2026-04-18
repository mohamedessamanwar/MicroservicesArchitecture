using RabbitMQ.Client;

namespace OrderService.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Singleton: one IConnection per application. Thread-safe; create channels via CreateChannel().
/// </summary>
public interface IRabbitMQConnectionManager : IDisposable
{
    bool IsConnected { get; }
    IConnection Connection { get; }
    IModel CreateChannel();
}
