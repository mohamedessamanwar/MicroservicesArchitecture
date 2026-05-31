namespace OrderService.Infrastructure.RabbitImplementation.Topology;

public interface IRabbitMqNamePrefixer
{
    string Prefix(string country, string name);
}
