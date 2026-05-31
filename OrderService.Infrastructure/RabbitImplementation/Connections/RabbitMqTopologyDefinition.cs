namespace OrderService.Infrastructure.RabbitImplementation.Connections;

public sealed class RabbitMqTopologyDefinition
{
    public string ProviderName { get; set; } = default!;
    public string ExchangeName { get; set; } = default!;
    public string ExchangeType { get; set; } = "topic";
    public string QueueName { get; set; } = default!;
    public string? QueueType { get; set; }
    public string DeadLetterExchangeName { get; set; } = default!;
    public string DeadLetterExchangeType { get; set; } = "direct";
    public string DeadLetterQueueName { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public string DeadLetterRoutingKey { get; set; } = default!;
}
