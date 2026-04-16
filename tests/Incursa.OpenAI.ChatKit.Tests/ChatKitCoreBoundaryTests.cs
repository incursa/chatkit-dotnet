using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitCoreBoundaryTests
{
    /// <summary>Thread detail responses exclude hidden context items even when they remain persisted in storage.</summary>
    /// <intent>Protect the public ChatKit thread contract from leaking hidden context back to clients.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Loading a thread by id returns visible user-facing items and omits hidden context item variants from the serialized response payload.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task ProcessAsync_ThreadsGetById_FiltersHiddenContextItemsFromResponse()
    {
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = new()
        {
            Id = "thr_visible",
            CreatedAt = ChatKitClock.Now(),
            Title = "seeded",
        };

        await store.SaveThreadAsync(thread, new Dictionary<string, object?>());
        await store.AddThreadItemAsync(
            thread.Id,
            new UserMessageItem
            {
                Id = "msg_visible",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = [new UserMessageTextContent { Text = "hello" }],
            },
            new Dictionary<string, object?>());
        await store.AddThreadItemAsync(
            thread.Id,
            new HiddenContextItem
            {
                Id = "ctx_hidden",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = new JsonObject
                {
                    ["internal"] = "secret",
                },
            },
            new Dictionary<string, object?>());
        await store.AddThreadItemAsync(
            thread.Id,
            new SdkHiddenContextItem
            {
                Id = "ctx_sdk",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = "sdk-secret",
            },
            new Dictionary<string, object?>());

        VisibilityServer server = new(store);
        byte[] request = ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsGetByIdRequest
        {
            Params = new ThreadGetByIdParams
            {
                ThreadId = thread.Id,
            },
        });

        ChatKitProcessResult result = await server.ProcessAsync(request, new Dictionary<string, object?>());

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        string json = Encoding.UTF8.GetString(nonStreaming.Json);
        Assert.Contains("\"id\":\"msg_visible\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ctx_hidden", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ctx_sdk", json, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden_context_item", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sdk_hidden_context", json, StringComparison.Ordinal);
    }

    /// <summary>Thread item list responses exclude hidden context items even when they remain persisted in storage.</summary>
    /// <intent>Protect the public ChatKit item-list contract from leaking hidden context back to clients.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Listing thread items returns visible user-facing items and omits hidden context item variants from the serialized response payload.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public async Task ProcessAsync_ItemsList_FiltersHiddenContextItemsFromResponse()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = new()
        {
            Id = "thr_items_visible",
            CreatedAt = ChatKitClock.Now(),
            Title = "seeded",
        };

        await store.SaveThreadAsync(thread, context);
        await store.AddThreadItemAsync(
            thread.Id,
            new UserMessageItem
            {
                Id = "msg_visible",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = [new UserMessageTextContent { Text = "hello" }],
            },
            context);
        await store.AddThreadItemAsync(
            thread.Id,
            new HiddenContextItem
            {
                Id = "ctx_hidden",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = new JsonObject
                {
                    ["internal"] = "secret",
                },
            },
            context);
        await store.AddThreadItemAsync(
            thread.Id,
            new SdkHiddenContextItem
            {
                Id = "ctx_sdk",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = "sdk-secret",
            },
            context);

        VisibilityServer server = new(store);
        byte[] request = ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ItemsListRequest
        {
            Params = new ItemsListParams
            {
                ThreadId = thread.Id,
                Order = "asc",
            },
        });

        ChatKitProcessResult result = await server.ProcessAsync(request, context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        string json = Encoding.UTF8.GetString(nonStreaming.Json);
        Assert.Contains("\"id\":\"msg_visible\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ctx_hidden", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ctx_sdk", json, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden_context_item", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sdk_hidden_context", json, StringComparison.Ordinal);
    }

    /// <summary>Request deserialization rejects null envelopes instead of producing an untyped placeholder.</summary>
    /// <intent>Protect request routing from silently accepting invalid root request payloads.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Deserializing a null ChatKit request payload throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public void DeserializeRequest_Throws_WhenEnvelopeIsNull()
    {
        byte[] json = Encoding.UTF8.GetBytes("null");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ChatKitJson.DeserializeRequest(json));

        Assert.Contains("Unable to deserialize ChatKit request payload.", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Unknown request discriminators are rejected instead of being treated as a valid ChatKit request kind.</summary>
    /// <intent>Protect request routing from silently accepting request types outside the approved protocol inventory.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Processing a request payload with an unsupported type discriminator throws a JSON exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task ProcessAsync_Throws_WhenRequestTypeDiscriminatorIsUnsupported()
    {
        VisibilityServer server = new(new InMemoryChatKitStore<Dictionary<string, object?>>());
        byte[] json = Encoding.UTF8.GetBytes("""
            {
              "type": "threads.unsupported",
              "params": {}
            }
            """);

        await Assert.ThrowsAsync<JsonException>(() => server.ProcessAsync(json, new Dictionary<string, object?>()));
    }

    /// <summary>Streaming widget diffs fall back to a root replacement when text no longer follows append-only semantics.</summary>
    /// <intent>Protect incremental widget updates from emitting an invalid delta when the new text is incompatible with the prior streamed value.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Diffing a streaming text node whose updated value no longer begins with the prior value emits a widget root replacement instead of a text delta.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void WidgetDiff_FallsBackToRootReplace_WhenStreamingTextIsNotPrefixAppend()
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
                        ["value"] = "Hello",
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
                        ["value"] = "World",
                        ["streaming"] = false,
                    },
                },
            ],
        };

        IReadOnlyList<ThreadItemUpdate> updates = WidgetStreaming.Diff(before, after);

        WidgetRootUpdated update = Assert.IsType<WidgetRootUpdated>(Assert.Single(updates));
        Assert.Same(after, update.Widget);
    }

    /// <summary>Widget definitions reject unsupported export versions before rendering begins.</summary>
    /// <intent>Protect file-backed widget loading from silently accepting incompatible export formats.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Parsing a widget definition with an unsupported version throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public void WidgetDefinition_Parse_Throws_WhenVersionIsUnsupported()
    {
        const string widgetJson = """
            {
              "version": "2.0",
              "name": "Unsupported Widget",
              "template": "{\"type\":\"Card\"}",
              "jsonSchema": {
                "type": "object"
              },
              "encodedWidget": "eyJpZCI6IndpZGdldC0xIiwibmFtZSI6IlVuc3VwcG9ydGVkIFdpZGdldCIsInZpZXciOiI8Q2FyZCBzaXplPVwic21cIiAvPiJ9"
            }
            """;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WidgetDefinition.Parse(widgetJson));

        Assert.Contains("Unsupported widget definition version", exception.Message, StringComparison.Ordinal);
    }

    private sealed class VisibilityServer : ChatKitServer<Dictionary<string, object?>>
    {
        public VisibilityServer(InMemoryChatKitStore<Dictionary<string, object?>> store)
            : base(store)
        {
        }

        public override async IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = thread;
            _ = inputUserMessage;
            _ = context;
            _ = cancellationToken;
            await Task.CompletedTask;
            yield break;
        }
    }
}
