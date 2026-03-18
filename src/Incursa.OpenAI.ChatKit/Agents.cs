using System.Text.Json;
using System.Text.Json.Nodes;
using Incursa.OpenAI.Agents;

namespace Incursa.OpenAI.ChatKit;

public sealed record ClientToolCall
{
    public required string Name { get; init; }

    public Dictionary<string, JsonNode?> Arguments { get; init; } = new(StringComparer.Ordinal);
}

public sealed class AgentContext<TContext>
{
    private readonly Queue<ThreadStreamEvent> bufferedEvents = new();

    public required ThreadMetadata Thread { get; init; }

    public required ChatKitStore<TContext> Store { get; init; }

    public required TContext RequestContext { get; init; }

    public string? PreviousResponseId { get; set; }

    public ClientToolCall? ClientToolCall { get; set; }

    public WorkflowItem? WorkflowItem { get; set; }

    public GeneratedImageItem? GeneratedImageItem { get; set; }

    public string GenerateId(string itemType, ThreadMetadata? thread = null)
        => string.Equals(itemType, StoreItemTypes.Thread, StringComparison.Ordinal)
            ? Store.GenerateThreadId(RequestContext)
            : Store.GenerateItemId(itemType, thread ?? Thread, RequestContext);

    public Task StreamAsync(ThreadStreamEvent @event)
    {
        bufferedEvents.Enqueue(@event);
        return Task.CompletedTask;
    }

    public async Task StreamWidgetAsync(WidgetRoot widget, string? copyText = null)
    {
        await foreach (ThreadStreamEvent @event in WidgetStreaming.StreamAsync(Thread, widget, GenerateId(StoreItemTypes.Message), copyText).ConfigureAwait(false))
        {
            bufferedEvents.Enqueue(@event);
        }
    }

    public async IAsyncEnumerable<ThreadStreamEvent> DrainAsync()
    {
        while (bufferedEvents.Count > 0)
        {
            yield return bufferedEvents.Dequeue();
            await Task.Yield();
        }
    }
}

public static class ChatKitAgents
{
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
