using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.MessagingV2.Connections;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.MessagingV2.Topology;

/// <summary>
/// Declares all RabbitMQ topology (exchanges, queues, bindings) against the broker.
/// Executes topology declarations idempotently; safe to run repeatedly as long as definitions are consistent.
/// 
/// Declaration failures are fatal: if topology cannot be established, the initializer throws
/// and prevents dependent background services from starting.
/// </summary>
public sealed class RabbitMqTopologyInitializer
{
    private readonly IRabbitMqConnectionRegistry _connectionRegistry;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(
        IRabbitMqConnectionRegistry connectionRegistry,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connectionRegistry = connectionRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Declares all topology defined in the provided definition.
    /// Must complete successfully before consumers or publishers begin processing.
    /// </summary>
    /// <exception cref="InvalidOperationException">If topology declaration fails.</exception>
    public void InitializeTopology(RabbitMqTopologyDefinition definition)
    {
        _logger.LogInformation(
            "Initializing RabbitMQ topology for provider '{ProviderName}'. " +
            "Exchanges={ExchangeCount}, Queues={QueueCount}, Bindings={BindingCount}",
            definition.ProviderName,
            definition.Exchanges.Count,
            definition.Queues.Count,
            definition.Bindings.Count);

        var connection = _connectionRegistry.GetConnection(definition.ProviderName);
        using var channel = connection.CreateModel();

        try
        {
            DeclareExchanges(channel, definition);
            DeclareQueues(channel, definition);
            CreateBindings(channel, definition);

            _logger.LogInformation(
                "RabbitMQ topology initialization completed successfully for provider '{ProviderName}'.",
                definition.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "RabbitMQ topology initialization failed for provider '{ProviderName}'. " +
                "Dependent background services will not start. Details: {Details}",
                definition.ProviderName,
                ex.Message);
            throw;
        }
    }

    private void DeclareExchanges(IModel channel, RabbitMqTopologyDefinition definition)
    {
        foreach (var exchange in definition.Exchanges)
        {
            try
            {
                channel.ExchangeDeclare(
                    exchange: exchange.Name,
                    type: exchange.Type,
                    durable: exchange.Durable,
                    autoDelete: exchange.AutoDelete,
                    arguments: exchange.Arguments);

                _logger.LogInformation(
                    "Exchange declared successfully. Exchange='{ExchangeName}', Type='{Type}', Durable={Durable}",
                    exchange.Name,
                    exchange.Type,
                    exchange.Durable);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to declare exchange '{ExchangeName}'. Type='{Type}'. Details: {Details}",
                    exchange.Name,
                    exchange.Type,
                    ex.Message);
                throw;
            }
        }
    }

    private void DeclareQueues(IModel channel, RabbitMqTopologyDefinition definition)
    {
        foreach (var queue in definition.Queues)
        {
            try
            {
                channel.QueueDeclare(
                    queue: queue.Name,
                    durable: queue.Durable,
                    exclusive: queue.Exclusive,
                    autoDelete: queue.AutoDelete,
                    arguments: queue.Arguments.Count > 0 ? queue.Arguments : null);

                _logger.LogInformation(
                    "Queue declared successfully. Queue='{QueueName}', Durable={Durable}",
                    queue.Name,
                    queue.Durable);

                // Log dead-letter configuration if present
                if (queue.Arguments.ContainsKey("x-dead-letter-exchange"))
                {
                    _logger.LogInformation(
                        "Queue '{QueueName}' configured for dead-lettering. DLX='{DLX}', DLQ='{DLQ}'",
                        queue.Name,
                        queue.DeadLetterExchangeName,
                        queue.DeadLetterQueueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to declare queue '{QueueName}'. Details: {Details}",
                    queue.Name,
                    ex.Message);
                throw;
            }
        }
    }

    private void CreateBindings(IModel channel, RabbitMqTopologyDefinition definition)
    {
        foreach (var binding in definition.Bindings)
        {
            try
            {
                channel.QueueBind(
                    queue: binding.QueueName,
                    exchange: binding.ExchangeName,
                    routingKey: binding.RoutingKey,
                    arguments: binding.Arguments.Count > 0 ? binding.Arguments : null);

                _logger.LogInformation(
                    "Binding created successfully. Queue='{QueueName}', Exchange='{ExchangeName}', RoutingKey='{RoutingKey}'",
                    binding.QueueName,
                    binding.ExchangeName,
                    binding.RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create binding. Queue='{QueueName}', Exchange='{ExchangeName}', RoutingKey='{RoutingKey}'. Details: {Details}",
                    binding.QueueName,
                    binding.ExchangeName,
                    binding.RoutingKey,
                    ex.Message);
                throw;
            }
        }
    }
}
