using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    /// <summary>Widget exports load from disk or stream, preserve their metadata, and hydrate widget roots from valid input state.</summary>
    /// <intent>Protect the file-backed widget template abstraction used by downstream ChatKit integrations.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Loading a widget export from disk or stream preserves the template, schema, preview, and encoded payload, and building with valid state hydrates the preview widget.</behavior>
    [Fact]
    public void WidgetDefinition_LoadFromFile_BuildsWidgetFromFixture()
    {
        string path = GetFixturePath("Email Summary.widget");

        WidgetDefinition definition = WidgetDefinition.Load(path);
        WidgetEncodedDefinition encoded = definition.DecodeEncodedWidget();
        WidgetRoot builtWidget = definition.Build(encoded.DefaultState);
        string builtJson = Encoding.UTF8.GetString(ChatKitJson.SerializeToUtf8Bytes(builtWidget));

        Assert.Equal("1.0", definition.Version);
        Assert.Equal("Email Summary", definition.Name);
        Assert.Contains("\"type\":\"Card\"", definition.Template);
        Assert.Contains("\"type\":\"object\"", definition.JsonSchema.ToJsonString());
        Assert.NotNull(definition.OutputJsonPreview);
        Assert.Equal("1e948b8d-af5b-4d49-9420-5651f880eecc", encoded.Id);
        Assert.Equal("Email Summary", encoded.Name);
        Assert.Contains("<Card", encoded.View, StringComparison.Ordinal);
        Assert.Equal("zod", encoded.SchemaMode);
        Assert.Equal("valid", encoded.SchemaValidity);
        Assert.Equal("valid", encoded.ViewValidity);
        Assert.Equal("valid", encoded.DefaultStateValidity);
        Assert.NotNull(encoded.DefaultState);
        Assert.Equal("Card", builtWidget.Type);
        Assert.Equal("sm", builtWidget.TryGetString("size"));
        Assert.Single(builtWidget.Children ?? []);
        Assert.Contains("Ada Lovelace", builtJson, StringComparison.Ordinal);
    }

    /// <summary>Widget exports also load from streams and hydrate the widget preview using the same Jinja-backed pipeline.</summary>
    /// <intent>Protect the stream-based widget definition API used by downstream integrations.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Loading a widget export from a stream preserves the template, schema, preview, and encoded payload, and building with valid state hydrates the preview widget.</behavior>
    [Fact]
    public async Task WidgetDefinition_LoadFromStream_BuildsWidgetFromFixture()
    {
        string path = GetFixturePath("EmailListId.widget");

        await using FileStream stream = File.OpenRead(path);
        WidgetDefinition definition = await WidgetDefinition.LoadAsync(stream);
        WidgetEncodedDefinition encoded = definition.DecodeEncodedWidget();

        Assert.Equal("1.0", definition.Version);
        Assert.Equal("EmailList", definition.Name);
        Assert.Contains("\"type\":\"ListView\"", definition.Template);
        Assert.Contains("\"type\":\"object\"", definition.JsonSchema.ToJsonString());
        Assert.NotNull(definition.OutputJsonPreview);
        Assert.Equal("EmailList", encoded.Name);
        Assert.Equal("zod", encoded.SchemaMode);
        Assert.Equal("valid", encoded.SchemaValidity);
        Assert.Equal("valid", encoded.ViewValidity);
        Assert.Equal("valid", encoded.DefaultStateValidity);
        Assert.NotNull(encoded.Schema);
        Assert.NotNull(encoded.DefaultState);
        WidgetRoot builtWidget = definition.Build(encoded.DefaultState);
        string builtJson = Encoding.UTF8.GetString(ChatKitJson.SerializeToUtf8Bytes(builtWidget));

        Assert.Equal("ListView", builtWidget.Type);
        Assert.Equal(3, builtWidget.Children?.Count);
        Assert.Contains("3 emails found", builtJson, StringComparison.Ordinal);
        Assert.Contains("alice@example.com", builtJson, StringComparison.Ordinal);
    }

    /// <summary>Widget exports reject state that does not satisfy the exported JSON schema.</summary>
    /// <intent>Protect schema-based input validation before widget rendering occurs.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Rendering a widget with invalid input state fails before the Jinja template is hydrated into a widget root.</behavior>
    [Fact]
    public void WidgetDefinition_Build_RejectsInvalidStateAgainstSchema()
    {
        string path = GetFixturePath("EmailListId.widget");
        WidgetDefinition definition = WidgetDefinition.Load(path);
        WidgetEncodedDefinition encoded = definition.DecodeEncodedWidget();
        JsonObject invalidState = Assert.IsType<JsonObject>(encoded.DefaultState!.DeepClone());
        JsonArray emails = Assert.IsType<JsonArray>(invalidState["emails"]);
        JsonObject firstEmail = Assert.IsType<JsonObject>(emails[0]);
        firstEmail["unexpected"] = true;

        Action act = () => definition.Build(invalidState);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("additional property", exception.Message, StringComparison.Ordinal);
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

    private static string GetFixturePath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

}
