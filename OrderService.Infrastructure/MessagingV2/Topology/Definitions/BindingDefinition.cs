namespace OrderService.Infrastructure.MessagingV2.Topology.Definitions;

/// <summary>
/// RabbitMQ binding definition connecting a queue to an exchange with a routing key.
/// </summary>
public sealed class BindingDefinition
{
    public required string ExchangeName { get; init; }
    public required string QueueName { get; init; }
    public required string RoutingKey { get; init; }
    public Dictionary<string, object> Arguments { get; init; } = new();

    /// <summary>
    /// Creates a binding between an exchange and queue with a routing key.
    /// </summary>
    public static BindingDefinition Create(string exchangeName, string queueName, string routingKey)
    {
        return new BindingDefinition
        {
            ExchangeName = exchangeName,
            QueueName = queueName,
            RoutingKey = routingKey
        };
    }
}
