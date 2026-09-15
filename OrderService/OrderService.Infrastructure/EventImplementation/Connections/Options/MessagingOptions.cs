namespace OrderService.Infrastructure.EventImplementation.Connections;

public sealed class MessagingOptions
{
    public List<string> Countries { get; set; } = [];
    public List<RabbitMqProviderOptions> Providers { get; set; } = [];
}

