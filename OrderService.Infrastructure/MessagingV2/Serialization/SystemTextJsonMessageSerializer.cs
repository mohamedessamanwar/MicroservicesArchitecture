using System.Text.Json;

namespace OrderService.Infrastructure.MessagingV2.Serialization;

public sealed class SystemTextJsonMessageSerializer : IMessageSerializer
{
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);

    public object? Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type);
}
