using OrderService.Infrastructure.MessagingV2.Outbox;
using OrderService.Infrastructure.MessagingV2.Topology.Definitions;

namespace OrderService.Infrastructure.MessagingV2.Topology;

/// <summary>
/// Builds the complete RabbitMQ topology for the OrderService application.
/// Topology is discovered from actual event routing and consumer definitions.
/// 
/// Discovered topology:
/// - Provider: BillingBroker (from OrderCreatedConsumerTopology)
/// - Exchange: billing.exchange (from EventRoutingRegistry for OrderCreatedEvent)
/// - Queue: order.created.q (from OrderCreatedConsumerTopology)
/// - Routing Key: order.created (from EventRoutingRegistry)
/// - Dead-letter support: order.created.q.dlx, order.created.q.dlq
/// </summary>
public sealed class RabbitMqTopologyConfigurator
{
    private readonly IEventRoutingRegistry _eventRoutingRegistry;

    public RabbitMqTopologyConfigurator(IEventRoutingRegistry eventRoutingRegistry)
    {
        _eventRoutingRegistry = eventRoutingRegistry;
    }

    /// <summary>
    /// Builds the complete topology definition for the application.
    /// All exchanges, queues, and bindings are created as durable to survive broker restarts.
    /// Dead-letter queues are configured for fault isolation.
    /// </summary>
    public RabbitMqTopologyDefinition BuildTopology()
    {
        var exchanges = BuildExchanges();
        var queues = BuildQueues();
        var bindings = BuildBindings();

        var topology = new RabbitMqTopologyDefinition
        {
            ProviderName = "BillingBroker",
            Exchanges = exchanges,
            Queues = queues,
            Bindings = bindings
        };

        // Validate topology consistency before returning
        topology.Validate();

        return topology;
    }

    private List<ExchangeDefinition> BuildExchanges()
    {
        var exchanges = new List<ExchangeDefinition>();

        // Main publishing exchange
        exchanges.Add(ExchangeDefinition.CreateDirectExchange("billing.exchange"));

        // Dead-letter exchange for order.created queue
        exchanges.Add(ExchangeDefinition.CreateDirectExchange("order.created.q.dlx"));

        return exchanges;
    }

    private List<QueueDefinition> BuildQueues()
    {
        var queues = new List<QueueDefinition>();

        // Main consumer queue with dead-lettering support
        var orderCreatedQueue = QueueDefinition.CreateDurableWithDeadLettering("order.created.q");
        queues.Add(orderCreatedQueue);

        // Dead-letter queue for order.created messages that failed processing
        // Dead-letter queue is simple (no DLX configured) to avoid infinite loops
        queues.Add(QueueDefinition.CreateDurable("order.created.q.dlq"));

        return queues;
    }

    private List<BindingDefinition> BuildBindings()
    {
        var bindings = new List<BindingDefinition>();

        // Bind order.created queue to billing.exchange with routing key "order.created"
        bindings.Add(
            BindingDefinition.Create("billing.exchange", "order.created.q", "order.created"));

        // Bind dead-letter queue to dead-letter exchange
        // All messages nacked without requeue or expired by TTL go here
        bindings.Add(
            BindingDefinition.Create("order.created.q.dlx", "order.created.q.dlq", "order.created"));

        return bindings;
    }
}
