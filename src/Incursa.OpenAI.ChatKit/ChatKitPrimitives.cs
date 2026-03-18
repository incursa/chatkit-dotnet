using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Provides a single place to obtain ChatKit timestamps.
/// </summary>
public static class ChatKitClock
{
    /// <summary>
    /// Returns the current UTC time as an unspecified <see cref="DateTime"/> to match the ChatKit wire model.
    /// </summary>
    /// <returns>The current timestamp with an unspecified kind.</returns>
    public static DateTime Now()
        => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}

/// <summary>
/// Represents a paged collection in ChatKit responses.
/// </summary>
/// <typeparam name="T">The element type contained in the page.</typeparam>
public sealed record Page<T>
{
    /// <summary>
    /// Gets the page data.
    /// </summary>
    public List<T> Data { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether more results are available after this page.
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// Gets the cursor that can be used to request the next page.
    /// </summary>
    public string? After { get; init; }
}

/// <summary>
/// Controls optional capabilities for a streaming response.
/// </summary>
public sealed record StreamOptions
{
    /// <summary>
    /// Gets a value indicating whether the client may cancel the current stream.
    /// </summary>
    public bool AllowCancel { get; init; }
}

/// <summary>
/// Describes inference-time options associated with a user message.
/// </summary>
public sealed record InferenceOptions
{
    /// <summary>
    /// Gets the preferred tool choice to honor while generating the response.
    /// </summary>
    public ToolChoice? ToolChoice { get; init; }

    /// <summary>
    /// Gets the optional model override for the request.
    /// </summary>
    public string? Model { get; init; }
}

/// <summary>
/// Identifies a specific tool selection requested for inference.
/// </summary>
public sealed record ToolChoice
{
    /// <summary>
    /// Gets the tool identifier.
    /// </summary>
    public required string Id { get; init; }
}

/// <summary>
/// Represents an audio payload submitted for transcription.
/// </summary>
public sealed record AudioInput
{
    /// <summary>
    /// Gets the raw audio bytes.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets the MIME type of the audio payload.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Gets the media type portion of <see cref="MimeType"/>.
    /// </summary>
    public string MediaType => MimeType.Split(';', 2)[0];
}

/// <summary>
/// Represents the result of an audio transcription request.
/// </summary>
public sealed record TranscriptionResult
{
    /// <summary>
    /// Gets the transcribed text.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Describes how the client should upload an attachment to external storage.
/// </summary>
public sealed record AttachmentUploadDescriptor
{
    /// <summary>
    /// Gets the upload URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the HTTP method the client should use when uploading.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the HTTP headers that should accompany the upload request.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents an image generated during a conversation.
/// </summary>
public sealed record GeneratedImage
{
    /// <summary>
    /// Gets the generated image identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the generated image URL.
    /// </summary>
    public required string Url { get; init; }
}

/// <summary>
/// Represents the result of a synchronous custom action invocation.
/// </summary>
public sealed record SyncCustomActionResponse
{
    /// <summary>
    /// Gets the optional thread item that should replace or update the sender item.
    /// </summary>
    public ThreadItem? UpdatedItem { get; init; }
}

/// <summary>
/// Represents the current lifecycle state of a thread.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ActiveStatus), "active")]
[JsonDerivedType(typeof(LockedStatus), "locked")]
[JsonDerivedType(typeof(ClosedStatus), "closed")]
public abstract record ThreadStatus;

/// <summary>
/// Indicates that a thread is active and can accept new input.
/// </summary>
public sealed record ActiveStatus : ThreadStatus;

/// <summary>
/// Indicates that a thread is locked and cannot accept new input.
/// </summary>
public sealed record LockedStatus : ThreadStatus
{
    /// <summary>
    /// Gets the optional reason the thread was locked.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Indicates that a thread is closed and no longer active.
/// </summary>
public sealed record ClosedStatus : ThreadStatus
{
    /// <summary>
    /// Gets the optional reason the thread was closed.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Represents thread metadata shared by thread list and detail responses.
/// </summary>
public record ThreadMetadata
{
    /// <summary>
    /// Gets or sets the display title of the thread.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets the unique thread identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the thread creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the current thread status.
    /// </summary>
    public ThreadStatus Status { get; init; } = new ActiveStatus();

    /// <summary>
    /// Gets the allowed image domains for the thread, when restricted.
    /// </summary>
    public List<string>? AllowedImageDomains { get; init; }

