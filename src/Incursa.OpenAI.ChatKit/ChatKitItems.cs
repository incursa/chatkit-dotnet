using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents a persisted or streamed item that belongs to a chat thread.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UserMessageItem), "user_message")]
[JsonDerivedType(typeof(AssistantMessageItem), "assistant_message")]
[JsonDerivedType(typeof(ClientToolCallItem), "client_tool_call")]
[JsonDerivedType(typeof(WidgetItem), "widget")]
[JsonDerivedType(typeof(GeneratedImageItem), "generated_image")]
[JsonDerivedType(typeof(TaskItem), "task")]
[JsonDerivedType(typeof(WorkflowItem), "workflow")]
[JsonDerivedType(typeof(EndOfTurnItem), "end_of_turn")]
[JsonDerivedType(typeof(HiddenContextItem), "hidden_context_item")]
[JsonDerivedType(typeof(SdkHiddenContextItem), "sdk_hidden_context")]
public abstract record ThreadItem
{
    /// <summary>
    /// Gets the unique identifier for the thread item.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the identifier of the thread that owns the item.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the timestamp when the item was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Represents a user-authored message item.
/// </summary>
public sealed record UserMessageItem : ThreadItem
{
    /// <summary>
    /// Gets the ordered message content parts entered by the user.
    /// </summary>
    public List<UserMessageContent> Content { get; init; } = [];

    /// <summary>
    /// Gets the attachments referenced by the user message.
    /// </summary>
    public List<Attachment> Attachments { get; init; } = [];

    /// <summary>
    /// Gets the optional quoted text included with the message.
    /// </summary>
    public string? QuotedText { get; init; }

    /// <summary>
    /// Gets the inference options that should be used while answering this message.
    /// </summary>
    public InferenceOptions InferenceOptions { get; init; } = new();
}

/// <summary>
/// Represents an assistant-authored message item.
/// </summary>
public sealed record AssistantMessageItem : ThreadItem
{
    /// <summary>
    /// Gets the ordered assistant content parts in the message.
    /// </summary>
    public List<AssistantMessageContent> Content { get; init; } = [];
}

/// <summary>
/// Represents a client tool invocation captured in the thread transcript.
/// </summary>
public sealed record ClientToolCallItem : ThreadItem
{
    /// <summary>
    /// Gets or sets the current tool call status.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Gets the tool call identifier supplied to the client.
    /// </summary>
    public required string CallId { get; init; }

    /// <summary>
    /// Gets the name of the client tool to invoke.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the arguments passed to the client tool.
    /// </summary>
    public Dictionary<string, JsonNode?> Arguments { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the optional result supplied by the client after the tool completes.
    /// </summary>
    public JsonNode? Output { get; set; }
}

/// <summary>
/// Represents a widget rendered into the conversation.
/// </summary>
public sealed record WidgetItem : ThreadItem
{
    /// <summary>
    /// Gets the widget tree that should be rendered.
    /// </summary>
    public required WidgetRoot Widget { get; init; }

    /// <summary>
    /// Gets optional plain-text content associated with the widget.
    /// </summary>
    public string? CopyText { get; init; }
}

/// <summary>
/// Represents a generated image placeholder or completed image result.
/// </summary>
public sealed record GeneratedImageItem : ThreadItem
{
    /// <summary>
    /// Gets or sets the current generated image payload.
    /// </summary>
    public GeneratedImage? Image { get; set; }
}

/// <summary>
/// Represents a single workflow task entry in the thread.
/// </summary>
public sealed record TaskItem : ThreadItem
{
    /// <summary>
    /// Gets the workflow task payload.
    /// </summary>
    public required WorkflowTask Task { get; init; }
}

/// <summary>
/// Represents the workflow summary item rendered in the thread.
/// </summary>
public sealed record WorkflowItem : ThreadItem
{
    /// <summary>
    /// Gets the workflow payload.
    /// </summary>
    public required Workflow Workflow { get; init; }
}

/// <summary>
/// Represents a marker indicating that the assistant finished its turn.
/// </summary>
public sealed record EndOfTurnItem : ThreadItem;

/// <summary>
/// Represents a hidden context item stored for model context but not shown to users.
/// </summary>
public sealed record HiddenContextItem : ThreadItem
{
    /// <summary>
    /// Gets the hidden JSON payload.
    /// </summary>
    public JsonNode? Content { get; init; }
}

/// <summary>
/// Represents SDK-managed hidden context stored for future turns.
/// </summary>
public sealed record SdkHiddenContextItem : ThreadItem
{
    /// <summary>
    /// Gets the hidden text payload maintained by the SDK.
    /// </summary>
    public required string Content { get; init; }
}
