using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents an incremental update applied to an existing thread item.
/// </summary>
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

/// <summary>
/// Announces that a new assistant content part was added.
/// </summary>
public sealed record AssistantMessageContentPartAdded : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based content index that was added.
    /// </summary>
    public required int ContentIndex { get; init; }

    /// <summary>
    /// Gets the content part that was added.
    /// </summary>
    public required AssistantMessageContent Content { get; init; }
}

/// <summary>
/// Announces a text delta for an assistant content part.
/// </summary>
public sealed record AssistantMessageContentPartTextDelta : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based content index being updated.
    /// </summary>
    public required int ContentIndex { get; init; }

    /// <summary>
    /// Gets the appended text delta.
    /// </summary>
    public required string Delta { get; init; }
}

/// <summary>
/// Announces that an annotation was added to an assistant content part.
/// </summary>
public sealed record AssistantMessageContentPartAnnotationAdded : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based content index being updated.
    /// </summary>
    public required int ContentIndex { get; init; }

    /// <summary>
    /// Gets the zero-based annotation index that was added.
    /// </summary>
    public required int AnnotationIndex { get; init; }

    /// <summary>
    /// Gets the annotation payload that was added.
    /// </summary>
    public required Annotation Annotation { get; init; }
}

/// <summary>
/// Announces that an assistant content part finished streaming.
/// </summary>
public sealed record AssistantMessageContentPartDone : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based content index that completed.
    /// </summary>
    public required int ContentIndex { get; init; }

    /// <summary>
    /// Gets the final completed content part.
    /// </summary>
    public required AssistantMessageContent Content { get; init; }
}

/// <summary>
/// Announces a streaming text delta within a widget component.
/// </summary>
public sealed record WidgetStreamingTextValueDelta : ThreadItemUpdate
{
    /// <summary>
    /// Gets the identifier of the widget component being updated.
    /// </summary>
    public required string ComponentId { get; init; }

    /// <summary>
    /// Gets the appended widget text delta.
    /// </summary>
    public required string Delta { get; init; }

    /// <summary>
    /// Gets a value indicating whether the streaming text value is complete.
    /// </summary>
    public required bool Done { get; init; }
}

/// <summary>
/// Announces that the entire widget root should be replaced.
/// </summary>
public sealed record WidgetRootUpdated : ThreadItemUpdate
{
    /// <summary>
    /// Gets the replacement widget tree.
    /// </summary>
    public required WidgetRoot Widget { get; init; }
}

/// <summary>
/// Announces that a single widget component should be replaced.
/// </summary>
public sealed record WidgetComponentUpdated : ThreadItemUpdate
{
    /// <summary>
    /// Gets the identifier of the component being replaced.
    /// </summary>
    public required string ComponentId { get; init; }

    /// <summary>
    /// Gets the replacement component payload.
    /// </summary>
    public required WidgetComponent Component { get; init; }
}

/// <summary>
/// Announces that a workflow task was appended.
/// </summary>
public sealed record WorkflowTaskAdded : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based task index that was added.
    /// </summary>
    public required int TaskIndex { get; init; }

    /// <summary>
    /// Gets the task payload that was added.
    /// </summary>
    public required WorkflowTask Task { get; init; }
}

/// <summary>
/// Announces that a workflow task was updated in place.
/// </summary>
public sealed record WorkflowTaskUpdated : ThreadItemUpdate
{
    /// <summary>
    /// Gets the zero-based task index being updated.
    /// </summary>
    public required int TaskIndex { get; init; }

    /// <summary>
    /// Gets the replacement task payload.
    /// </summary>
    public required WorkflowTask Task { get; init; }
}

/// <summary>
/// Announces progress for a generated image item.
/// </summary>
public sealed record GeneratedImageUpdated : ThreadItemUpdate
{
    /// <summary>
    /// Gets the generated image payload.
    /// </summary>
    public required GeneratedImage Image { get; init; }

    /// <summary>
    /// Gets the optional progress value between zero and one.
    /// </summary>
    public double? Progress { get; init; }
}

/// <summary>
/// Represents a streamed ChatKit event.
/// </summary>
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

/// <summary>
/// Announces that a thread was created.
/// </summary>
public sealed record ThreadCreatedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the created thread payload.
    /// </summary>
    public required Thread Thread { get; init; }
}

/// <summary>
/// Announces that thread metadata was updated.
/// </summary>
public sealed record ThreadUpdatedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the updated thread payload.
    /// </summary>
    public required Thread Thread { get; init; }
}

/// <summary>
/// Announces that a thread item was added.
/// </summary>
public sealed record ThreadItemAddedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the added thread item.
    /// </summary>
    public required ThreadItem Item { get; init; }
}

/// <summary>
/// Announces an incremental update to an existing thread item.
/// </summary>
public sealed record ThreadItemUpdatedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the identifier of the item being updated.
    /// </summary>
    public required string ItemId { get; init; }

    /// <summary>
    /// Gets the update payload to apply.
    /// </summary>
    public required ThreadItemUpdate Update { get; init; }
}

/// <summary>
/// Announces that a thread item completed and should be persisted.
/// </summary>
public sealed record ThreadItemDoneEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the completed item.
    /// </summary>
    public required ThreadItem Item { get; init; }
}

/// <summary>
/// Announces that a thread item was removed.
/// </summary>
public sealed record ThreadItemRemovedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the identifier of the removed item.
    /// </summary>
    public required string ItemId { get; init; }
}

/// <summary>
/// Announces that a thread item should be replaced.
/// </summary>
public sealed record ThreadItemReplacedEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the replacement item payload.
    /// </summary>
    public required ThreadItem Item { get; init; }
}

/// <summary>
/// Announces stream configuration for the current response.
/// </summary>
public sealed record StreamOptionsEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the stream options.
    /// </summary>
    public required StreamOptions StreamOptions { get; init; }
}

/// <summary>
/// Announces a user-visible progress update.
/// </summary>
public sealed record ProgressUpdateEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the optional progress icon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the progress message text.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Announces a client-side effect that should be performed outside the transcript.
/// </summary>
public sealed record ClientEffectEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the client effect name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets effect-specific data for the client.
    /// </summary>
    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Announces that an error occurred while processing the stream.
/// </summary>
public sealed record ErrorEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the protocol error code.
    /// </summary>
    public string Code { get; init; } = "custom";

    /// <summary>
    /// Gets the optional human-readable error message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets a value indicating whether the client may retry the failed request.
    /// </summary>
    public bool AllowRetry { get; init; }
}

/// <summary>
/// Announces a user-visible notice message.
/// </summary>
public sealed record NoticeEvent : ThreadStreamEvent
{
    /// <summary>
    /// Gets the notice severity or level.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets the notice body text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the optional notice title.
    /// </summary>
    public string? Title { get; init; }
}
