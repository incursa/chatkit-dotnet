using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Carries parameters for a <c>threads.get_by_id</c> request.
/// </summary>
public sealed record ThreadGetByIdParams
{
    /// <summary>
    /// Gets the identifier of the thread to load.
    /// </summary>
    public required string ThreadId { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.create</c> request.
/// </summary>
public sealed record ThreadCreateParams
{
    /// <summary>
    /// Gets the initial user input that should seed the new thread.
    /// </summary>
    public required UserMessageInput Input { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.list</c> request.
/// </summary>
public sealed record ThreadListParams
{
    /// <summary>
    /// Gets the maximum number of threads to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the sort order for thread results.
    /// </summary>
    public string Order { get; init; } = "desc";

    /// <summary>
    /// Gets the pagination cursor to continue listing after.
    /// </summary>
    public string? After { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.add_user_message</c> request.
/// </summary>
public sealed record ThreadAddUserMessageParams
{
    /// <summary>
    /// Gets the user input to append to the thread.
    /// </summary>
    public required UserMessageInput Input { get; init; }

    /// <summary>
    /// Gets the identifier of the thread that should receive the message.
    /// </summary>
    public required string ThreadId { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.add_client_tool_output</c> request.
/// </summary>
public sealed record ThreadAddClientToolOutputParams
{
    /// <summary>
    /// Gets the identifier of the thread that owns the pending tool call.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the optional tool output payload returned by the client.
    /// </summary>
    public JsonNode? Result { get; init; }
}

/// <summary>
/// Carries parameters for custom action requests.
/// </summary>
public sealed record ThreadCustomActionParams
{
    /// <summary>
    /// Gets the identifier of the target thread.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the optional identifier of the widget item that triggered the action.
    /// </summary>
    public string? ItemId { get; init; }

    /// <summary>
    /// Gets the action to execute.
    /// </summary>
    public required ChatKitAction Action { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.retry_after_item</c> request.
/// </summary>
public sealed record ThreadRetryAfterItemParams
{
    /// <summary>
    /// Gets the identifier of the thread to replay.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the item identifier after which later items should be discarded and retried.
    /// </summary>
    public required string ItemId { get; init; }
}

/// <summary>
/// Carries parameters for an <c>items.feedback</c> request.
/// </summary>
public sealed record ItemFeedbackParams
{
    /// <summary>
    /// Gets the identifier of the thread that owns the rated items.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the identifiers of the items receiving feedback.
    /// </summary>
    public List<string> ItemIds { get; init; } = [];

    /// <summary>
    /// Gets the feedback kind submitted by the client.
    /// </summary>
    public required string Kind { get; init; }
}

/// <summary>
/// Carries parameters for an <c>attachments.delete</c> request.
/// </summary>
public sealed record AttachmentDeleteParams
{
    /// <summary>
    /// Gets the identifier of the attachment to delete.
    /// </summary>
    public required string AttachmentId { get; init; }
}

/// <summary>
/// Carries parameters for an <c>attachments.create</c> request.
/// </summary>
public sealed record AttachmentCreateParams
{
    /// <summary>
    /// Gets the original attachment file name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the attachment size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the attachment MIME type.
    /// </summary>
    public required string MimeType { get; init; }
}

/// <summary>
/// Carries parameters for an <c>input.transcribe</c> request.
/// </summary>
public sealed record InputTranscribeParams
{
    /// <summary>
    /// Gets the base64-encoded audio payload to transcribe.
    /// </summary>
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// Gets the MIME type describing the encoded audio payload.
    /// </summary>
    public required string MimeType { get; init; }
}

/// <summary>
/// Carries parameters for an <c>items.list</c> request.
/// </summary>
public sealed record ItemsListParams
{
    /// <summary>
    /// Gets the identifier of the thread whose items should be listed.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the maximum number of items to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the sort order for thread items.
    /// </summary>
    public string Order { get; init; } = "desc";

    /// <summary>
    /// Gets the pagination cursor to continue listing after.
    /// </summary>
    public string? After { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.update</c> request.
/// </summary>
public sealed record ThreadUpdateParams
{
    /// <summary>
    /// Gets the identifier of the thread to update.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets the new thread title.
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Carries parameters for a <c>threads.delete</c> request.
/// </summary>
public sealed record ThreadDeleteParams
{
    /// <summary>
    /// Gets the identifier of the thread to delete.
    /// </summary>
    public required string ThreadId { get; init; }
}

/// <summary>
/// Represents a ChatKit protocol request envelope.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ThreadsGetByIdRequest), "threads.get_by_id")]
[JsonDerivedType(typeof(ThreadsCreateRequest), "threads.create")]
[JsonDerivedType(typeof(ThreadsListRequest), "threads.list")]
[JsonDerivedType(typeof(ThreadsAddUserMessageRequest), "threads.add_user_message")]
[JsonDerivedType(typeof(ThreadsAddClientToolOutputRequest), "threads.add_client_tool_output")]
[JsonDerivedType(typeof(ThreadsCustomActionRequest), "threads.custom_action")]
[JsonDerivedType(typeof(ThreadsSyncCustomActionRequest), "threads.sync_custom_action")]
[JsonDerivedType(typeof(ThreadsRetryAfterItemRequest), "threads.retry_after_item")]
[JsonDerivedType(typeof(ItemsFeedbackRequest), "items.feedback")]
[JsonDerivedType(typeof(AttachmentsDeleteRequest), "attachments.delete")]
[JsonDerivedType(typeof(AttachmentsCreateRequest), "attachments.create")]
[JsonDerivedType(typeof(InputTranscribeRequest), "input.transcribe")]
[JsonDerivedType(typeof(ItemsListRequest), "items.list")]
[JsonDerivedType(typeof(ThreadsUpdateRequest), "threads.update")]
[JsonDerivedType(typeof(ThreadsDeleteRequest), "threads.delete")]
public abstract record ChatKitRequest
{
    /// <summary>
    /// Gets request-scoped metadata that should flow through processing.
    /// </summary>
    public Dictionary<string, JsonNode?> Metadata { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Represents a <c>threads.get_by_id</c> request.
/// </summary>
public sealed record ThreadsGetByIdRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadGetByIdParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.create</c> request.
/// </summary>
public sealed record ThreadsCreateRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadCreateParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.list</c> request.
/// </summary>
public sealed record ThreadsListRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public ThreadListParams Params { get; init; } = new();
}

/// <summary>
/// Represents a <c>threads.add_user_message</c> request.
/// </summary>
public sealed record ThreadsAddUserMessageRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadAddUserMessageParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.add_client_tool_output</c> request.
/// </summary>
public sealed record ThreadsAddClientToolOutputRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadAddClientToolOutputParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.custom_action</c> request.
/// </summary>
public sealed record ThreadsCustomActionRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadCustomActionParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.sync_custom_action</c> request.
/// </summary>
public sealed record ThreadsSyncCustomActionRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadCustomActionParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.retry_after_item</c> request.
/// </summary>
public sealed record ThreadsRetryAfterItemRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadRetryAfterItemParams Params { get; init; }
}

/// <summary>
/// Represents an <c>items.feedback</c> request.
/// </summary>
public sealed record ItemsFeedbackRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ItemFeedbackParams Params { get; init; }
}

/// <summary>
/// Represents an <c>attachments.delete</c> request.
/// </summary>
public sealed record AttachmentsDeleteRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required AttachmentDeleteParams Params { get; init; }
}

/// <summary>
/// Represents an <c>attachments.create</c> request.
/// </summary>
public sealed record AttachmentsCreateRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required AttachmentCreateParams Params { get; init; }
}

/// <summary>
/// Represents an <c>input.transcribe</c> request.
/// </summary>
public sealed record InputTranscribeRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required InputTranscribeParams Params { get; init; }
}

/// <summary>
/// Represents an <c>items.list</c> request.
/// </summary>
public sealed record ItemsListRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ItemsListParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.update</c> request.
/// </summary>
public sealed record ThreadsUpdateRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadUpdateParams Params { get; init; }
}

/// <summary>
/// Represents a <c>threads.delete</c> request.
/// </summary>
public sealed record ThreadsDeleteRequest : ChatKitRequest
{
    /// <summary>
    /// Gets the typed request parameters.
    /// </summary>
    public required ThreadDeleteParams Params { get; init; }
}
