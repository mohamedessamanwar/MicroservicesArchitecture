namespace OrderService.Infrastructure.MessagingV2.Topology.Definitions;

/// <summary>
/// RabbitMQ exchange definition with durability and fault tolerance settings.
/// All exchanges are durable by design to survive broker restarts.
/// </summary>
public sealed class ExchangeDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Durable { get; init; } = true;
    public bool AutoDelete { get; init; } = false;
    public Dictionary<string, object> Arguments { get; init; } = new();

    /// <summary>
    /// Creates a durable direct exchange (point-to-point or fan-out with explicit routing keys).
    /// </summary>
    public static ExchangeDefinition CreateDirectExchange(string name)
    {
        return new ExchangeDefinition { Name = name, Type = "direct" };
    }

    /// <summary>
    /// Creates a durable fanout exchange (broadcast to all bound queues).
    /// </summary>
    public static ExchangeDefinition CreateFanoutExchange(string name)
    {
        return new ExchangeDefinition { Name = name, Type = "fanout" };
    }

    /// <summary>
    /// Creates a durable topic exchange (pattern-based routing).
    /// </summary>
    public static ExchangeDefinition CreateTopicExchange(string name)
    {
        return new ExchangeDefinition { Name = name, Type = "topic" };
    }
}
