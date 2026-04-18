using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Connections;

public sealed class RabbitMqChannelPool : IChannelPool
{
    private readonly IRabbitMqConnectionRegistry _connectionRegistry;
    private readonly ConcurrentDictionary<string, ConcurrentBag<IModel>> _channels = new();

    public RabbitMqChannelPool(IRabbitMqConnectionRegistry connectionRegistry)
    {
        _connectionRegistry = connectionRegistry;
    }

    public IModel Rent(string providerName)
    {
        var bag = _channels.GetOrAdd(providerName, _ => new ConcurrentBag<IModel>());
        if (bag.TryTake(out var channel) && channel.IsOpen)
            return channel;

        return _connectionRegistry.GetConnection(providerName).CreateModel();
    }

    public void Return(string providerName, IModel channel)
    {
        if (!channel.IsOpen)
        {
            channel.Dispose();
            return;
        }

        var bag = _channels.GetOrAdd(providerName, _ => new ConcurrentBag<IModel>());
        bag.Add(channel);
    }
}
