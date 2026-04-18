namespace OrderService.Infrastructure.Messaging.RabbitMqConfiguration;

public class RabbitMqConfiguration
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 5;
    // Dead-letter and queue limits (CDC flow)
    public string DeadLetterExchange { get; set; } = string.Empty;
    public string DeadLetterQueue { get; set; } = string.Empty;
    public string DeadLetterRoutingKey { get; set; } = string.Empty;
    public int MessageTtlMs { get; set; } = 86400000; // 24h default
    public int MaxLength { get; set; } = 50000;
}