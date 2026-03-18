using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

public static class ChatKitClock
{
    public static DateTime Now()
        => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
}

public sealed record Page<T>
{
    public List<T> Data { get; init; } = [];

    public bool HasMore { get; init; }

    public string? After { get; init; }
}

public sealed record StreamOptions
{
    public bool AllowCancel { get; init; }
}

public sealed record InferenceOptions
{
    public ToolChoice? ToolChoice { get; init; }

    public string? Model { get; init; }
}

public sealed record ToolChoice
{
    public required string Id { get; init; }
}

public sealed record AudioInput
{
    public required byte[] Data { get; init; }

    public required string MimeType { get; init; }

    public string MediaType => MimeType.Split(';', 2)[0];
}

public sealed record TranscriptionResult
{
    public required string Text { get; init; }
}

public sealed record AttachmentUploadDescriptor
{
    public required string Url { get; init; }

    public required string Method { get; init; }

    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.Ordinal);
}

public sealed record GeneratedImage
{
    public required string Id { get; init; }

    public required string Url { get; init; }
}

public sealed record SyncCustomActionResponse
{
    public ThreadItem? UpdatedItem { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ActiveStatus), "active")]
[JsonDerivedType(typeof(LockedStatus), "locked")]
[JsonDerivedType(typeof(ClosedStatus), "closed")]
public abstract record ThreadStatus;

public sealed record ActiveStatus : ThreadStatus;

public sealed record LockedStatus : ThreadStatus
{
    public string? Reason { get; init; }
}

public sealed record ClosedStatus : ThreadStatus
{
    public string? Reason { get; init; }
}

public record ThreadMetadata
{
    public string? Title { get; set; }

    public required string Id { get; init; }

    public required DateTime CreatedAt { get; init; }

    public ThreadStatus Status { get; init; } = new ActiveStatus();

    public List<string>? AllowedImageDomains { get; init; }

    public Dictionary<string, JsonNode?> Metadata { get; init; } = new(StringComparer.Ordinal);
}

public sealed record Thread : ThreadMetadata
{
    public Page<ThreadItem> Items { get; init; } = new();
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UserMessageTextContent), "input_text")]
[JsonDerivedType(typeof(UserMessageTagContent), "input_tag")]
public abstract record UserMessageContent;

public sealed record UserMessageTextContent : UserMessageContent
{
    public required string Text { get; init; }
}

public sealed record UserMessageTagContent : UserMessageContent
{
    public required string Id { get; init; }

    public required string Text { get; init; }

    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);

    public string? Group { get; init; }

    public bool Interactive { get; init; }
}

public sealed record UserMessageInput
{
    public List<UserMessageContent> Content { get; init; } = [];

    public List<string> Attachments { get; init; } = [];

    public string? QuotedText { get; init; }

    public InferenceOptions InferenceOptions { get; init; } = new();
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FileAttachment), "file")]
[JsonDerivedType(typeof(ImageAttachment), "image")]
public abstract record Attachment
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string MimeType { get; init; }

    public AttachmentUploadDescriptor? UploadDescriptor { get; init; }

    public string? ThreadId { get; init; }

    public Dictionary<string, JsonNode?>? Metadata { get; init; }
}

public sealed record FileAttachment : Attachment;

public sealed record ImageAttachment : Attachment
{
    public required string PreviewUrl { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(URLSource), "url")]
[JsonDerivedType(typeof(FileSource), "file")]
[JsonDerivedType(typeof(EntitySource), "entity")]
public abstract record Source
{
    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Timestamp { get; init; }

    public string? Group { get; init; }
}

public sealed record FileSource : Source
{
    public required string Filename { get; init; }
}

public sealed record URLSource : Source
{
    public required string Url { get; init; }

    public string? Attribution { get; init; }
}

public sealed record EntitySource : Source
{
    public required string Id { get; init; }

    public string? Icon { get; init; }

    public string? Label { get; init; }

    public string? InlineLabel { get; init; }

    public bool Interactive { get; init; }

    public Dictionary<string, JsonNode?> Data { get; init; } = new(StringComparer.Ordinal);

    public string? Preview { get; init; }
}

public sealed record Annotation
{
    public string Type { get; init; } = "annotation";

    public required Source Source { get; init; }

    public int? Index { get; init; }
}

public sealed record AssistantMessageContent
{
    public List<Annotation> Annotations { get; init; } = [];

    public required string Text { get; init; }

    public string Type { get; init; } = "output_text";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CustomTask), "custom")]
[JsonDerivedType(typeof(SearchTask), "web_search")]
[JsonDerivedType(typeof(ThoughtTask), "thought")]
[JsonDerivedType(typeof(FileTask), "file")]
[JsonDerivedType(typeof(ImageTask), "image")]
public abstract record WorkflowTask
{
    public string StatusIndicator { get; init; } = "none";
}

public sealed record CustomTask : WorkflowTask
{
    public string? Title { get; init; }

    public string? Icon { get; init; }

    public string? Content { get; init; }
}

public sealed record SearchTask : WorkflowTask
{
    public string? Title { get; init; }

    public string? TitleQuery { get; init; }

    public List<string> Queries { get; init; } = [];

    public List<URLSource> Sources { get; init; } = [];
}

public sealed record ThoughtTask : WorkflowTask
{
    public string? Title { get; init; }

    public required string Content { get; init; }
}

public sealed record FileTask : WorkflowTask
{
    public string? Title { get; init; }

    public List<FileSource> Sources { get; init; } = [];
}

public sealed record ImageTask : WorkflowTask
{
    public string? Title { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CustomSummary), "custom")]
[JsonDerivedType(typeof(DurationSummary), "duration")]
public abstract record WorkflowSummary;

public sealed record CustomSummary : WorkflowSummary
{
    public required string Title { get; init; }

    public string? Icon { get; init; }
}

public sealed record DurationSummary : WorkflowSummary
{
    public required int Duration { get; init; }
}

public sealed record Workflow
{
    public required string Type { get; init; }

    public List<WorkflowTask> Tasks { get; init; } = [];

    public WorkflowSummary? Summary { get; set; }

    public bool Expanded { get; set; }
}
