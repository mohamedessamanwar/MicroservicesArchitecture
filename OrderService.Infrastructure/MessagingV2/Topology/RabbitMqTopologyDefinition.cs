using OrderService.Infrastructure.MessagingV2.Topology.Definitions;

namespace OrderService.Infrastructure.MessagingV2.Topology;

/// <summary>
/// Complete RabbitMQ topology definition for the application.
/// Immutable structure containing all exchanges, queues, and bindings required by the system.
/// </summary>
public sealed class RabbitMqTopologyDefinition
{
    public required string ProviderName { get; init; }
    public required List<ExchangeDefinition> Exchanges { get; init; }
    public required List<QueueDefinition> Queues { get; init; }
    public required List<BindingDefinition> Bindings { get; init; }

    /// <summary>
    /// Validates that the topology is complete and self-consistent.
    /// - All binding references valid exchanges and queues
    /// - All dead-letter exchanges and queues are declared
    /// </summary>
    public void Validate()
    {
        var exchangeNames = Exchanges.Select(e => e.Name).ToHashSet();
        var queueNames = Queues.Select(q => q.Name).ToHashSet();

        // Validate bindings reference existing exchanges and queues
        foreach (var binding in Bindings)
        {
            if (!exchangeNames.Contains(binding.ExchangeName))
                throw new InvalidOperationException(
                    $"Binding references undefined exchange '{binding.ExchangeName}'.");

            if (!queueNames.Contains(binding.QueueName))
                throw new InvalidOperationException(
                    $"Binding references undefined queue '{binding.QueueName}'.");
        }

        // Validate dead-letter topology is complete
        foreach (var queue in Queues)
        {
            if (queue.Arguments.ContainsKey("x-dead-letter-exchange"))
            {
                var dlxName = queue.DeadLetterExchangeName;
                var dlqName = queue.DeadLetterQueueName;

                if (!exchangeNames.Contains(dlxName))
                    throw new InvalidOperationException(
                        $"Queue '{queue.Name}' references dead-letter exchange '{dlxName}' which is not declared.");

                if (!queueNames.Contains(dlqName))
                    throw new InvalidOperationException(
                        $"Queue '{queue.Name}' references dead-letter queue '{dlqName}' which is not declared.");
            }
        }
    }
}
