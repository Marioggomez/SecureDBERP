using System.Text.Json;

namespace SecureERP.Infrastructure.Serialization;

public sealed class SystemTextJsonSerializer : IJsonSerializer
{
    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json);
    }
}
