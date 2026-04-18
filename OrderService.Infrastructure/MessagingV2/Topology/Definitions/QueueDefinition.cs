namespace OrderService.Infrastructure.MessagingV2.Topology.Definitions;

/// <summary>
/// RabbitMQ queue definition with durability and dead-lettering support.
/// All queues are durable by design to survive broker restarts.
/// </summary>
public sealed class QueueDefinition
{
    public required string Name { get; init; }
    public bool Durable { get; init; } = true;
    public bool Exclusive { get; init; } = false;
    public bool AutoDelete { get; init; } = false;
    public Dictionary<string, object> Arguments { get; init; } = new();

    /// <summary>
    /// Gets or creates the dead-letter exchange name for this queue.
    /// Follows the naming convention: {QueueName}.dlx
    /// </summary>
    public string DeadLetterExchangeName => $"{Name}.dlx";

    /// <summary>
    /// Gets or creates the dead-letter queue name for this queue.
    /// Follows the naming convention: {QueueName}.dlq
    /// </summary>
    public string DeadLetterQueueName => $"{Name}.dlq";

    /// <summary>
    /// Configures dead-lettering for this queue.
    /// When messages are nacked without requeue or TTL expires, they are sent to the DLX.
    /// </summary>
    public void ConfigureDeadLettering()
    {
        Arguments["x-dead-letter-exchange"] = DeadLetterExchangeName;
    }

    /// <summary>
    /// Sets the message TTL (time-to-live) in milliseconds.
    /// Messages older than this TTL are dead-lettered.
    /// </summary>
    public void SetMessageTtlMilliseconds(int ttlMs)
    {
        Arguments["x-message-ttl"] = ttlMs;
    }

    /// <summary>
    /// Sets the maximum length of the queue.
    /// When the queue reaches this limit, older messages are dropped or dead-lettered.
    /// </summary>
    public void SetMaxLength(int maxLength)
    {
        Arguments["x-max-length"] = maxLength;
    }

    /// <summary>
    /// Creates a durable queue without dead-lettering (simple case).
    /// </summary>
    public static QueueDefinition CreateDurable(string name)
    {
        return new QueueDefinition { Name = name };
    }

    /// <summary>
    /// Creates a durable queue with dead-lettering support.
    /// </summary>
    public static QueueDefinition CreateDurableWithDeadLettering(string name)
    {
        var queue = new QueueDefinition { Name = name };
        queue.ConfigureDeadLettering();
        return queue;
    }
}
