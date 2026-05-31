namespace OrderService.Infrastructure.RabbitImplementation.Topology;

public sealed class RabbitMqNamePrefixer : IRabbitMqNamePrefixer
{
    public string Prefix(string country, string name)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        return $"{country}.{name}";
    }
}
