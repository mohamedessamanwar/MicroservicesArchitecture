namespace OrderService.Infrastructure.RabbitImplementation.Connections;

public sealed class MessagingOptions
{
    public List<string> Countries { get; set; } = [];
    public List<RabbitMqProviderOptions> Providers { get; set; } = [];
    public List<RabbitMqTopologyDefinition> Topologies { get; set; } = [];
}

