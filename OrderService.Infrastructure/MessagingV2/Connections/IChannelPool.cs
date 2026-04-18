using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Connections;

/// <summary>
/// Pools <see cref="IModel"/> channels per provider to avoid creating/disposing a channel per publish.
/// Channels are not thread-safe; this pool is intended for sequential/batch use (e.g. outbox dispatch per provider batch).
/// </summary>
public interface IChannelPool
{
    IModel Rent(string providerName);
    void Return(string providerName, IModel channel);
}
