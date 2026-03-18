using System.Text.Json;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents a single widget component node.
/// </summary>
public class WidgetComponent
{
    /// <summary>
    /// Gets the optional stable key used by the client.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the optional component identifier.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the component type name.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the optional child components.
    /// </summary>
    public List<WidgetComponent>? Children { get; init; }

    /// <summary>
    /// Gets arbitrary component properties serialized as extension data.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Creates a deep clone of the component using ChatKit JSON serialization.
    /// </summary>
    /// <returns>A cloned widget component.</returns>
    public WidgetComponent DeepClone()
        => ChatKitJson.Deserialize<WidgetComponent>(ChatKitJson.SerializeToUtf8Bytes(this))!;

    /// <summary>
    /// Attempts to read a component property as a string.
    /// </summary>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The string value when present; otherwise, <see langword="null"/>.</returns>
    public string? TryGetString(string propertyName)
    {
        if (!Properties.TryGetValue(propertyName, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
            _ => value.ToString(),
        };
    }

    /// <summary>
    /// Attempts to read a component property as a Boolean value.
    /// </summary>
    /// <param name="propertyName">The property name to read.</param>
    /// <param name="value">When this method returns, contains the parsed Boolean value if one was found.</param>
    /// <returns><see langword="true"/> when the property could be interpreted as a Boolean; otherwise, <see langword="false"/>.</returns>
    public bool TryGetBoolean(string propertyName, out bool value)
    {
        value = false;
        if (!Properties.TryGetValue(propertyName, out object? node) || node is null)
        {
            return false;
        }

        switch (node)
        {
            case bool boolean:
                value = boolean;
                return true;
            case JsonElement json when json.ValueKind is JsonValueKind.True or JsonValueKind.False:
                value = json.GetBoolean();
                return true;
            default:
                return false;
        }
    }
}

/// <summary>
/// Represents the root component of a widget tree.
/// </summary>
public sealed class WidgetRoot : WidgetComponent;

/// <summary>
/// Provides helpers for computing and streaming widget updates.
/// </summary>
public static class WidgetStreaming
{
    /// <summary>
    /// Computes the minimal set of thread item updates needed to transform one widget tree into another.
    /// </summary>
    /// <param name="before">The previously rendered widget tree.</param>
    /// <param name="after">The updated widget tree.</param>
    /// <returns>The thread item updates required to represent the change.</returns>
    public static IReadOnlyList<ThreadItemUpdate> Diff(WidgetRoot before, WidgetRoot after)
    {
        if (RequiresFullReplace(before, after))
        {
            return [new WidgetRootUpdated { Widget = after }];
        }

        Dictionary<string, WidgetComponent> beforeNodes = new(StringComparer.Ordinal);
        Dictionary<string, WidgetComponent> afterNodes = new(StringComparer.Ordinal);
        IndexStreamingNodes(before, beforeNodes);
        IndexStreamingNodes(after, afterNodes);

        List<ThreadItemUpdate> updates = [];
        foreach ((string id, WidgetComponent afterNode) in afterNodes)
        {
            if (!beforeNodes.TryGetValue(id, out WidgetComponent? beforeNode))
            {
                throw new InvalidOperationException($"Node {id} was not present when the widget was initially rendered.");
            }

            string beforeValue = beforeNode.TryGetString("value") ?? string.Empty;
            string afterValue = afterNode.TryGetString("value") ?? string.Empty;
            if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
            {
                continue;
            }

            if (!afterValue.StartsWith(beforeValue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Node {id} was updated with a new value that is not a prefix of the initial value.");
            }

            bool done = !(afterNode.TryGetBoolean("streaming", out bool streaming) && streaming);
            updates.Add(new WidgetStreamingTextValueDelta
            {
                ComponentId = id,
                Delta = afterValue[beforeValue.Length..],
                Done = done,
            });
        }

        return updates;
    }

    /// <summary>
    /// Streams the initial widget item event for a widget tree.
    /// </summary>
    /// <param name="thread">The thread that owns the widget item.</param>
    /// <param name="widget">The widget tree to emit.</param>
    /// <param name="itemId">The identifier of the widget item.</param>
    /// <param name="copyText">Optional plain-text copy associated with the widget.</param>
    /// <returns>An async sequence of thread stream events for the widget.</returns>
    public static async IAsyncEnumerable<ThreadStreamEvent> StreamAsync(
        ThreadMetadata thread,
        WidgetRoot widget,
        string itemId,
        string? copyText = null)
    {
        yield return new ThreadItemDoneEvent
        {
            Item = new WidgetItem
            {
                Id = itemId,
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Widget = widget,
                CopyText = copyText,
            },
        };

        await Task.CompletedTask;
    }

    private static void IndexStreamingNodes(WidgetComponent component, IDictionary<string, WidgetComponent> nodes)
    {
        if (IsStreamingText(component) && !string.IsNullOrWhiteSpace(component.Id))
        {
            nodes[component.Id!] = component;
        }

        if (component.Children is null)
        {
            return;
        }

        foreach (WidgetComponent child in component.Children)
        {
            IndexStreamingNodes(child, nodes);
        }
    }

    private static bool IsStreamingText(WidgetComponent component)
        => (string.Equals(component.Type, "Text", StringComparison.Ordinal) || string.Equals(component.Type, "Markdown", StringComparison.Ordinal))
            && component.Properties.TryGetValue("value", out object? value)
            && value is not null;

    private static bool RequiresFullReplace(WidgetComponent before, WidgetComponent after)
    {
        if (!string.Equals(before.Type, after.Type, StringComparison.Ordinal)
            || !string.Equals(before.Id, after.Id, StringComparison.Ordinal)
            || !string.Equals(before.Key, after.Key, StringComparison.Ordinal))
        {
            return true;
        }

        if (!PropertiesMatch(before, after))
        {
            return true;
        }

        IReadOnlyList<WidgetComponent> beforeChildren = before.Children ?? [];
        IReadOnlyList<WidgetComponent> afterChildren = after.Children ?? [];
        if (beforeChildren.Count != afterChildren.Count)
        {
            return true;
        }

        for (int index = 0; index < beforeChildren.Count; index++)
        {
            if (RequiresFullReplace(beforeChildren[index], afterChildren[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PropertiesMatch(WidgetComponent before, WidgetComponent after)
    {
        HashSet<string> keys = new(before.Properties.Keys, StringComparer.Ordinal);
        keys.UnionWith(after.Properties.Keys);

        foreach (string key in keys)
        {
            if (string.Equals(key, "children", StringComparison.Ordinal))
            {
                continue;
            }

            before.Properties.TryGetValue(key, out object? beforeValue);
            after.Properties.TryGetValue(key, out object? afterValue);

            if (string.Equals(key, "value", StringComparison.Ordinal) && IsStreamingText(before) && IsStreamingText(after))
            {
                string beforeText = before.TryGetString("value") ?? string.Empty;
                string afterText = after.TryGetString("value") ?? string.Empty;
                if (afterText.StartsWith(beforeText, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            if (string.Equals(key, "streaming", StringComparison.Ordinal) && IsStreamingText(before) && IsStreamingText(after))
            {
                continue;
            }

            string beforeJson = JsonSerializer.Serialize(beforeValue, ChatKitJson.SerializerOptions);
            string afterJson = JsonSerializer.Serialize(afterValue, ChatKitJson.SerializerOptions);
            if (!string.Equals(beforeJson, afterJson, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
