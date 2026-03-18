using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents a client-side action emitted from a widget or requested by the ChatKit runtime.
/// </summary>
public sealed record ChatKitAction
{
    /// <summary>
    /// Gets the action type identifier understood by the server.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the optional JSON payload supplied with the action.
    /// </summary>
    public JsonNode? Payload { get; init; }
}

/// <summary>
/// Describes how an action should be rendered and confirmed in the client.
/// </summary>
public sealed record ActionConfig
{
    /// <summary>
    /// Gets the action that should be posted back when the control is invoked.
    /// </summary>
    public required ChatKitAction Action { get; init; }

    /// <summary>
    /// Gets the label shown for the action trigger.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets the optional confirmation dialog title.
    /// </summary>
    public string? ConfirmTitle { get; init; }

    /// <summary>
    /// Gets the optional confirmation dialog body text.
    /// </summary>
    public string? ConfirmBody { get; init; }

    /// <summary>
    /// Gets a value indicating whether the action should be presented as destructive.
    /// </summary>
    public bool Destructive { get; init; }
}
