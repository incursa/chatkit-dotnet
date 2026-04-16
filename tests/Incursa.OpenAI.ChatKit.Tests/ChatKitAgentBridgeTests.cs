using System.Text.Json.Nodes;
using Incursa.OpenAI.Agents;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitAgentBridgeTests
{
    /// <summary>Agent context delegates thread and item identifier generation to the backing store using the expected item type and thread inputs.</summary>
    /// <intent>Protect the bridge layer from generating incorrect ChatKit identifiers when materializing agent output.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Generating a thread id uses the store thread path, and generating an item id uses the provided item type and thread metadata.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void AgentContext_GenerateId_UsesExpectedStorePath()
    {
        RecordingStore store = new();
        ThreadMetadata primaryThread = CreateThread("thr_primary");
        ThreadMetadata overrideThread = CreateThread("thr_override");
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(store, primaryThread);

        string generatedThreadId = context.GenerateId(StoreItemTypes.Thread);
        string generatedMessageId = context.GenerateId(StoreItemTypes.Message, overrideThread);

        Assert.Equal("thr_generated", generatedThreadId);
        Assert.Equal("message_for_thr_override", generatedMessageId);
        Assert.Equal(1, store.GenerateThreadIdCalls);
        Assert.Collection(
            store.GenerateItemIdCalls,
            call =>
            {
                Assert.Equal(StoreItemTypes.Message, call.ItemType);
                Assert.Equal("thr_override", call.ThreadId);
            });
    }

    /// <summary>Buffered bridge events drain in FIFO order after they are recorded.</summary>
    /// <intent>Protect the bridge layer from reordering events that should be emitted after an agent turn completes.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Buffered events drain in the same order they were enqueued.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task AgentContext_StreamAsync_ThenDrainAsync_PreservesFifoOrder()
    {
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(new RecordingStore(), CreateThread("thr_1"));
        await context.StreamAsync(new ProgressUpdateEvent { Text = "first" });
        await context.StreamAsync(new NoticeEvent { Level = "info", Message = "second" });

        List<ThreadStreamEvent> drained = await ToListAsync(context.DrainAsync());

        Assert.Collection(
            drained,
            first => Assert.Equal("first", Assert.IsType<ProgressUpdateEvent>(first).Text),
            second => Assert.Equal("second", Assert.IsType<NoticeEvent>(second).Message));
    }

    /// <summary>Widget buffering materializes a widget item event with the thread, copy text, and generated item identifier.</summary>
    /// <intent>Protect widget bridge flows from dropping buffered widget events or generating them against the wrong thread context.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Streaming a widget through the agent context buffers a completed widget item event that can be drained afterward.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task AgentContext_StreamWidgetAsync_BuffersWidgetItemDoneEvent()
    {
        RecordingStore store = new();
        ThreadMetadata thread = CreateThread("thr_widget");
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(store, thread);
        WidgetRoot widget = new()
        {
            Type = "Card",
            Id = "root",
        };

        await context.StreamWidgetAsync(widget, "Copy me");

        ThreadItemDoneEvent doneEvent = Assert.IsType<ThreadItemDoneEvent>(Assert.Single(await ToListAsync(context.DrainAsync())));
        WidgetItem widgetItem = Assert.IsType<WidgetItem>(doneEvent.Item);
        Assert.Equal("message_for_thr_widget", widgetItem.Id);
        Assert.Equal(thread.Id, widgetItem.ThreadId);
        Assert.Same(widget, widgetItem.Widget);
        Assert.Equal("Copy me", widgetItem.CopyText);
        Assert.Collection(
            store.GenerateItemIdCalls,
            call =>
            {
                Assert.Equal(StoreItemTypes.Message, call.ItemType);
                Assert.Equal("thr_widget", call.ThreadId);
            });
    }

    /// <summary>Assistant message output is translated into a completed assistant thread item before any buffered post-turn events are drained.</summary>
    /// <intent>Protect the public ChatKit transcript ordering when upstream agent output is converted into ChatKit stream events.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Assistant message output emits a thread item done event, and buffered bridge events are emitted only after the agent stream finishes.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task StreamAgentResponse_MapsAssistantOutput_AndDrainsBufferedEventsAfterStream()
    {
        RecordingStore store = new();
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(store, CreateThread("thr_assistant"));
        await context.StreamAsync(new NoticeEvent { Level = "info", Message = "buffered" });

        List<ThreadStreamEvent> events = await ToListAsync(ChatKitAgents.StreamAgentResponse(
            context,
            ToAsyncEnumerable(
            [
                new AgentStreamEvent(
                    "item.completed",
                    "demo-agent",
                    new AgentRunItem(
                        AgentItemTypes.MessageOutput,
                        "assistant",
                        "demo-agent",
                        null,
                        "pong",
                        null,
                        null,
                        null,
                        null),
                    null,
                    null,
                    null),
            ])));

        Assert.Collection(
            events,
            first =>
            {
                ThreadItemDoneEvent done = Assert.IsType<ThreadItemDoneEvent>(first);
                AssistantMessageItem item = Assert.IsType<AssistantMessageItem>(done.Item);
                Assert.Equal("message_for_thr_assistant", item.Id);
                Assert.Equal("pong", Assert.Single(item.Content).Text);
            },
            second => Assert.Equal("buffered", Assert.IsType<NoticeEvent>(second).Message));
    }

    /// <summary>Tool-call agent output is translated into a pending client tool call item with object arguments preserved.</summary>
    /// <intent>Protect client tool interoperability between upstream agent output and the ChatKit transcript model.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Assistant tool-call output emits a pending client tool call item with the original tool name, call id, and object arguments.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task StreamAgentResponse_MapsToolCallOutput_WithObjectArguments()
    {
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(new RecordingStore(), CreateThread("thr_tool"));

        List<ThreadStreamEvent> events = await ToListAsync(ChatKitAgents.StreamAgentResponse(
            context,
            ToAsyncEnumerable(
            [
                new AgentStreamEvent(
                    "item.completed",
                    "demo-agent",
                    new AgentRunItem(
                        AgentItemTypes.ToolCall,
                        "assistant",
                        "demo-agent",
                        "lookup_contact",
                        null,
                        "call_1",
                        JsonNode.Parse("""{"query":"Ada"}"""),
                        "completed",
                        null),
                    null,
                    null,
                    null),
            ])));

        ThreadItemDoneEvent done = Assert.IsType<ThreadItemDoneEvent>(Assert.Single(events));
        ClientToolCallItem item = Assert.IsType<ClientToolCallItem>(done.Item);
        Assert.Equal("tool_call_for_thr_tool", item.Id);
        Assert.Equal("lookup_contact", item.Name);
        Assert.Equal("call_1", item.CallId);
        Assert.Equal("pending", item.Status);
        Assert.Equal("Ada", item.Arguments["query"]?.GetValue<string>());
    }

    /// <summary>Non-object tool-call argument payloads are normalized to an empty ChatKit argument bag instead of causing translation failures.</summary>
    /// <intent>Protect the bridge layer from surfacing malformed upstream tool-call argument payloads as invalid ChatKit argument dictionaries.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Assistant tool-call output with a non-object data payload emits a client tool call item whose arguments collection is empty.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task StreamAgentResponse_ToolCallWithNonObjectArguments_UsesEmptyArgumentDictionary()
    {
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(new RecordingStore(), CreateThread("thr_tool"));

        List<ThreadStreamEvent> events = await ToListAsync(ChatKitAgents.StreamAgentResponse(
            context,
            ToAsyncEnumerable(
            [
                new AgentStreamEvent(
                    "item.completed",
                    "demo-agent",
                    new AgentRunItem(
                        AgentItemTypes.ToolCall,
                        "assistant",
                        "demo-agent",
                        "lookup_contact",
                        null,
                        "call_1",
                        JsonNode.Parse("""["Ada"]"""),
                        null,
                        null),
                    null,
                    null,
                    null),
            ])));

        ThreadItemDoneEvent done = Assert.IsType<ThreadItemDoneEvent>(Assert.Single(events));
        ClientToolCallItem item = Assert.IsType<ClientToolCallItem>(done.Item);
        Assert.Empty(item.Arguments);
    }

    /// <summary>Null items and unsupported agent output shapes are ignored instead of leaking partial or invalid transcript events.</summary>
    /// <intent>Protect the bridge layer from surfacing unsupported upstream items into the public ChatKit event stream.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Agent events without an item or with unsupported role/type combinations do not emit any ChatKit stream events.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task StreamAgentResponse_IgnoresNullAndUnsupportedItems()
    {
        AgentContext<Dictionary<string, object?>> context = CreateAgentContext(new RecordingStore(), CreateThread("thr_ignored"));

        List<ThreadStreamEvent> events = await ToListAsync(ChatKitAgents.StreamAgentResponse(
            context,
            ToAsyncEnumerable(
            [
                new AgentStreamEvent("keepalive", "demo-agent"),
                new AgentStreamEvent(
                    "item.completed",
                    "demo-agent",
                    new AgentRunItem(
                        AgentItemTypes.MessageOutput,
                        "user",
                        "demo-agent",
                        null,
                        "hello",
                        null,
                        null,
                        null,
                        null),
                    null,
                    null,
                    null),
                new AgentStreamEvent(
                    "item.completed",
                    "demo-agent",
                    new AgentRunItem(
                        "custom",
                        "assistant",
                        "demo-agent",
                        null,
                        "ignored",
                        null,
                        null,
                        null,
                        null),
                    null,
                    null,
                    null),
            ])));

        Assert.Empty(events);
    }

    private static AgentContext<Dictionary<string, object?>> CreateAgentContext(RecordingStore store, ThreadMetadata thread)
        => new()
        {
            Thread = thread,
            Store = store,
            RequestContext = new Dictionary<string, object?>(),
        };

    private static ThreadMetadata CreateThread(string id)
        => new()
        {
            Id = id,
            CreatedAt = ChatKitClock.Now(),
            Title = id,
        };

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        List<T> items = [];
        await foreach (T item in source)
        {
            items.Add(item);
        }

        return items;
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (T item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private sealed class RecordingStore : ChatKitStore<Dictionary<string, object?>>
    {
        public int GenerateThreadIdCalls { get; private set; }

        public List<(string ItemType, string ThreadId)> GenerateItemIdCalls { get; } = [];

        public override string GenerateThreadId(Dictionary<string, object?> context)
        {
            _ = context;
            GenerateThreadIdCalls++;
            return "thr_generated";
        }

        public override string GenerateItemId(string itemType, ThreadMetadata thread, Dictionary<string, object?> context)
        {
            _ = context;
            GenerateItemIdCalls.Add((itemType, thread.Id));
            return $"{itemType}_for_{thread.Id}";
        }

        public override Task<ThreadMetadata> LoadThreadAsync(string threadId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task SaveThreadAsync(ThreadMetadata thread, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task<Page<ThreadItem>> LoadThreadItemsAsync(string threadId, string? after, int limit, string order, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task SaveAttachmentAsync(Attachment attachment, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task<Attachment> LoadAttachmentAsync(string attachmentId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task DeleteAttachmentAsync(string attachmentId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task<Page<ThreadMetadata>> LoadThreadsAsync(int limit, string? after, string order, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task AddThreadItemAsync(string threadId, ThreadItem item, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task SaveItemAsync(string threadId, ThreadItem item, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task<ThreadItem> LoadItemAsync(string threadId, string itemId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task DeleteThreadAsync(string threadId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public override Task DeleteThreadItemAsync(string threadId, string itemId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