    /// <summary>
    /// Gets additional thread metadata.
    /// </summary>
    public Dictionary<string, JsonNode?> Metadata { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents a full thread response including items.
/// </summary>
public sealed record Thread : ThreadMetadata
{
    /// <summary>
    /// Gets the paged thread items.
    /// </summary>
    public Page<ThreadItem> Items { get; init; } = new();
}

/// <summary>
/// Represents a single piece of user-authored message content.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UserMessageTextContent), "input_text")]
[JsonDerivedType(typeof(UserMessageTagContent), "input_tag")]
public abstract record UserMessageContent;

/// <summary>
/// Represents plain text entered by the user.
/// </summary>
public sealed record UserMessageTextContent : UserMessageContent
{
    /// <summary>
    /// Gets the text content.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Represents a tagged user input value with optional structured data.
/// </summary>
public sealed record UserMessageTagContent : UserMessageContent
{
    /// <summary>
    /// Gets the tag identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the visible tag text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the structured data associated with the tag.
    /// </summary>
    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the optional group name used for organizing related tags.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tag is interactive in the client UI.
    /// </summary>
    public bool Interactive { get; init; }
}

/// <summary>
/// Represents user input submitted when creating or extending a thread.
/// </summary>
public sealed record UserMessageInput
{
    /// <summary>
    /// Gets the user message content parts.
    /// </summary>
    public List<UserMessageContent> Content { get; init; } = [];

    /// <summary>
    /// Gets attachment identifiers that should be associated with the message.
    /// </summary>
    public List<string> Attachments { get; init; } = [];

    /// <summary>
    /// Gets the optional quoted text included with the message.
    /// </summary>
    public string? QuotedText { get; init; }

    /// <summary>
    /// Gets the inference options that should apply to the message.
    /// </summary>
    public InferenceOptions InferenceOptions { get; init; } = new();
}

/// <summary>
/// Represents an attachment referenced by a message.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FileAttachment), "file")]
[JsonDerivedType(typeof(ImageAttachment), "image")]
public abstract record Attachment
{
    /// <summary>
    /// Gets the attachment identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the original attachment file name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the attachment MIME type.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Gets the optional external upload instructions for the attachment.
    /// </summary>
    public AttachmentUploadDescriptor? UploadDescriptor { get; init; }

    /// <summary>
    /// Gets the thread identifier associated with the attachment, when attached to a thread.
    /// </summary>
    public string? ThreadId { get; init; }

    /// <summary>
    /// Gets optional metadata associated with the attachment.
    /// </summary>
    public Dictionary<string, JsonNode?>? Metadata { get; init; }
}

/// <summary>
/// Represents a generic file attachment.
/// </summary>
public sealed record FileAttachment : Attachment;

/// <summary>
/// Represents an image attachment with a preview URL.
/// </summary>
public sealed record ImageAttachment : Attachment
{
    /// <summary>
    /// Gets the preview URL that clients can display inline.
    /// </summary>
    public required string PreviewUrl { get; init; }
}

/// <summary>
/// Represents a citation or source reference attached to assistant content.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(URLSource), "url")]
[JsonDerivedType(typeof(FileSource), "file")]
[JsonDerivedType(typeof(EntitySource), "entity")]
public abstract record Source
{
    /// <summary>
    /// Gets the source title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the optional source description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional timestamp string associated with the source.
    /// </summary>
    public string? Timestamp { get; init; }

    /// <summary>
    /// Gets the optional source grouping label.
    /// </summary>
    public string? Group { get; init; }
}

/// <summary>
/// Represents a file-backed source reference.
/// </summary>
public sealed record FileSource : Source
{
    /// <summary>
    /// Gets the referenced file name.
    /// </summary>
    public required string Filename { get; init; }
}

/// <summary>
/// Represents a URL-backed source reference.
/// </summary>
public sealed record URLSource : Source
{
    /// <summary>
    /// Gets the referenced URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets optional attribution text for the source.
    /// </summary>
    public string? Attribution { get; init; }
}

/// <summary>
/// Represents an entity-backed source reference with structured data.
/// </summary>
public sealed record EntitySource : Source
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the optional icon for the entity.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the optional label shown for the entity.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets the optional inline label shown within content.
    /// </summary>
    public string? InlineLabel { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entity is interactive.
    /// </summary>
    public bool Interactive { get; init; }

    /// <summary>
    /// Gets structured entity data passed through to the client.
    /// </summary>
    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the optional preview text for the entity.
    /// </summary>
    public string? Preview { get; init; }
}

/// <summary>
/// Represents an annotation attached to assistant message content.
/// </summary>
public sealed record Annotation
{
    /// <summary>
    /// Gets the annotation type identifier.
    /// </summary>
    public string Type { get; init; } = "annotation";

    /// <summary>
    /// Gets the source referenced by the annotation.
    /// </summary>
    public required Source Source { get; init; }

    /// <summary>
    /// Gets the optional character index associated with the annotation.
    /// </summary>
    public int? Index { get; init; }
}

/// <summary>
/// Represents a single assistant output content part.
/// </summary>
public sealed record AssistantMessageContent
{
    /// <summary>
    /// Gets the annotations attached to the content part.
    /// </summary>
    public List<Annotation> Annotations { get; init; } = [];

    /// <summary>
    /// Gets the assistant text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the content type identifier.
    /// </summary>
    public string Type { get; init; } = "output_text";
}

/// <summary>
/// Represents a workflow task displayed in the client.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CustomTask), "custom")]
[JsonDerivedType(typeof(SearchTask), "web_search")]
[JsonDerivedType(typeof(ThoughtTask), "thought")]
[JsonDerivedType(typeof(FileTask), "file")]
[JsonDerivedType(typeof(ImageTask), "image")]
public abstract record WorkflowTask
{
    /// <summary>
    /// Gets the status indicator shown by the client.
    /// </summary>
    public string StatusIndicator { get; init; } = "none";
}

/// <summary>
/// Represents a custom workflow task.
/// </summary>
public sealed record CustomTask : WorkflowTask
{
    /// <summary>
    /// Gets the optional task title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the optional task icon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the optional task body content.
    /// </summary>
    public string? Content { get; init; }
}

/// <summary>
/// Represents a web search workflow task.
/// </summary>
public sealed record SearchTask : WorkflowTask
{
    /// <summary>
    /// Gets the optional task title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the query string highlighted as the search title.
    /// </summary>
    public string? TitleQuery { get; init; }

    /// <summary>
    /// Gets the queries issued by the task.
    /// </summary>
    public List<string> Queries { get; init; } = [];

    /// <summary>
    /// Gets the sources returned by the search.
    /// </summary>
    public List<URLSource> Sources { get; init; } = [];
}

/// <summary>
/// Represents a reasoning or thought workflow task.
/// </summary>
public sealed record ThoughtTask : WorkflowTask
{
    /// <summary>
    /// Gets the optional task title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the task body content.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Represents a file-focused workflow task.
/// </summary>
public sealed record FileTask : WorkflowTask
{
    /// <summary>
    /// Gets the optional task title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the file sources referenced by the task.
    /// </summary>
    public List<FileSource> Sources { get; init; } = [];
}

/// <summary>
/// Represents an image-oriented workflow task.
/// </summary>
public sealed record ImageTask : WorkflowTask
{
    /// <summary>
    /// Gets the optional task title.
    /// </summary>
    public string? Title { get; init; }
}

/// <summary>
/// Represents the summary shown for a workflow.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CustomSummary), "custom")]
[JsonDerivedType(typeof(DurationSummary), "duration")]
public abstract record WorkflowSummary;

/// <summary>
/// Represents a workflow summary with explicit title and icon.
/// </summary>
public sealed record CustomSummary : WorkflowSummary
{
    /// <summary>
    /// Gets the summary title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the optional summary icon.
    /// </summary>
    public string? Icon { get; init; }
}

/// <summary>
/// Represents a workflow summary that reports duration.
/// </summary>
public sealed record DurationSummary : WorkflowSummary
{
    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    public required int Duration { get; init; }
}

/// <summary>
/// Represents a workflow and its current task list.
/// </summary>
public sealed record Workflow
{
    /// <summary>
    /// Gets the workflow type identifier.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the workflow tasks.
    /// </summary>
    public List<WorkflowTask> Tasks { get; init; } = [];

    /// <summary>
    /// Gets or sets the optional workflow summary.
    /// </summary>
    public WorkflowSummary? Summary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the workflow should be expanded in the client.
    /// </summary>
    public bool Expanded { get; set; }
}
