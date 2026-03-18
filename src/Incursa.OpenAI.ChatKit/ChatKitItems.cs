using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

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
    public required string Id { get; init; }

    public required string ThreadId { get; init; }

    public required DateTime CreatedAt { get; init; }
}

public sealed record UserMessageItem : ThreadItem
{
    public List<UserMessageContent> Content { get; init; } = [];

    public List<Attachment> Attachments { get; init; } = [];

    public string? QuotedText { get; init; }

    public InferenceOptions InferenceOptions { get; init; } = new();
}

public sealed record AssistantMessageItem : ThreadItem
{
    public List<AssistantMessageContent> Content { get; init; } = [];
}

public sealed record ClientToolCallItem : ThreadItem
{
    public string Status { get; set; } = "pending";

    public required string CallId { get; init; }

    public required string Name { get; init; }

    public Dictionary<string, JsonNode?> Arguments { get; init; } = new(StringComparer.Ordinal);

    public JsonNode? Output { get; set; }
}

public sealed record WidgetItem : ThreadItem
{
    public required WidgetRoot Widget { get; init; }

    public string? CopyText { get; init; }
}

public sealed record GeneratedImageItem : ThreadItem
{
    public GeneratedImage? Image { get; set; }
}

public sealed record TaskItem : ThreadItem
{
    public required WorkflowTask Task { get; init; }
}

public sealed record WorkflowItem : ThreadItem
{
    public required Workflow Workflow { get; init; }
}

public sealed record EndOfTurnItem : ThreadItem;

public sealed record HiddenContextItem : ThreadItem
{
    public JsonNode? Content { get; init; }
}

public sealed record SdkHiddenContextItem : ThreadItem
{
    public required string Content { get; init; }
}
