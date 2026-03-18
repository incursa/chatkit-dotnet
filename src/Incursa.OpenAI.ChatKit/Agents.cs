using System.Text.Json;
using System.Text.Json.Nodes;
using Incursa.OpenAI.Agents;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents a client tool call surfaced through the ChatKit agent bridge.
/// </summary>
public sealed record ClientToolCall
{
    /// <summary>
    /// Gets the client tool name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the arguments passed to the client tool.
    /// </summary>
    public Dictionary<string, JsonNode?> Arguments { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Carries request-scoped state while converting agent output into ChatKit stream events.
/// </summary>
/// <typeparam name="TContext">The application request context type.</typeparam>
public sealed class AgentContext<TContext>
{
    private readonly Queue<ThreadStreamEvent> bufferedEvents = new();

    /// <summary>
    /// Gets the thread being processed.
    /// </summary>
    public required ThreadMetadata Thread { get; init; }

    /// <summary>
    /// Gets the ChatKit store used for identifiers and persistence.
    /// </summary>
    public required ChatKitStore<TContext> Store { get; init; }

    /// <summary>
    /// Gets the application request context.
    /// </summary>
    public required TContext RequestContext { get; init; }

    /// <summary>
    /// Gets or sets the previous response identifier used for continuation.
    /// </summary>
    public string? PreviousResponseId { get; set; }

    /// <summary>
    /// Gets or sets the current pending client tool call, when applicable.
    /// </summary>
    public ClientToolCall? ClientToolCall { get; set; }

    /// <summary>
    /// Gets or sets the current workflow item being streamed.
    /// </summary>
    public WorkflowItem? WorkflowItem { get; set; }

    /// <summary>
    /// Gets or sets the generated image item being updated.
    /// </summary>
    public GeneratedImageItem? GeneratedImageItem { get; set; }

    /// <summary>
    /// Generates a ChatKit identifier for a new item.
    /// </summary>
    /// <param name="itemType">The ChatKit item type being created.</param>
    /// <param name="thread">The optional thread override to use when generating the identifier.</param>
    /// <returns>A new ChatKit identifier.</returns>
    public string GenerateId(string itemType, ThreadMetadata? thread = null)
        => string.Equals(itemType, StoreItemTypes.Thread, StringComparison.Ordinal)
            ? Store.GenerateThreadId(RequestContext)
            : Store.GenerateItemId(itemType, thread ?? Thread, RequestContext);

    /// <summary>
    /// Buffers a stream event so it can be emitted after the agent turn finishes.
    /// </summary>
    /// <param name="event">The event to buffer.</param>
    /// <returns>A completed task.</returns>
    public Task StreamAsync(ThreadStreamEvent @event)
    {
        bufferedEvents.Enqueue(@event);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Buffers the stream events required to render a widget item.
    /// </summary>
    /// <param name="widget">The widget tree to stream.</param>
    /// <param name="copyText">Optional copy text associated with the widget.</param>
    /// <returns>A task that completes when the widget events have been buffered.</returns>
    public async Task StreamWidgetAsync(WidgetRoot widget, string? copyText = null)
    {
        await foreach (ThreadStreamEvent @event in WidgetStreaming.StreamAsync(Thread, widget, GenerateId(StoreItemTypes.Message), copyText).ConfigureAwait(false))
        {
            bufferedEvents.Enqueue(@event);
        }
    }

    /// <summary>
    /// Drains all buffered events in FIFO order.
    /// </summary>
    /// <returns>An async sequence of buffered stream events.</returns>
    public async IAsyncEnumerable<ThreadStreamEvent> DrainAsync()
    {
        while (bufferedEvents.Count > 0)
        {
            yield return bufferedEvents.Dequeue();
            await Task.Yield();
        }
    }
}

/// <summary>
/// Provides helpers for bridging the agent runtime and ChatKit contracts.
/// </summary>
public static class ChatKitAgents
{
    /// <summary>
    /// Converts a subset of ChatKit thread items into agent conversation input items.
    /// </summary>
    /// <param name="threadItems">The thread items to convert.</param>
    /// <returns>The converted agent conversation items.</returns>
    public static IReadOnlyList<AgentConversationItem> SimpleToAgentInput(IEnumerable<ThreadItem> threadItems)
    {
        List<AgentConversationItem> items = [];
        foreach (ThreadItem item in threadItems)
        {
            switch (item)
            {
                case UserMessageItem user:
                    items.Add(new AgentConversationItem(
                        AgentItemTypes.UserInput,
                        "user",
                        "chatkit",
                        null,
                        string.Join("\n", user.Content.OfType<UserMessageTextContent>().Select(x => x.Text)),
                        null,
                        null,
                        null,
                        null));
                    break;
                case AssistantMessageItem assistant:
                    items.Add(new AgentConversationItem(
                        AgentItemTypes.MessageOutput,
                        "assistant",
                        "chatkit",
                        null,
                        string.Join("\n", assistant.Content.Select(x => x.Text)),
                        null,
                        null,
                        null,
                        null));
                    break;
                case ClientToolCallItem tool:
                    items.Add(new AgentConversationItem(
                        AgentItemTypes.ToolCall,
                        "assistant",
                        "chatkit",
                        tool.Name,
                        null,
                        tool.CallId,
                        JsonSerializer.SerializeToNode(tool.Arguments, ChatKitJson.SerializerOptions),
                        tool.Status,
                        null));
                    break;
            }
        }

        return items;
    }

    /// <summary>
    /// Maps an agent event stream to ChatKit thread stream events.
    /// </summary>
    /// <typeparam name="TContext">The application request context type.</typeparam>
    /// <param name="agentContext">The ChatKit agent context for the current request.</param>
    /// <param name="stream">The agent event stream to translate.</param>
    /// <returns>An async sequence of ChatKit stream events.</returns>
    public static async IAsyncEnumerable<ThreadStreamEvent> StreamAgentResponse<TContext>(
        AgentContext<TContext> agentContext,
        IAsyncEnumerable<AgentStreamEvent> stream)
    {
        await foreach (AgentStreamEvent @event in stream.ConfigureAwait(false))
        {
            if (@event.Item is null)
            {
                continue;
            }

            switch (@event.Item)
            {
                case { ItemType: var itemType, Role: "assistant", Text: { } text } when string.Equals(itemType, AgentItemTypes.MessageOutput, StringComparison.Ordinal):
                    yield return new ThreadItemDoneEvent
                    {
                        Item = new AssistantMessageItem
                        {
                            Id = agentContext.GenerateId(StoreItemTypes.Message),
                            ThreadId = agentContext.Thread.Id,
                            CreatedAt = ChatKitClock.Now(),
                            Content = [new AssistantMessageContent { Text = text }],
                        },
                    };
                    break;
                case { ItemType: var itemType, Role: "assistant", Name: { } name, ToolCallId: { } toolCallId, Data: JsonNode arguments } when string.Equals(itemType, AgentItemTypes.ToolCall, StringComparison.Ordinal):
                    yield return new ThreadItemDoneEvent
                    {
                        Item = new ClientToolCallItem
                        {
                            Id = agentContext.GenerateId(StoreItemTypes.ToolCall),
                            ThreadId = agentContext.Thread.Id,
                            CreatedAt = ChatKitClock.Now(),
                            CallId = toolCallId,
                            Name = name,
                            Arguments = arguments as JsonObject is { } argsObj
                                ? argsObj.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal)
                                : new Dictionary<string, JsonNode?>(StringComparer.Ordinal),
                            Status = "pending",
                        },
                    };
                    break;
            }
        }

        await foreach (ThreadStreamEvent @event in agentContext.DrainAsync().ConfigureAwait(false))
        {
            yield return @event;
        }
    }
}
