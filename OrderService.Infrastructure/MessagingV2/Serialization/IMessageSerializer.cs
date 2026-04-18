namespace OrderService.Infrastructure.MessagingV2.Serialization;

public interface IMessageSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string json);
    object? Deserialize(string json, Type type);
}
