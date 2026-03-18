using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit;

public sealed record ChatKitAction
{
    public required string Type { get; init; }

    public JsonNode? Payload { get; init; }
}

public sealed record ActionConfig
{
    public required ChatKitAction Action { get; init; }

    public string? Label { get; init; }

    public string? ConfirmTitle { get; init; }

    public string? ConfirmBody { get; init; }

    public bool Destructive { get; init; }
}
