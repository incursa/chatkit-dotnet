using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents a ChatKit widget export file containing the authoring template, schema, preview, and encoded widget payload.
/// </summary>
public sealed class WidgetDefinition
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>
    /// Gets the widget export version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the widget name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the widget template source used by the authoring tool.
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// Gets the JSON schema that describes the widget input state.
    /// </summary>
    public required JsonNode JsonSchema { get; init; }

    /// <summary>
    /// Gets the rendered preview widget, when the export includes one.
    /// </summary>
    public WidgetRoot? OutputJsonPreview { get; init; }

    /// <summary>
    /// Gets the encoded widget payload emitted by the authoring tool.
    /// </summary>
    public required string EncodedWidget { get; init; }

    /// <summary>
    /// Gets any additional export properties that were not modeled explicitly.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> ExtensionData { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Parses a widget export from a JSON string.
    /// </summary>
    /// <param name="json">The widget export JSON payload.</param>
    /// <returns>The parsed widget definition.</returns>
    public static WidgetDefinition Parse(string json)
        => Validate(JsonSerializer.Deserialize<WidgetDefinition>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Unable to deserialize widget definition."));

    /// <summary>
    /// Loads a widget export from a file path.
    /// </summary>
    /// <param name="path">The widget file path.</param>
    /// <returns>The parsed widget definition.</returns>
    public static WidgetDefinition Load(string path)
        => Parse(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>
    /// Loads a widget export from a stream.
    /// </summary>
    /// <param name="stream">The stream that contains the widget export JSON.</param>
    /// <returns>The parsed widget definition.</returns>
    public static WidgetDefinition Load(Stream stream)
    {
        using JsonDocument document = JsonDocument.Parse(stream);
        return Parse(document.RootElement.GetRawText());
    }

    /// <summary>
    /// Loads a widget export from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream that contains the widget export JSON.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed widget definition.</returns>
    public static async Task<WidgetDefinition> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Parse(document.RootElement.GetRawText());
    }

    /// <summary>
    /// Decodes and parses the embedded widget payload.
    /// </summary>
    /// <returns>The decoded widget payload.</returns>
    public WidgetEncodedDefinition DecodeEncodedWidget()
        => WidgetEncodedDefinition.Parse(EncodedWidget);

    private static WidgetDefinition Validate(WidgetDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Version))
        {
            throw new InvalidOperationException("Widget definition is missing a version.");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidOperationException("Widget definition is missing a name.");
        }

        if (string.IsNullOrWhiteSpace(definition.Template))
        {
            throw new InvalidOperationException("Widget definition is missing a template.");
        }

        if (definition.JsonSchema is null)
        {
            throw new InvalidOperationException("Widget definition is missing a JSON schema.");
        }

        if (string.IsNullOrWhiteSpace(definition.EncodedWidget))
        {
            throw new InvalidOperationException("Widget definition is missing an encoded widget payload.");
        }

        return definition;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return options;
    }
}

/// <summary>
/// Represents the decoded widget payload embedded in a widget export file.
/// </summary>
public sealed class WidgetEncodedDefinition
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>
    /// Gets the encoded widget identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the encoded widget name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the widget view source, typically a JSX-like string emitted by the authoring tool.
    /// </summary>
    public required string View { get; init; }

    /// <summary>
    /// Gets the default widget state, when one is embedded in the export.
    /// </summary>
    public JsonNode? DefaultState { get; init; }

    /// <summary>
    /// Gets the named widget states, when the export includes them.
    /// </summary>
    public List<JsonNode?>? States { get; init; }

    /// <summary>
    /// Gets the schema embedded inside the encoded widget payload.
    /// </summary>
    public JsonNode? Schema { get; init; }

    /// <summary>
    /// Gets the schema mode reported by the authoring tool.
    /// </summary>
    public string? SchemaMode { get; init; }

    /// <summary>
    /// Gets the schema validity status reported by the authoring tool.
    /// </summary>
    public string? SchemaValidity { get; init; }

    /// <summary>
    /// Gets the view validity status reported by the authoring tool.
    /// </summary>
    public string? ViewValidity { get; init; }

    /// <summary>
    /// Gets the default-state validity status reported by the authoring tool.
    /// </summary>
    public string? DefaultStateValidity { get; init; }

    /// <summary>
    /// Gets any additional encoded-widget properties that were not modeled explicitly.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> ExtensionData { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Parses a widget payload from its base64url-encoded JSON representation.
    /// </summary>
    /// <param name="encodedWidget">The base64url-encoded widget payload.</param>
    /// <returns>The parsed widget payload.</returns>
    public static WidgetEncodedDefinition Parse(string encodedWidget)
        => Validate(JsonSerializer.Deserialize<WidgetEncodedDefinition>(DecodePayload(encodedWidget), SerializerOptions)
            ?? throw new InvalidOperationException("Unable to deserialize encoded widget payload."));

    /// <summary>
    /// Re-encodes the widget payload as a base64url JSON string.
    /// </summary>
    /// <returns>The encoded widget payload.</returns>
    public string ToEncodedWidget()
        => EncodePayload(JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions));

    private static WidgetEncodedDefinition Validate(WidgetEncodedDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new InvalidOperationException("Encoded widget payload is missing an id.");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidOperationException("Encoded widget payload is missing a name.");
        }

        if (string.IsNullOrWhiteSpace(definition.View))
        {
            throw new InvalidOperationException("Encoded widget payload is missing a view.");
        }

        return definition;
    }

    private static string DecodePayload(string encodedWidget)
    {
        string normalized = new string(encodedWidget.Where(static character => !char.IsWhiteSpace(character)).ToArray());
        normalized = normalized.Replace('-', '+').Replace('_', '/');

        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
            case 1:
                normalized += "===";
                break;
        }

        byte[] bytes = Convert.FromBase64String(normalized);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string EncodePayload(byte[] payload)
    {
        string base64 = Convert.ToBase64String(payload);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return options;
    }
}
