using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Connections;

/// <summary>
/// One long-lived RabbitMQ <see cref="IConnection"/> (TCP) per configured provider name.
/// </summary>
public interface IRabbitMqConnectionRegistry
{
    IConnection GetConnection(string providerName);
}
