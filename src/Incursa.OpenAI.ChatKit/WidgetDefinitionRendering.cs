using System.Collections;
using System.Text.Json;
using Incursa.Jinja;
using JinjaEnvironment = Incursa.Jinja.Environment;
using JinjaStrictUndefined = Incursa.Jinja.StrictUndefined;
using JinjaTemplate = Incursa.Jinja.Template;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Compiled renderer for a file-backed ChatKit widget export.
/// </summary>
internal static class WidgetDefinitionRenderer
{
    private const string DropFirstFilterName = "widget_drop_first";
    private const string LengthFilterName = "length";

    private static readonly JinjaEnvironment TemplateEnvironment = new(undefinedType: typeof(JinjaStrictUndefined));
    private static readonly JsonSerializerOptions StateSerializerOptions = CreateStateSerializerOptions();

    public static WidgetRoot Build(WidgetDefinition definition, object? state)
    {
        ValidateDefinition(definition);

        string normalizedTemplate = NormalizeTemplateSource(definition.Template);
        JinjaTemplate template = new(normalizedTemplate, TemplateEnvironment, definition.Name, $"{definition.Name}.widget");

        using JsonDocument stateDocument = SerializeState(state);
        using JsonDocument schemaDocument = JsonDocument.Parse(definition.JsonSchema.ToJsonString());

        ValidateState(schemaDocument.RootElement, stateDocument.RootElement, "$");

        Dictionary<string, object?> context = ConvertObject(schemaDocument.RootElement, stateDocument.RootElement);
        string rendered = template.Render(context);

        return ChatKitJson.Deserialize<WidgetRoot>(rendered)
            ?? throw new InvalidOperationException("Unable to deserialize rendered widget output.");
    }

