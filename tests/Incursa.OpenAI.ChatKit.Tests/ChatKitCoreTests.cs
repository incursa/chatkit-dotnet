using System.Text;
using Incursa.OpenAI.Agents;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Smoke")]
public sealed class ChatKitCoreTests
{
    /// <summary>Threads create requests serialize with the upstream ChatKit discriminator and payload field names.</summary>
    /// <intent>Protect exact wire compatibility for the core request envelope.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Serializing a threads.create request emits the expected type discriminator and content payload shape.</behavior>
    [Fact]
    public void Serialize_ThreadsCreateRequest_UsesExactDiscriminator()
    {
        ThreadsCreateRequest request = new()
        {
            Params = new ThreadCreateParams
            {
                Input = new UserMessageInput
                {
                    Content =
                    [
                        new UserMessageTextContent { Text = "hello" },
                    ],
                },
            },
        };

        string json = Encoding.UTF8.GetString(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request));

        Assert.Contains("\"type\":\"threads.create\"", json);
        Assert.Contains("\"input_text\"", json);
    }

    /// <summary>Streaming text widgets emit deltas instead of forcing a root replacement when text is appended.</summary>
    /// <intent>Protect incremental widget updates for ChatKit streaming UI flows.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Diffing compatible before and after widgets returns only the appended text delta and completion state.</behavior>
    [Fact]
    public void WidgetDiff_StreamingText_ReturnsDeltaOnly()
    {
        WidgetRoot before = new()
        {
            Type = "Box",
            Children =
            [
                new WidgetComponent
                {
                    Type = "Text",
                    Id = "summary",
                    Properties = new Dictionary<string, object?>
                    {
                        ["value"] = "Hel",
                        ["streaming"] = true,
                    },
                },
            ],
        };

        WidgetRoot after = new()
        {
            Type = "Box",
            Children =
            [
                new WidgetComponent
                {
                    Type = "Text",
                    Id = "summary",
                    Properties = new Dictionary<string, object?>
                    {
                        ["value"] = "Hello",
                        ["streaming"] = false,
                    },
                },
            ],
        };

        IReadOnlyList<ThreadItemUpdate> deltas = WidgetStreaming.Diff(before, after);

        WidgetStreamingTextValueDelta delta = Assert.IsType<WidgetStreamingTextValueDelta>(Assert.Single(deltas));
        Assert.Equal("lo", delta.Delta);
        Assert.True(delta.Done);
    }

    /// <summary>Threads create requests stream the created thread and assistant response events through the ChatKit pipeline.</summary>
    /// <intent>Protect the core thread routing path used by hosted ChatKit servers.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.create payload produces stream events for thread creation and the completed assistant item.</behavior>
    [Fact]
    public async Task ProcessAsync_ThreadsCreate_StreamsUserAndAssistantTurn()
    {
        DemoServer server = new();
        ThreadsCreateRequest request = new()
        {
            Params = new ThreadCreateParams
            {
                Input = new UserMessageInput
                {
                    Content =
                    [
                        new UserMessageTextContent { Text = "hello" },
                    ],
                },
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), new Dictionary<string, object?>());

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        List<string> chunks = [];
        await foreach (byte[] chunk in streaming)
        {
            chunks.Add(Encoding.UTF8.GetString(chunk));
        }

        Assert.Contains(chunks, chunk => chunk.Contains("\"type\":\"thread.created\"", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Contains("\"type\":\"thread.item.done\"", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Contains("pong", StringComparison.Ordinal));
    }

    /// <summary>ChatKit conversation items map into the agent input shape expected by the agents dependency.</summary>
    /// <intent>Protect interop between ChatKit message history and the shared agents runtime.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Simple user and assistant items are translated into the corresponding agent conversation item types and text values.</behavior>
    [Fact]
    public void SimpleToAgentInput_MapsUserAndAssistantMessages()
    {
        ThreadItem[] items =
        [
            new UserMessageItem
            {
                Id = "msg_1",
                ThreadId = "thr_1",
                CreatedAt = ChatKitClock.Now(),
                Content = [new UserMessageTextContent { Text = "hello" }],
            },
            new AssistantMessageItem
            {
                Id = "msg_2",
                ThreadId = "thr_1",
                CreatedAt = ChatKitClock.Now(),
                Content = [new AssistantMessageContent { Text = "hi" }],
            },
        ];

        IReadOnlyList<AgentConversationItem> mapped = ChatKitAgents.SimpleToAgentInput(items);

        Assert.Collection(
            mapped,
            first =>
            {
                Assert.Equal(AgentItemTypes.UserInput, first.ItemType);
                Assert.Equal("hello", first.Text);
            },
            second =>
            {
                Assert.Equal(AgentItemTypes.MessageOutput, second.ItemType);
                Assert.Equal("hi", second.Text);
            });
    }

    private sealed class DemoServer : ChatKitServer<Dictionary<string, object?>>
    {
        public DemoServer()
            : base(new InMemoryChatKitStore<Dictionary<string, object?>>())
        {
        }

        public override async IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ThreadItemDoneEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = Store.GenerateItemId(StoreItemTypes.Message, thread, context),
                    ThreadId = thread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "pong" }],
                },
            };

            await Task.CompletedTask;
        }
    }
}
