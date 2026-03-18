using System.Text.Json;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

public static class ChatKitJson
{
    public static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public static byte[] SerializeToUtf8Bytes<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

    public static T? Deserialize<T>(byte[] json)
        => JsonSerializer.Deserialize<T>(json, SerializerOptions);

    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, SerializerOptions);

    public static ChatKitRequest DeserializeRequest(ReadOnlyMemory<byte> json)
        => JsonSerializer.Deserialize<ChatKitRequest>(json.Span, SerializerOptions)
            ?? throw new InvalidOperationException("Unable to deserialize ChatKit request payload.");

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        return options;
    }
}