    private static void ValidateDefinition(WidgetDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Version))
        {
            throw new InvalidOperationException("Widget definition is missing a version.");
        }

        if (!string.Equals(definition.Version, "1.0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported widget definition version '{definition.Version}'.");
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
    }

    private static string NormalizeTemplateSource(string template)
        => template.Replace(
            "{{- (_c[1:] if _c and _c[0] == ',' else _c) -}}",
            "{{- (_c | widget_drop_first) -}}",
            StringComparison.Ordinal);

    private static JsonDocument SerializeState(object? state)
        => JsonSerializer.SerializeToDocument(state ?? new Dictionary<string, object?>(), StateSerializerOptions);

    private static JsonSerializerOptions CreateStateSerializerOptions()
    {
        TemplateEnvironment.Filters[DropFirstFilterName] = DropFirst;
        TemplateEnvironment.Filters[LengthFilterName] = CountLength;

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        };

        return options;
    }

    private static object? DropFirst(object? value, params object?[] arguments)
    {
        _ = arguments;

        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text.Length > 0 ? text[1..] : text;
        }

        if (value is IEnumerable enumerable)
        {
            List<object?> items = [];
            bool skippedFirst = false;

            foreach (object? item in enumerable)
            {
                if (!skippedFirst)
                {
                    skippedFirst = true;
                    continue;
                }

                items.Add(item);
            }

            return items;
        }

        return value;
    }

    private static object CountLength(object? value, params object?[] arguments)
    {
        _ = arguments;

        return value switch
        {
            null => 0,
            string text => text.Length,
            Array array => array.Length,
            WidgetSequence sequence => sequence.Count,
            ICollection collection => collection.Count,
            IEnumerable enumerable => CountEnumerable(enumerable),
            _ => 0,
        };
    }

    private static int CountEnumerable(IEnumerable enumerable)
    {
        int count = 0;
        foreach (object? _ in enumerable)
        {
            count++;
        }

        return count;
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement? schema, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Widget input state must be a JSON object.");
        }

        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        Dictionary<string, JsonElement> properties = new(StringComparer.Ordinal);
        JsonElement? additionalPropertiesSchema = null;
        bool additionalPropertiesAllowed = true;

        if (schema is JsonElement schemaElement && schemaElement.ValueKind == JsonValueKind.Object)
        {
            if (schemaElement.TryGetProperty("properties", out JsonElement propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in propertiesElement.EnumerateObject())
                {
                    properties[property.Name] = property.Value;
                }
            }

            if (schemaElement.TryGetProperty("additionalProperties", out JsonElement additionalPropertiesElement))
            {
                switch (additionalPropertiesElement.ValueKind)
                {
                    case JsonValueKind.False:
                        additionalPropertiesAllowed = false;
                        break;
                    case JsonValueKind.True:
                        additionalPropertiesAllowed = true;
                        break;
                    case JsonValueKind.Object:
                        additionalPropertiesSchema = additionalPropertiesElement;
                        break;
                }
            }
        }

        foreach (KeyValuePair<string, JsonElement> property in properties)
        {
            if (element.TryGetProperty(property.Key, out JsonElement propertyValue))
            {
                result[property.Key] = ConvertValue(property.Value, propertyValue);
            }
            else
            {
                result[property.Key] = CreateMissingValue(property.Value);
            }
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (properties.ContainsKey(property.Name))
            {
                continue;
            }

            if (additionalPropertiesSchema is not null)
            {
                result[property.Name] = ConvertValue(additionalPropertiesSchema, property.Value);
                continue;
            }

            if (additionalPropertiesAllowed)
            {
                result[property.Name] = ConvertValue(null, property.Value);
            }
        }

        return result;
    }

    private static WidgetSequence ConvertArray(JsonElement? schema, JsonElement element)
    {
        List<object?> result = [];

        JsonElement? itemsSchema = null;
        if (schema is JsonElement schemaElement && schemaElement.ValueKind == JsonValueKind.Object && schemaElement.TryGetProperty("items", out JsonElement schemaItems))
        {
            itemsSchema = schemaItems;
        }

        foreach (JsonElement item in element.EnumerateArray())
        {
            result.Add(ConvertValue(itemsSchema, item));
        }

        return new WidgetSequence(result);
    }

    private static object? ConvertValue(JsonElement? schema, JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(schema, element),
            JsonValueKind.Array => ConvertArray(schema, element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => ConvertNumber(element),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => throw new InvalidOperationException($"Unsupported widget input value kind '{element.ValueKind}'."),
        };

    private static object? CreateMissingValue(JsonElement schema)
    {
        if (!TryGetExpectedTypes(schema, out IReadOnlyList<string> expectedTypes) || expectedTypes.Count == 0)
        {
            return null;
        }

        if (expectedTypes.Contains("array", StringComparer.Ordinal))
        {
            return new WidgetSequence([]);
        }

        if (expectedTypes.Contains("object", StringComparer.Ordinal))
        {
            return CreateMissingObject(schema);
        }

        if (expectedTypes.Contains("boolean", StringComparer.Ordinal))
        {
            return false;
        }

        if (expectedTypes.Contains("integer", StringComparer.Ordinal) || expectedTypes.Contains("number", StringComparer.Ordinal))
        {
            return 0;
        }

        if (expectedTypes.Contains("string", StringComparer.Ordinal))
        {
            return string.Empty;
        }

        return null;
    }

    private static Dictionary<string, object?> CreateMissingObject(JsonElement schema)
    {
        using JsonDocument empty = JsonDocument.Parse("{}");
        return ConvertObject(schema, empty.RootElement);
    }

    private static object ConvertNumber(JsonElement element)
    {
        if (element.TryGetInt64(out long int64Value))
        {
            return int64Value;
        }

        if (element.TryGetDecimal(out decimal decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }

    private static void ValidateState(JsonElement schema, JsonElement value, string path)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (TryGetExpectedTypes(schema, out IReadOnlyList<string> expectedTypes) && expectedTypes.Count > 0)
        {
            ValidateType(expectedTypes, value, path);
        }

        if (schema.TryGetProperty("enum", out JsonElement enumElement))
        {
            ValidateEnum(enumElement, value, path);
        }

        if (schema.TryGetProperty("const", out JsonElement constElement))
        {
            ValidateConst(constElement, value, path);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            ValidateString(schema, value, path);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(schema, value, path);
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            ValidateArray(schema, value, path);
        }
    }

    private static void ValidateObject(JsonElement schema, JsonElement value, string path)
    {
        Dictionary<string, JsonElement> properties = new(StringComparer.Ordinal);
        if (schema.TryGetProperty("properties", out JsonElement propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in propertiesElement.EnumerateObject())
            {
                properties[property.Name] = property.Value;
            }
        }

        if (schema.TryGetProperty("required", out JsonElement requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement requiredProperty in requiredElement.EnumerateArray())
            {
                string propertyName = requiredProperty.GetString() ?? string.Empty;
                if (!value.TryGetProperty(propertyName, out _))
                {
                    throw new InvalidOperationException($"Widget input state is missing required property '{path}.{propertyName}'.");
                }
            }
        }

        bool additionalPropertiesAllowed = true;
        JsonElement? additionalPropertiesSchema = null;

        if (schema.TryGetProperty("additionalProperties", out JsonElement additionalPropertiesElement))
        {
            switch (additionalPropertiesElement.ValueKind)
            {
                case JsonValueKind.False:
                    additionalPropertiesAllowed = false;
                    break;
                case JsonValueKind.True:
                    additionalPropertiesAllowed = true;
                    break;
                case JsonValueKind.Object:
                    additionalPropertiesSchema = additionalPropertiesElement;
                    break;
            }
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (properties.TryGetValue(property.Name, out JsonElement propertySchema))
            {
                ValidateState(propertySchema, property.Value, AppendPath(path, property.Name));
                continue;
            }

            if (additionalPropertiesSchema is not null)
            {
                ValidateState(additionalPropertiesSchema.Value, property.Value, AppendPath(path, property.Name));
                continue;
            }

            if (!additionalPropertiesAllowed)
            {
                throw new InvalidOperationException($"Widget input state contains an additional property '{AppendPath(path, property.Name)}' that is not allowed by the schema.");
            }
        }
    }

    private static void ValidateArray(JsonElement schema, JsonElement value, string path)
    {
        if (schema.TryGetProperty("minItems", out JsonElement minItemsElement) && minItemsElement.TryGetInt32(out int minItems) && value.GetArrayLength() < minItems)
        {
            throw new InvalidOperationException($"Widget input state at '{path}' must contain at least {minItems} item(s).");
        }

        if (!schema.TryGetProperty("items", out JsonElement itemsSchema))
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            ValidateState(itemsSchema, item, $"{path}[{index}]");
            index++;
        }
    }

    private static void ValidateString(JsonElement schema, JsonElement value, string path)
    {
        if (schema.TryGetProperty("minLength", out JsonElement minLengthElement) && minLengthElement.TryGetInt32(out int minLength))
        {
            string? stringValue = value.GetString();
            if (stringValue is null || stringValue.Length < minLength)
            {
                throw new InvalidOperationException($"Widget input state at '{path}' must contain at least {minLength} character(s).");
            }
        }

        if (schema.TryGetProperty("maxLength", out JsonElement maxLengthElement) && maxLengthElement.TryGetInt32(out int maxLength))
        {
            string? stringValue = value.GetString();
            if (stringValue is not null && stringValue.Length > maxLength)
            {
                throw new InvalidOperationException($"Widget input state at '{path}' must contain at most {maxLength} character(s).");
            }
        }
    }

    private static bool TryGetExpectedTypes(JsonElement schema, out IReadOnlyList<string> expectedTypes)
    {
        expectedTypes = [];

        if (!schema.TryGetProperty("type", out JsonElement typeElement))
        {
            return false;
        }

        List<string> types = [];
        switch (typeElement.ValueKind)
        {
            case JsonValueKind.String:
                {
                    string? type = typeElement.GetString();
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        types.Add(type);
                    }

                    break;
                }
            case JsonValueKind.Array:
                {
                    foreach (JsonElement item in typeElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        {
                            types.Add(item.GetString()!);
                        }
                    }

                    break;
                }
        }

        expectedTypes = types;
        return true;
    }

    private static void ValidateType(IReadOnlyList<string> expectedTypes, JsonElement value, string path)
    {
        foreach (string expectedType in expectedTypes)
        {
            if (MatchesType(expectedType, value))
            {
                return;
            }
        }

        string expected = string.Join(" or ", expectedTypes.Select(type => $"'{type}'"));
        throw new InvalidOperationException($"Widget input state at '{path}' must be of type {expected}.");
    }

    private static bool MatchesType(string expectedType, JsonElement value)
        => expectedType switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            "number" => value.ValueKind == JsonValueKind.Number,
            "integer" => value.ValueKind == JsonValueKind.Number && IsInteger(value),
            _ => true,
        };

    private static bool IsInteger(JsonElement value)
    {
        if (value.TryGetInt64(out _))
        {
            return true;
        }

        if (value.TryGetDecimal(out decimal decimalValue))
        {
            return decimal.Truncate(decimalValue) == decimalValue;
        }

        return false;
    }

    private static void ValidateEnum(JsonElement enumElement, JsonElement value, string path)
    {
        if (enumElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement enumValue in enumElement.EnumerateArray())
        {
            if (JsonElement.DeepEquals(enumValue, value))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Widget input state at '{path}' does not match any allowed enum value.");
    }

    private static void ValidateConst(JsonElement constElement, JsonElement value, string path)
    {
        if (!JsonElement.DeepEquals(constElement, value))
        {
            throw new InvalidOperationException($"Widget input state at '{path}' does not match the required constant value.");
        }
    }

    private static string AppendPath(string path, string segment)
        => string.Equals(path, "$", StringComparison.Ordinal)
            ? $"$.{segment}"
            : $"{path}.{segment}";

    private sealed class WidgetSequence : IReadOnlyList<object?>
    {
        private readonly List<object?> items;

        public WidgetSequence(List<object?> items)
        {
            this.items = items;
        }

        public int Count => items.Count;

        public object? this[int index] => items[index];

        public string Join(string separator)
            => string.Join(separator, items.Select(item => Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty));

        public IEnumerator<object?> GetEnumerator()
            => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
