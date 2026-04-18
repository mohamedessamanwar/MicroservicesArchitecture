namespace OrderService.Infrastructure.MessagingV2.Connections;

public sealed class MessagingOptions
{
    public List<RabbitMqProviderOptions> Providers { get; set; } = [];
}

public sealed class RabbitMqProviderOptions
{
    public string Name { get; set; } = default!;
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public int HeartbeatSeconds { get; set; } = 30;
}
