namespace OrderService.Infrastructure.EventImplementation.Topology;

public interface IRabbitMqNamePrefixer
{
    string Prefix(string country, string name);
}
