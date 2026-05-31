using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using OrderService.Infrastructure.RabbitImplementation.Connections;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.RabbitImplementation.Topology;

public sealed class RabbitMqTopologyInitializerJob : BackgroundService
{
    private readonly IRabbitMqConnectionRegistry _connectionRegistry;
    private readonly IRabbitMqNamePrefixer _namePrefixer;
    private readonly MessagingOptions _options;
    private readonly ILogger<RabbitMqTopologyInitializerJob> _logger;

    public RabbitMqTopologyInitializerJob(
        IRabbitMqConnectionRegistry connectionRegistry,
        IRabbitMqNamePrefixer namePrefixer,
        IOptions<MessagingOptions> options,
        ILogger<RabbitMqTopologyInitializerJob> logger)
    {
        _connectionRegistry = connectionRegistry;
        _namePrefixer = namePrefixer;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RabbitMqTopologyInitializerJob is waiting to declare {TopologyCount} topology definition(s).",
            _options.Topologies.Count);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Topologies.Count == 0)
        {
            _logger.LogWarning("No RabbitMQ topologies were configured. Topology initialization skipped.");
            return;
        }

        if (_options.Countries.Count == 0)
        {
            _logger.LogWarning("No countries were configured for RabbitMQ topology initialization. Topology initialization skipped.");
            return;
        }

        foreach (var country in _options.Countries)
        {
            foreach (var providerGroup in _options.Topologies.GroupBy(x => x.ProviderName))
            {
                stoppingToken.ThrowIfCancellationRequested();

                using var channel = _connectionRegistry.GetConnection(providerGroup.Key).CreateModel();

                foreach (var topology in providerGroup)
                {
                    DeclareTopology(channel, country, topology);

                    _logger.LogInformation(
                        "Declared RabbitMQ topology. Country={Country}, Provider={Provider}, Exchange={Exchange}, Queue={Queue}, DeadLetterExchange={DeadLetterExchange}, DeadLetterQueue={DeadLetterQueue}, RoutingKey={RoutingKey}",
                        country,
                        topology.ProviderName,
                        _namePrefixer.Prefix(country, topology.ExchangeName),
                        _namePrefixer.Prefix(country, topology.QueueName),
                        _namePrefixer.Prefix(country, topology.DeadLetterExchangeName),
                        _namePrefixer.Prefix(country, topology.DeadLetterQueueName),
                        topology.RoutingKey);
                }
            }
        }

        _logger.LogInformation("RabbitMQ topology initialization completed successfully.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void DeclareTopology(IModel channel, string country, RabbitMqTopologyDefinition topology)
    {
        var exchangeName = _namePrefixer.Prefix(country, topology.ExchangeName);
        var queueName = _namePrefixer.Prefix(country, topology.QueueName);
        var deadLetterExchangeName = _namePrefixer.Prefix(country, topology.DeadLetterExchangeName);
        var deadLetterQueueName = _namePrefixer.Prefix(country, topology.DeadLetterQueueName);

        channel.ExchangeDeclare(deadLetterExchangeName, topology.DeadLetterExchangeType, durable: true, autoDelete: false);
        channel.QueueDeclare(deadLetterQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(deadLetterQueueName, deadLetterExchangeName, topology.DeadLetterRoutingKey);

        channel.ExchangeDeclare(exchangeName, topology.ExchangeType, durable: true, autoDelete: false);

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = deadLetterExchangeName,
            ["x-dead-letter-routing-key"] = topology.DeadLetterRoutingKey
        };

        if (!string.IsNullOrWhiteSpace(topology.QueueType))
        {
            queueArguments["x-queue-type"] = topology.QueueType;
        }

        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);
        channel.QueueBind(queueName, exchangeName, topology.RoutingKey);
    }
}
