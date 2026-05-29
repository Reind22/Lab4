using System.Text.Json;
using System.Text.Json.Serialization;

namespace TicTacToe.Shared.Protocol;

/// <summary>Общие настройки сериализации JSON для протокола.</summary>
public static class ProtocolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static JsonElement ToElement<T>(T value) =>
        JsonSerializer.SerializeToElement(value, Options);

    public static T? FromElement<T>(JsonElement element) =>
        element.Deserialize<T>(Options);
}
