using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Incursa.OpenAI.ChatKit;

public sealed record ThreadGetByIdParams
{
    public required string ThreadId { get; init; }
}

public sealed record ThreadCreateParams
{
    public required UserMessageInput Input { get; init; }
}

public sealed record ThreadListParams
{
    public int? Limit { get; init; }

    public string Order { get; init; } = "desc";

    public string? After { get; init; }
}

public sealed record ThreadAddUserMessageParams
{
    public required UserMessageInput Input { get; init; }

    public required string ThreadId { get; init; }
}

public sealed record ThreadAddClientToolOutputParams
{
    public required string ThreadId { get; init; }

    public JsonNode? Result { get; init; }
}

public sealed record ThreadCustomActionParams
{
    public required string ThreadId { get; init; }

    public string? ItemId { get; init; }

    public required ChatKitAction Action { get; init; }
}

public sealed record ThreadRetryAfterItemParams
{
    public required string ThreadId { get; init; }

    public required string ItemId { get; init; }
}

public sealed record ItemFeedbackParams
{
    public required string ThreadId { get; init; }

    public List<string> ItemIds { get; init; } = [];

    public required string Kind { get; init; }
}

public sealed record AttachmentDeleteParams
{
    public required string AttachmentId { get; init; }
}

public sealed record AttachmentCreateParams
{
    public required string Name { get; init; }

    public required long Size { get; init; }

    public required string MimeType { get; init; }
}

public sealed record InputTranscribeParams
{
    public required string AudioBase64 { get; init; }

    public required string MimeType { get; init; }
}

public sealed record ItemsListParams
{
    public required string ThreadId { get; init; }

    public int? Limit { get; init; }

    public string Order { get; init; } = "desc";

    public string? After { get; init; }
}

public sealed record ThreadUpdateParams
{
    public required string ThreadId { get; init; }

    public required string Title { get; init; }
}

public sealed record ThreadDeleteParams
{
    public required string ThreadId { get; init; }
}

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
    public Dictionary<string, JsonNode?> Metadata { get; init; } = new(StringComparer.Ordinal);
}

public sealed record ThreadsGetByIdRequest : ChatKitRequest
{
    public required ThreadGetByIdParams Params { get; init; }
}

public sealed record ThreadsCreateRequest : ChatKitRequest
{
    public required ThreadCreateParams Params { get; init; }
}

public sealed record ThreadsListRequest : ChatKitRequest
{
    public ThreadListParams Params { get; init; } = new();
}

public sealed record ThreadsAddUserMessageRequest : ChatKitRequest
{
    public required ThreadAddUserMessageParams Params { get; init; }
}

public sealed record ThreadsAddClientToolOutputRequest : ChatKitRequest
{
    public required ThreadAddClientToolOutputParams Params { get; init; }
}

public sealed record ThreadsCustomActionRequest : ChatKitRequest
{
    public required ThreadCustomActionParams Params { get; init; }
}

public sealed record ThreadsSyncCustomActionRequest : ChatKitRequest
{
    public required ThreadCustomActionParams Params { get; init; }
}

public sealed record ThreadsRetryAfterItemRequest : ChatKitRequest
{
    public required ThreadRetryAfterItemParams Params { get; init; }
}

public sealed record ItemsFeedbackRequest : ChatKitRequest
{
    public required ItemFeedbackParams Params { get; init; }
}

public sealed record AttachmentsDeleteRequest : ChatKitRequest
{
    public required AttachmentDeleteParams Params { get; init; }
}

public sealed record AttachmentsCreateRequest : ChatKitRequest
{
    public required AttachmentCreateParams Params { get; init; }
}

public sealed record InputTranscribeRequest : ChatKitRequest
{
    public required InputTranscribeParams Params { get; init; }
}

public sealed record ItemsListRequest : ChatKitRequest
{
    public required ItemsListParams Params { get; init; }
}

public sealed record ThreadsUpdateRequest : ChatKitRequest
{
    public required ThreadUpdateParams Params { get; init; }
}

public sealed record ThreadsDeleteRequest : ChatKitRequest
{
    public required ThreadDeleteParams Params { get; init; }
}
