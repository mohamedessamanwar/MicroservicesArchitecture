using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Connections;

/// <summary>
/// RabbitMQ connection = TCP connection. .NET does not pool RabbitMQ connections;
/// we keep one <see cref="IConnection"/> per provider and create channels on top of it.
/// Channels are lighter than connections but are not thread-safe.
/// </summary>
public sealed class RabbitMqConnectionRegistry : IRabbitMqConnectionRegistry, IDisposable
{
    private readonly ConcurrentDictionary<string, IConnection> _connections = new();
    private readonly MessagingOptions _options;

    public RabbitMqConnectionRegistry(IOptions<MessagingOptions> options)
    {
        _options = options.Value;
    }

    public IConnection GetConnection(string providerName) =>
        _connections.GetOrAdd(providerName, CreateConnection);

    private IConnection CreateConnection(string providerName)
    {
        var provider = _options.Providers.FirstOrDefault(x => x.Name == providerName)
            ?? throw new InvalidOperationException($"Messaging provider '{providerName}' was not found in configuration.");

        var factory = new ConnectionFactory
        {
            HostName = provider.Host,
            Port = provider.Port,
            VirtualHost = provider.VirtualHost,
            UserName = provider.Username,
            Password = provider.Password,
            RequestedHeartbeat = TimeSpan.FromSeconds(provider.HeartbeatSeconds),
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        return factory.CreateConnection(provider.Name);
    }

    public void Dispose()
    {
        foreach (var connection in _connections.Values)
        {
            if (connection.IsOpen)
                connection.Close();

            connection.Dispose();
        }
    }
}
