using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AssistantMessageContentPartAdded), "assistant_message.content_part.added")]
[JsonDerivedType(typeof(AssistantMessageContentPartTextDelta), "assistant_message.content_part.text_delta")]
[JsonDerivedType(typeof(AssistantMessageContentPartAnnotationAdded), "assistant_message.content_part.annotation_added")]
[JsonDerivedType(typeof(AssistantMessageContentPartDone), "assistant_message.content_part.done")]
[JsonDerivedType(typeof(WidgetStreamingTextValueDelta), "widget.streaming_text.value_delta")]
[JsonDerivedType(typeof(WidgetRootUpdated), "widget.root.updated")]
[JsonDerivedType(typeof(WidgetComponentUpdated), "widget.component.updated")]
[JsonDerivedType(typeof(WorkflowTaskAdded), "workflow.task.added")]
[JsonDerivedType(typeof(WorkflowTaskUpdated), "workflow.task.updated")]
[JsonDerivedType(typeof(GeneratedImageUpdated), "generated_image.updated")]
public abstract record ThreadItemUpdate;

public sealed record AssistantMessageContentPartAdded : ThreadItemUpdate
{
    public required int ContentIndex { get; init; }

    public required AssistantMessageContent Content { get; init; }
}

public sealed record AssistantMessageContentPartTextDelta : ThreadItemUpdate
{
    public required int ContentIndex { get; init; }

    public required string Delta { get; init; }
}

public sealed record AssistantMessageContentPartAnnotationAdded : ThreadItemUpdate
{
    public required int ContentIndex { get; init; }

    public required int AnnotationIndex { get; init; }

    public required Annotation Annotation { get; init; }
}

public sealed record AssistantMessageContentPartDone : ThreadItemUpdate
{
    public required int ContentIndex { get; init; }

    public required AssistantMessageContent Content { get; init; }
}

public sealed record WidgetStreamingTextValueDelta : ThreadItemUpdate
{
    public required string ComponentId { get; init; }

    public required string Delta { get; init; }

    public required bool Done { get; init; }
}

public sealed record WidgetRootUpdated : ThreadItemUpdate
{
    public required WidgetRoot Widget { get; init; }
}

public sealed record WidgetComponentUpdated : ThreadItemUpdate
{
    public required string ComponentId { get; init; }

    public required WidgetComponent Component { get; init; }
}

public sealed record WorkflowTaskAdded : ThreadItemUpdate
{
    public required int TaskIndex { get; init; }

    public required WorkflowTask Task { get; init; }
}

public sealed record WorkflowTaskUpdated : ThreadItemUpdate
{
    public required int TaskIndex { get; init; }

    public required WorkflowTask Task { get; init; }
}

public sealed record GeneratedImageUpdated : ThreadItemUpdate
{
    public required GeneratedImage Image { get; init; }

    public double? Progress { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ThreadCreatedEvent), "thread.created")]
[JsonDerivedType(typeof(ThreadUpdatedEvent), "thread.updated")]
[JsonDerivedType(typeof(ThreadItemAddedEvent), "thread.item.added")]
[JsonDerivedType(typeof(ThreadItemUpdatedEvent), "thread.item.updated")]
[JsonDerivedType(typeof(ThreadItemDoneEvent), "thread.item.done")]
[JsonDerivedType(typeof(ThreadItemRemovedEvent), "thread.item.removed")]
[JsonDerivedType(typeof(ThreadItemReplacedEvent), "thread.item.replaced")]
[JsonDerivedType(typeof(StreamOptionsEvent), "stream_options")]
[JsonDerivedType(typeof(ProgressUpdateEvent), "progress_update")]
[JsonDerivedType(typeof(ClientEffectEvent), "client_effect")]
[JsonDerivedType(typeof(ErrorEvent), "error")]
[JsonDerivedType(typeof(NoticeEvent), "notice")]
public abstract record ThreadStreamEvent;

public sealed record ThreadCreatedEvent : ThreadStreamEvent
{
    public required Thread Thread { get; init; }
}

public sealed record ThreadUpdatedEvent : ThreadStreamEvent
{
    public required Thread Thread { get; init; }
}

public sealed record ThreadItemAddedEvent : ThreadStreamEvent
{
    public required ThreadItem Item { get; init; }
}

public sealed record ThreadItemUpdatedEvent : ThreadStreamEvent
{
    public required string ItemId { get; init; }

    public required ThreadItemUpdate Update { get; init; }
}

public sealed record ThreadItemDoneEvent : ThreadStreamEvent
{
    public required ThreadItem Item { get; init; }
}

public sealed record ThreadItemRemovedEvent : ThreadStreamEvent
{
    public required string ItemId { get; init; }
}

public sealed record ThreadItemReplacedEvent : ThreadStreamEvent
{
    public required ThreadItem Item { get; init; }
}

public sealed record StreamOptionsEvent : ThreadStreamEvent
{
    public required StreamOptions StreamOptions { get; init; }
}

public sealed record ProgressUpdateEvent : ThreadStreamEvent
{
    public string? Icon { get; init; }

    public required string Text { get; init; }
}

public sealed record ClientEffectEvent : ThreadStreamEvent
{
    public required string Name { get; init; }

    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);
}

public sealed record ErrorEvent : ThreadStreamEvent
{
    public string Code { get; init; } = "custom";

    public string? Message { get; init; }

    public bool AllowRetry { get; init; }
}

public sealed record NoticeEvent : ThreadStreamEvent
{
    public required string Level { get; init; }

    public required string Message { get; init; }

    public string? Title { get; init; }
}
