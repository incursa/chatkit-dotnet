using System.Text.Json;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Provides centralized JSON serialization settings for ChatKit contracts.
/// </summary>
public static class ChatKitJson
{
    /// <summary>
    /// Gets the serializer options shared across ChatKit request and response payloads.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    /// <summary>
    /// Serializes a value to UTF-8 JSON using ChatKit serializer settings.
    /// </summary>
    /// <typeparam name="T">The value type being serialized.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized UTF-8 payload.</returns>
    public static byte[] SerializeToUtf8Bytes<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

    /// <summary>
    /// Deserializes a UTF-8 JSON payload using ChatKit serializer settings.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="json">The UTF-8 JSON payload.</param>
    /// <returns>The deserialized value, or <see langword="null"/> when the payload represents <see langword="null"/>.</returns>
    public static T? Deserialize<T>(byte[] json)
        => JsonSerializer.Deserialize<T>(json, SerializerOptions);

    /// <summary>
    /// Deserializes a JSON string using ChatKit serializer settings.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="json">The JSON string payload.</param>
    /// <returns>The deserialized value, or <see langword="null"/> when the payload represents <see langword="null"/>.</returns>
    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, SerializerOptions);

    /// <summary>
    /// Deserializes a ChatKit request payload and validates that a request envelope was produced.
    /// </summary>
    /// <param name="json">The UTF-8 request payload.</param>
    /// <returns>The deserialized ChatKit request.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the payload cannot be deserialized to a <see cref="ChatKitRequest"/>.</exception>
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
