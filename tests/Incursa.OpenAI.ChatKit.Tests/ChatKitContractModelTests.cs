using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Incursa.OpenAI.Agents;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitContractModelTests
{
    /// <summary>Client-facing action configuration serializes with the expected snake-case field names and nested action payload.</summary>
    /// <intent>Protect the public action contract consumed by ChatKit clients and widgets.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Serializing an action configuration emits the expected field names, destructive flag, and nested JSON payload.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void Serialize_ActionConfig_UsesExpectedWireShape()
    {
        ActionConfig config = new()
        {
            Action = new ChatKitAction
            {
                Type = "open_url",
                Payload = JsonNode.Parse("""{"url":"https://example.com"}"""),
            },
            Label = "Open",
            ConfirmTitle = "Continue",
            ConfirmBody = "Leave the current view?",
            Destructive = true,
        };

        string json = Serialize(config);

        Assert.Contains("\"action\":{\"type\":\"open_url\",\"payload\":{\"url\":\"https://example.com\"}}", json, StringComparison.Ordinal);
        Assert.Contains("\"label\":\"Open\"", json, StringComparison.Ordinal);
        Assert.Contains("\"confirm_title\":\"Continue\"", json, StringComparison.Ordinal);
        Assert.Contains("\"confirm_body\":\"Leave the current view?\"", json, StringComparison.Ordinal);
        Assert.Contains("\"destructive\":true", json, StringComparison.Ordinal);
    }

    /// <summary>Client tool call transcript items translate into agent tool-call items with preserved identifiers, arguments, and status.</summary>
    /// <intent>Protect interoperability between persisted ChatKit tool calls and the shared agents runtime.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Mapping a client tool call item to agent input preserves its tool type, name, call id, arguments, and pending status.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void SimpleToAgentInput_MapsClientToolCallItem()
    {
        ClientToolCallItem toolCall = new()
        {
            Id = "tool_1",
            ThreadId = "thr_1",
            CreatedAt = ChatKitClock.Now(),
            CallId = "call_1",
            Name = "lookup_contact",
            Arguments = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            {
                ["query"] = JsonValue.Create("Ada"),
            },
            Status = "pending",
        };

        object mapped = Assert.Single(ChatKitAgents.SimpleToAgentInput([toolCall]));

        Assert.Equal(AgentItemTypes.ToolCall, ReadProperty(mapped, "ItemType"));
        Assert.Equal("lookup_contact", ReadProperty(mapped, "Name"));
        Assert.Equal("call_1", ReadProperty(mapped, "ToolCallId"));
        Assert.Equal("pending", ReadProperty(mapped, "Status"));

        JsonNode? data = Assert.IsAssignableFrom<JsonNode>(ReadProperty(mapped, "Data"));
        Assert.Equal("Ada", data?["query"]?.GetValue<string>());
    }

    /// <summary>Representative request envelopes serialize with the exact upstream discriminators, default sort order, and snake-case metadata keys.</summary>
    /// <intent>Protect the public ChatKit request contract across common list, custom action, and transcription paths.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Serializing request envelopes emits the expected type discriminators, default descending order, and snake-case metadata fields.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void Serialize_RequestEnvelopes_UsesExpectedDiscriminatorsAndDefaults()
    {
        ChatKitRequest[] requests =
        [
            new ThreadsListRequest
            {
                Metadata = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
                {
                    ["traceId"] = JsonValue.Create("abc-123"),
                },
            },
            new ThreadsSyncCustomActionRequest
            {
                Params = new ThreadCustomActionParams
                {
                    ThreadId = "thr_1",
                    ItemId = "widget_1",
                    Action = new ChatKitAction
                    {
                        Type = "refresh",
                    },
                },
            },
            new InputTranscribeRequest
            {
                Params = new InputTranscribeParams
                {
                    AudioBase64 = "AQID",
                    MimeType = "audio/webm",
                },
            },
        ];

        string json = string.Join("\n", requests.Select(static request => Serialize(request)));

        Assert.Contains("\"type\":\"threads.list\"", json, StringComparison.Ordinal);
        Assert.Contains("\"order\":\"desc\"", json, StringComparison.Ordinal);
        Assert.Contains("\"metadata\":{\"trace_id\":\"abc-123\"}", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"threads.sync_custom_action\"", json, StringComparison.Ordinal);
        Assert.Contains("\"item_id\":\"widget_1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"action\":{\"type\":\"refresh\"}", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"input.transcribe\"", json, StringComparison.Ordinal);
        Assert.Contains("\"audio_base64\":\"AQID\"", json, StringComparison.Ordinal);
        Assert.Contains("\"mime_type\":\"audio/webm\"", json, StringComparison.Ordinal);
    }

    /// <summary>Representative stream events serialize with the expected event/update discriminators and nested payload shapes.</summary>
    /// <intent>Protect the public ChatKit event stream contract across thread lifecycle, workflow, widget, and client-effect paths.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Serializing stream events emits the expected outer event discriminators and nested polymorphic payload types.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void Serialize_ThreadStreamEvents_UsesExpectedDiscriminatorsAndPayloadShapes()
    {
        DateTime createdAt = ChatKitClock.Now();
        Thread thread = new()
        {
            Id = "thr_1",
            CreatedAt = createdAt,
            Title = "Research",
            Status = new LockedStatus { Reason = "moderation" },
            AllowedImageDomains = ["example.com"],
            Metadata = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            {
                ["audience"] = JsonValue.Create("internal"),
            },
            Items = new Page<ThreadItem>(),
        };

        ThreadStreamEvent[] events =
        [
            new ThreadCreatedEvent { Thread = thread },
            new ThreadUpdatedEvent { Thread = thread with { Title = "Research (updated)" } },
            new ThreadItemAddedEvent
            {
                Item = new UserMessageItem
                {
                    Id = "msg_1",
                    ThreadId = thread.Id,
                    CreatedAt = createdAt,
                    Content =
                    [
                        new UserMessageTagContent
                        {
                            Id = "tag_1",
                            Text = "Ada Lovelace",
                            Group = "people",
                            Interactive = true,
                            Data = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
                            {
                                ["email"] = JsonValue.Create("ada@example.com"),
                            },
                        },
                    ],
                    Attachments =
                    [
                        new ImageAttachment
                        {
                            Id = "att_1",
                            Name = "diagram.png",
                            MimeType = "image/png",
                            PreviewUrl = "https://example.com/preview.png",
                            UploadDescriptor = new AttachmentUploadDescriptor
                            {
                                Url = "https://example.com/upload",
                                Method = "PUT",
                                Headers = new Dictionary<string, string>(StringComparer.Ordinal)
                                {
                                    ["x-test-token"] = "demo",
                                },
                            },
                        },
                    ],
                },
            },
            new ThreadItemUpdatedEvent
            {
                ItemId = "msg_2",
                Update = new AssistantMessageContentPartAnnotationAdded
                {
                    ContentIndex = 0,
                    AnnotationIndex = 0,
                    Annotation = new Annotation
                    {
                        Source = new URLSource
                        {
                            Title = "Reference",
                            Url = "https://example.com/reference",
                            Attribution = "Example",
                        },
                        Index = 4,
                    },
                },
            },
            new ThreadItemUpdatedEvent
            {
                ItemId = "widget_1",
                Update = new WidgetStreamingTextValueDelta
                {
                    ComponentId = "summary",
                    Delta = "lo",
                    Done = true,
                },
            },
            new ThreadItemUpdatedEvent
            {
                ItemId = "workflow_1",
                Update = new WorkflowTaskAdded
                {
                    TaskIndex = 0,
                    Task = new SearchTask
                    {
                        TitleQuery = "Ada Lovelace",
                        Queries = ["ada"],
                        Sources =
                        [
                            new URLSource
                            {
                                Title = "Search Result",
                                Url = "https://example.com/result",
                            },
                        ],
                    },
                },
            },
            new ThreadItemUpdatedEvent
            {
                ItemId = "image_1",
                Update = new GeneratedImageUpdated
                {
                    Image = new GeneratedImage
                    {
                        Id = "img_1",
                        Url = "https://example.com/generated.png",
                    },
                    Progress = 0.5,
                },
            },
            new ThreadItemDoneEvent
            {
                Item = new WorkflowItem
                {
                    Id = "workflow_1",
                    ThreadId = thread.Id,
                    CreatedAt = createdAt,
                    Workflow = new Workflow
                    {
                        Type = "reasoning",
                        Tasks =
                        [
                            new ThoughtTask
                            {
                                Content = "Working through the result set.",
                            },
                        ],
                        Summary = new DurationSummary
                        {
                            Duration = 12,
                        },
                        Expanded = true,
                    },
                },
            },
            new ThreadItemRemovedEvent { ItemId = "msg_3" },
            new ThreadItemReplacedEvent
            {
                Item = new WidgetItem
                {
                    Id = "widget_1",
                    ThreadId = thread.Id,
                    CreatedAt = createdAt,
                    Widget = new WidgetRoot
                    {
                        Type = "Card",
                        Id = "root",
                        Children =
                        [
                            new WidgetComponent
                            {
                                Type = "Text",
                                Id = "summary",
                                Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                                {
                                    ["value"] = "Hello",
                                },
                            },
                        ],
                    },
                    CopyText = "Hello",
                },
            },
            new StreamOptionsEvent
            {
                StreamOptions = new StreamOptions
                {
                    AllowCancel = true,
                },
            },
            new ProgressUpdateEvent
            {
                Icon = "spinner",
                Text = "Working",
            },
            new ClientEffectEvent
            {
                Name = "scroll_to_bottom",
                Data = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
                {
                    ["behavior"] = JsonValue.Create("smooth"),
                },
            },
            new ErrorEvent
            {
                Message = "boom",
                AllowRetry = true,
            },
            new NoticeEvent
            {
                Level = "info",
                Message = "Heads up",
                Title = "FYI",
            },
        ];

        string json = string.Join("\n", events.Select(static @event => Serialize<ThreadStreamEvent>(@event)));

        Assert.Contains("\"type\":\"thread.created\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"thread.updated\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"thread.item.added\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"input_tag\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"image\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"thread.item.updated\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"assistant_message.content_part.annotation_added\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"widget.streaming_text.value_delta\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"workflow.task.added\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"generated_image.updated\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"duration\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"thought\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"stream_options\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"client_effect\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"error\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"notice\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"locked\"", json, StringComparison.Ordinal);
    }

    /// <summary>Widget transcript items preserve newer upstream root and component shapes without requiring typed .NET wrappers for each addition.</summary>
    /// <intent>Protect the generic widget wire contract when upstream ChatKit adds new widget roots or component types that the current .NET model can already represent.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Serializing and deserializing a widget item with a Basic root, table hierarchy, and Card border properties preserves the current upstream type names and nested payload members.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void Serialize_WidgetItem_PreservesBasicRootTableAndBorderShapes()
    {
        WidgetItem item = new()
        {
            Id = "widget_1",
            ThreadId = "thr_1",
            CreatedAt = ChatKitClock.Now(),
            Widget = new WidgetRoot
            {
                Type = "Basic",
                Id = "root",
                Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["theme"] = "dark",
                    ["direction"] = "col",
                    ["gap"] = 12,
                },
                Children =
                [
                    new WidgetComponent
                    {
                        Type = "Card",
                        Id = "card_1",
                        Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            ["border"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                            {
                                ["size"] = 2,
                                ["style"] = "dashed",
                            },
                        },
                        Children =
                        [
                            new WidgetComponent
                            {
                                Type = "Table",
                                Id = "table_1",
                                Children =
                                [
                                    new WidgetComponent
                                    {
                                        Type = "Table.Row",
                                        Id = "row_1",
                                        Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                                        {
                                            ["header"] = true,
                                        },
                                        Children =
                                        [
                                            new WidgetComponent
                                            {
                                                Type = "Table.Cell",
                                                Id = "cell_1",
                                                Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                                                {
                                                    ["colSpan"] = 2,
                                                    ["align"] = "center",
                                                    ["colSize"] = "lg",
                                                },
                                                Children =
                                                [
                                                    new WidgetComponent
                                                    {
                                                        Type = "Text",
                                                        Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
                                                        {
                                                            ["value"] = "Summary",
                                                        },
                                                    },
                                                ],
                                            },
                                        ],
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        };

        string json = Serialize(item);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement widget = document.RootElement.GetProperty("widget");
        Assert.Equal("Basic", widget.GetProperty("type").GetString());
        Assert.Equal("dark", widget.GetProperty("theme").GetString());

        JsonElement card = widget.GetProperty("children")[0];
        Assert.Equal("Card", card.GetProperty("type").GetString());
        Assert.Equal(2, card.GetProperty("border").GetProperty("size").GetInt32());
        Assert.Equal("dashed", card.GetProperty("border").GetProperty("style").GetString());

        JsonElement table = card.GetProperty("children")[0];
        Assert.Equal("Table", table.GetProperty("type").GetString());

        JsonElement row = table.GetProperty("children")[0];
        Assert.Equal("Table.Row", row.GetProperty("type").GetString());
        Assert.True(row.GetProperty("header").GetBoolean());

        JsonElement cell = row.GetProperty("children")[0];
        Assert.Equal("Table.Cell", cell.GetProperty("type").GetString());
        Assert.Equal(2, cell.GetProperty("colSpan").GetInt32());
        Assert.Equal("center", cell.GetProperty("align").GetString());
        Assert.Equal("lg", cell.GetProperty("colSize").GetString());

        WidgetItem roundTripped = ChatKitJson.Deserialize<WidgetItem>(Encoding.UTF8.GetBytes(json))
            ?? throw new InvalidOperationException("Round-tripped widget item did not deserialize.");

        WidgetRoot roundTrippedWidget = roundTripped.Widget;
        WidgetComponent roundTrippedCard = Assert.Single(roundTrippedWidget.Children ?? throw new InvalidOperationException("Round-tripped widget root is missing children."));
        WidgetComponent roundTrippedTable = Assert.Single(roundTrippedCard.Children ?? throw new InvalidOperationException("Round-tripped card is missing children."));
        WidgetComponent roundTrippedRow = Assert.Single(roundTrippedTable.Children ?? throw new InvalidOperationException("Round-tripped table is missing children."));

        Assert.Equal("Basic", roundTrippedWidget.Type);
        Assert.Equal("Card", roundTrippedCard.Type);
        Assert.Equal("Table", roundTrippedTable.Type);
        Assert.Equal("Table.Row", roundTrippedRow.Type);
    }

    /// <summary>Unsupported event discriminators are rejected instead of silently producing an untyped placeholder.</summary>
    /// <intent>Protect stream consumers from accepting unknown event types outside the approved contract inventory.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Deserializing a thread stream event with an unsupported type discriminator throws a JSON exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public void DeserializeThreadStreamEvent_Throws_WhenTypeDiscriminatorIsUnsupported()
    {
        byte[] json = Encoding.UTF8.GetBytes("""
            {
              "type": "thread.unsupported"
            }
            """);

        Assert.Throws<JsonException>(() => ChatKitJson.Deserialize<ThreadStreamEvent>(json));
    }

    /// <summary>Audio payloads expose a stable media type without MIME parameters.</summary>
    /// <intent>Protect the transcription contract from treating codec or charset parameters as part of the media type identifier.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>The audio input media type accessor strips any MIME parameters and preserves only the base media type.</behavior>
    [Trait("Category", "Positive")]
    [Theory]
    [InlineData("audio/webm; codecs=opus", "audio/webm")]
    [InlineData("audio/mpeg", "audio/mpeg")]
    public void AudioInput_MediaType_StripsParameters(string mimeType, string expectedMediaType)
    {
        AudioInput input = new()
        {
            Data = [0x01, 0x02],
            MimeType = mimeType,
        };

        Assert.Equal(expectedMediaType, input.MediaType);
    }

    /// <summary>Widget rendering supplies schema-shaped empty values for optional properties that are omitted from state.</summary>
    /// <intent>Protect the widget renderer from failing when optional arrays and objects are missing from otherwise valid state.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Rendering a widget with omitted optional schema members supplies empty defaults that the template can safely read.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void WidgetDefinition_Build_SuppliesSchemaShapedDefaultsForMissingOptionalState()
    {
        WidgetDefinition definition = CreateWidgetDefinition(
            """
            {
              "type": "Card",
              "title": "{{ title }}",
              "count": "{{ tags | length }}",
              "subtitle": "{{ details.subtitle }}"
            }
            """,
            """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "tags": {
                  "type": "array",
                  "items": { "type": "string" }
                },
                "details": {
                  "type": "object",
                  "properties": {
                    "subtitle": { "type": "string" }
                  },
                  "additionalProperties": false
                }
              },
              "required": ["title"],
              "additionalProperties": false
            }
            """);

        WidgetRoot widget = definition.Build(new { title = "Inbox" });

        Assert.Equal("Card", widget.Type);
        Assert.Equal("Inbox", widget.TryGetString("title"));
        Assert.Equal("0", widget.TryGetString("count"));
        Assert.Equal(string.Empty, widget.TryGetString("subtitle"));
    }

    /// <summary>Widget rendering rejects enum violations before template hydration occurs.</summary>
    /// <intent>Protect schema-based widget validation from allowing invalid enum selections into the rendered output.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Rendering a widget with an enum value outside the schema inventory throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public void WidgetDefinition_Build_RejectsEnumMismatch()
    {
        WidgetDefinition definition = CreateWidgetDefinition(
            """
            {
              "type": "Card",
              "mode": "{{ mode }}"
            }
            """,
            """
            {
              "type": "object",
              "properties": {
                "mode": {
                  "type": "string",
                  "enum": ["compact", "full"]
                }
              },
              "required": ["mode"],
              "additionalProperties": false
            }
            """);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => definition.Build(new { mode = "wide" }));

        Assert.Contains("allowed enum value", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Widget rendering rejects constant mismatches before template hydration occurs.</summary>
    /// <intent>Protect schema-based widget validation from allowing invalid constant values into rendered output.</intent>
    /// <scenario>LIB-CHATKIT-CORE-004</scenario>
    /// <behavior>Rendering a widget with a property that does not match the required constant value throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public void WidgetDefinition_Build_RejectsConstMismatch()
    {
        WidgetDefinition definition = CreateWidgetDefinition(
            """
            {
              "type": "Card",
              "kind": "{{ kind }}"
            }
            """,
            """
            {
              "type": "object",
              "properties": {
                "kind": {
                  "type": "string",
                  "const": "email"
                }
              },
              "required": ["kind"],
              "additionalProperties": false
            }
            """);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => definition.Build(new { kind = "sms" }));

        Assert.Contains("required constant value", exception.Message, StringComparison.Ordinal);
    }

    private static WidgetDefinition CreateWidgetDefinition(string template, string schemaJson)
        => new()
        {
            Version = "1.0",
            Name = "Inline Widget",
            Template = template,
            JsonSchema = JsonNode.Parse(schemaJson) ?? throw new InvalidOperationException("Test schema JSON did not parse."),
            EncodedWidget = new WidgetEncodedDefinition
            {
                Id = "widget_inline",
                Name = "Inline Widget",
                View = "<Card />",
            }.ToEncodedWidget(),
        };

    private static object? ReadProperty(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);

    private static string Serialize<T>(T value)
        => Encoding.UTF8.GetString(ChatKitJson.SerializeToUtf8Bytes(value));
}
