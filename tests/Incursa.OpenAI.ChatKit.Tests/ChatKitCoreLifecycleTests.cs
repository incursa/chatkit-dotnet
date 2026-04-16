using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitCoreLifecycleTests
{
    /// <summary>Attachment creation persists the created descriptor when an attachment store is configured.</summary>
    /// <intent>Protect the external attachment creation lane owned by the core runtime and attachment store boundary.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an attachments.create request calls the configured attachment store, persists the returned attachment, and returns the created payload as JSON.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0011")]
    [Fact]
    public async Task ProcessAsync_AttachmentsCreate_PersistsCreatedAttachment()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        RecordingAttachmentStore attachmentStore = new();
        DelegateServer server = new(store, attachmentStore, EmptyEventsAsync);
        AttachmentsCreateRequest request = new()
        {
            Params = new AttachmentCreateParams
            {
                Name = "notes.txt",
                Size = 42,
                MimeType = "text/plain",
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        string json = Encoding.UTF8.GetString(nonStreaming.Json);
        Attachment created = Assert.Single(attachmentStore.CreatedAttachments);
        Attachment persisted = await store.LoadAttachmentAsync(created.Id, context);

        Assert.Equal("notes.txt", created.Name);
        Assert.Equal("text/plain", created.MimeType);
        Assert.Equal(created.Id, persisted.Id);
        Assert.Contains(created.Id, json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"file\"", json, StringComparison.Ordinal);
    }

    /// <summary>Attachment deletion removes persisted descriptors and forwards the delete to the attachment store.</summary>
    /// <intent>Protect the two-phase attachment delete workflow from leaving stale records behind.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an attachments.delete request calls the configured attachment store delete hook and removes the attachment from the backing ChatKit store.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0011")]
    [Fact]
    public async Task ProcessAsync_AttachmentsDelete_RemovesPersistedAttachment()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        RecordingAttachmentStore attachmentStore = new();
        DelegateServer server = new(store, attachmentStore, EmptyEventsAsync);
        FileAttachment attachment = new()
        {
            Id = "atc_existing",
            Name = "report.pdf",
            MimeType = "application/pdf",
        };

        await store.SaveAttachmentAsync(attachment, context);

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new AttachmentsDeleteRequest
            {
                Params = new AttachmentDeleteParams
                {
                    AttachmentId = attachment.Id,
                },
            }),
            context);

        _ = Assert.IsType<NonStreamingResult>(result);
        Assert.Equal([attachment.Id], attachmentStore.DeletedAttachmentIds);
        await Assert.ThrowsAsync<NotFoundException>(() => store.LoadAttachmentAsync(attachment.Id, context));
    }

    /// <summary>Attachment operations reject requests when no external attachment store has been configured.</summary>
    /// <intent>Protect attachment endpoints from appearing enabled when the repo owner has not supplied an attachment implementation.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an attachments.create request without an attachment store throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0011")]
    [Fact]
    public async Task ProcessAsync_AttachmentsCreate_Throws_WhenAttachmentStoreIsMissing()
    {
        DelegateServer server = new(new InMemoryChatKitStore<Dictionary<string, object?>>(), attachmentStore: null, EmptyEventsAsync);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new AttachmentsCreateRequest
            {
                Params = new AttachmentCreateParams
                {
                    Name = "notes.txt",
                    Size = 42,
                    MimeType = "text/plain",
                },
            }),
            new Dictionary<string, object?>()));

        Assert.Contains("AttachmentStore is not configured", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Attachment deletion rejects requests when no external attachment store has been configured.</summary>
    /// <intent>Protect attachment delete endpoints from appearing enabled when the repo owner has not supplied an attachment implementation.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an attachments.delete request without an attachment store throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0011")]
    [Fact]
    public async Task ProcessAsync_AttachmentsDelete_Throws_WhenAttachmentStoreIsMissing()
    {
        DelegateServer server = new(new InMemoryChatKitStore<Dictionary<string, object?>>(), attachmentStore: null, EmptyEventsAsync);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new AttachmentsDeleteRequest
            {
                Params = new AttachmentDeleteParams
                {
                    AttachmentId = "atc_missing",
                },
            }),
            new Dictionary<string, object?>()));

        Assert.Contains("AttachmentStore is not configured", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Asynchronous custom actions load the widget sender and stream the action result.</summary>
    /// <intent>Protect the custom widget-action lane from losing the sender context before the server action hook runs.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.custom_action request with a widget item sender passes that widget to the action hook and streams the resulting assistant item.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsCustomAction_PassesWidgetSenderToActionHook()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_custom_action");
        WidgetItem sender = new()
        {
            Id = "wgt_sender",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Widget = new WidgetRoot { Type = "Card" },
        };
        await store.AddThreadItemAsync(thread.Id, sender, context);

        WidgetItem? observedSender = null;
        ChatKitAction? observedAction = null;
        CustomActionServer server = new(
            store,
            onAction: (respondingThread, action, widgetSender, respondingContext, cancellationToken) =>
            {
                _ = respondingContext;
                _ = cancellationToken;
                observedSender = widgetSender;
                observedAction = action;
                return StreamCustomActionResultAsync(respondingThread);
            });

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsCustomActionRequest
            {
                Params = new ThreadCustomActionParams
                {
                    ThreadId = thread.Id,
                    ItemId = sender.Id,
                    Action = new ChatKitAction
                    {
                        Type = "refresh_widget",
                    },
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        List<string> chunks = await DrainStreamingAsync(streaming);

        Assert.Equal(sender.Id, observedSender?.Id);
        Assert.Equal("refresh_widget", observedAction?.Type);
        Assert.Contains(chunks, chunk => chunk.Contains("action-finished", StringComparison.Ordinal));

        static async IAsyncEnumerable<ThreadStreamEvent> StreamCustomActionResultAsync(ThreadMetadata respondingThread)
        {
            yield return new ThreadItemDoneEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_custom_action",
                    ThreadId = respondingThread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "action-finished" }],
                },
            };

            await Task.CompletedTask;
        }
    }

    /// <summary>Asynchronous custom actions reject sender items that are not widgets.</summary>
    /// <intent>Protect the widget-action pipeline from passing a non-widget thread item into the action hook.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.custom_action request with a non-widget sender item throws an invalid operation exception during stream evaluation.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsCustomAction_Throws_WhenSenderItemIsNotWidget()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_custom_action_invalid");
        UserMessageItem nonWidgetSender = new()
        {
            Id = "msg_sender",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Content = [new UserMessageTextContent { Text = "not-a-widget" }],
        };
        await store.AddThreadItemAsync(thread.Id, nonWidgetSender, context);

        DelegateServer server = new(store, attachmentStore: null, EmptyEventsAsync);
        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsCustomActionRequest
            {
                Params = new ThreadCustomActionParams
                {
                    ThreadId = thread.Id,
                    ItemId = nonWidgetSender.Id,
                    Action = new ChatKitAction
                    {
                        Type = "refresh_widget",
                    },
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DrainStreamingAsync(streaming));

        Assert.Contains("threads.custom_action requires a widget sender item", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Synchronous custom actions load the widget sender and return a JSON response from the sync action hook.</summary>
    /// <intent>Protect the synchronous custom action lane from losing sender context or drifting away from the non-streaming response contract.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.sync_custom_action request with a widget item sender passes that widget to the sync action hook and returns the serialized updated item.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0003")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsSyncCustomAction_ReturnsSerializedUpdatedItem()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_sync_action");
        WidgetItem sender = new()
        {
            Id = "wgt_sync_sender",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Widget = new WidgetRoot { Type = "Card" },
        };
        await store.AddThreadItemAsync(thread.Id, sender, context);

        WidgetItem? observedSender = null;
        ChatKitAction? observedAction = null;
        CustomActionServer server = new(
            store,
            onSyncAction: (respondingThread, action, widgetSender, respondingContext, cancellationToken) =>
            {
                _ = respondingThread;
                _ = respondingContext;
                _ = cancellationToken;
                observedSender = widgetSender;
                observedAction = action;
                return Task.FromResult(new SyncCustomActionResponse
                {
                    UpdatedItem = new WidgetItem
                    {
                        Id = "wgt_sync_updated",
                        ThreadId = sender.ThreadId,
                        CreatedAt = ChatKitClock.Now(),
                        Widget = new WidgetRoot { Type = "Card" },
                    },
                });
            });

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsSyncCustomActionRequest
            {
                Params = new ThreadCustomActionParams
                {
                    ThreadId = thread.Id,
                    ItemId = sender.Id,
                    Action = new ChatKitAction
                    {
                        Type = "refresh_widget",
                    },
                },
            }),
            context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        string json = Encoding.UTF8.GetString(nonStreaming.Json);

        Assert.Equal(sender.Id, observedSender?.Id);
        Assert.Equal("refresh_widget", observedAction?.Type);
        Assert.Contains("\"updated_item\"", json, StringComparison.Ordinal);
        Assert.Contains("wgt_sync_updated", json, StringComparison.Ordinal);
    }

    /// <summary>Synchronous custom actions reject sender items that are not widgets.</summary>
    /// <intent>Protect the synchronous widget-action pipeline from passing a non-widget thread item into the sync action hook.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.sync_custom_action request with a non-widget sender item throws an invalid operation exception.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0003")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsSyncCustomAction_Throws_WhenSenderItemIsNotWidget()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_sync_action_invalid");
        UserMessageItem nonWidgetSender = new()
        {
            Id = "msg_sync_sender",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Content = [new UserMessageTextContent { Text = "not-a-widget" }],
        };
        await store.AddThreadItemAsync(thread.Id, nonWidgetSender, context);

        CustomActionServer server = new(store);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsSyncCustomActionRequest
            {
                Params = new ThreadCustomActionParams
                {
                    ThreadId = thread.Id,
                    ItemId = nonWidgetSender.Id,
                    Action = new ChatKitAction
                    {
                        Type = "refresh_widget",
                    },
                },
            }),
            context));

        Assert.Contains("threads.sync_custom_action requires a widget sender item", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Client tool output completes the pending tool-call item before the assistant response continues.</summary>
    /// <intent>Protect the tool continuation lane used when the client returns a tool result into an existing thread.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.add_client_tool_output request marks the pending tool call completed, persists the tool result, and then streams the next assistant response.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0009")]
    [Fact]
    public async Task ProcessAsync_ThreadsAddClientToolOutput_CompletesPendingToolCall()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_tool");
        ClientToolCallItem pendingToolCall = new()
        {
            Id = "tc_pending",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            CallId = "call_1",
            Name = "lookup-weather",
            Arguments = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
            {
                ["city"] = "Denver",
            },
        };
        await store.AddThreadItemAsync(thread.Id, pendingToolCall, context);

        bool observedCompletedState = false;
        DelegateServer server = new(store, attachmentStore: null, RespondAfterToolOutputAsync);

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsAddClientToolOutputRequest
            {
                Params = new ThreadAddClientToolOutputParams
                {
                    ThreadId = thread.Id,
                    Result = new JsonObject
                    {
                        ["ok"] = true,
                    },
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        List<string> chunks = await DrainStreamingAsync(streaming);
        ClientToolCallItem completed = Assert.IsType<ClientToolCallItem>(await store.LoadItemAsync(thread.Id, pendingToolCall.Id, context));

        Assert.True(observedCompletedState);
        Assert.Equal("completed", completed.Status);
        Assert.True(completed.Output?["ok"]?.GetValue<bool>());
        Assert.Contains(chunks, chunk => chunk.Contains("tool-finished", StringComparison.Ordinal));

        async IAsyncEnumerable<ThreadStreamEvent> RespondAfterToolOutputAsync(
            ThreadMetadata respondingThread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> respondingContext,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = inputUserMessage;
            _ = respondingContext;
            _ = cancellationToken;

            ClientToolCallItem storedToolCall = Assert.IsType<ClientToolCallItem>(await store.LoadItemAsync(respondingThread.Id, pendingToolCall.Id, context));
            observedCompletedState = true;
            Assert.Equal("completed", storedToolCall.Status);
            Assert.True(storedToolCall.Output?["ok"]?.GetValue<bool>());

            yield return new ThreadItemDoneEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_after_tool",
                    ThreadId = respondingThread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "tool-finished" }],
                },
            };
        }
    }

    /// <summary>Client tool output rejects threads that do not end with a pending tool-call item.</summary>
    /// <intent>Protect the continuation pipeline from applying client tool results to the wrong kind of thread item.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.add_client_tool_output request without a pending client tool call throws an invalid operation exception during stream evaluation.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0009")]
    [Fact]
    public async Task ProcessAsync_ThreadsAddClientToolOutput_Throws_WhenPendingToolCallIsMissing()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_tool_missing");
        await store.AddThreadItemAsync(
            thread.Id,
            new AssistantMessageItem
            {
                Id = "msg_existing",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = [new AssistantMessageContent { Text = "already-finished" }],
            },
            context);

        DelegateServer server = new(store, attachmentStore: null, EmptyEventsAsync);
        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsAddClientToolOutputRequest
            {
                Params = new ThreadAddClientToolOutputParams
                {
                    ThreadId = thread.Id,
                    Result = new JsonObject
                    {
                        ["ok"] = true,
                    },
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DrainStreamingAsync(streaming));

        Assert.Contains("pending ClientToolCallItem", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Retry-after-item removes later items and replays the assistant turn from the retained user message.</summary>
    /// <intent>Protect the destructive retry lane from leaving superseded items behind after a replay.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.retry_after_item request deletes later items after the target user message and streams a replacement assistant response based on that retained message.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0010")]
    [Fact]
    public async Task ProcessAsync_ThreadsRetryAfterItem_RemovesLaterItemsAndReplays()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_retry");
        UserMessageItem retainedUserMessage = new()
        {
            Id = "msg_user_keep",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now().AddMinutes(-3),
            Content = [new UserMessageTextContent { Text = "retry me" }],
        };
        await store.AddThreadItemAsync(thread.Id, retainedUserMessage, context);
        await store.AddThreadItemAsync(
            thread.Id,
            new AssistantMessageItem
            {
                Id = "msg_assistant_old",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now().AddMinutes(-2),
                Content = [new AssistantMessageContent { Text = "old-answer" }],
            },
            context);
        await store.AddThreadItemAsync(
            thread.Id,
            new UserMessageItem
            {
                Id = "msg_user_remove",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now().AddMinutes(-1),
                Content = [new UserMessageTextContent { Text = "remove me" }],
            },
            context);

        string? replayedInputText = null;
        DelegateServer server = new(store, attachmentStore: null, ReplayAssistantTurnAsync);

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsRetryAfterItemRequest
            {
                Params = new ThreadRetryAfterItemParams
                {
                    ThreadId = thread.Id,
                    ItemId = retainedUserMessage.Id,
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        List<string> chunks = await DrainStreamingAsync(streaming);
        Page<ThreadItem> persistedItems = await store.LoadThreadItemsAsync(thread.Id, null, 20, "asc", context);

        Assert.Equal("retry me", replayedInputText);
        Assert.DoesNotContain(persistedItems.Data, item => item.Id == "msg_assistant_old");
        Assert.DoesNotContain(persistedItems.Data, item => item.Id == "msg_user_remove");
        Assert.Contains(persistedItems.Data, item => item.Id == retainedUserMessage.Id);
        Assert.Contains(persistedItems.Data, item => item.Id == "msg_assistant_new");
        Assert.Contains(chunks, chunk => chunk.Contains("replacement-answer", StringComparison.Ordinal));

        async IAsyncEnumerable<ThreadStreamEvent> ReplayAssistantTurnAsync(
            ThreadMetadata respondingThread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> respondingContext,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = respondingContext;
            _ = cancellationToken;
            replayedInputText = Assert.Single(inputUserMessage!.Content.OfType<UserMessageTextContent>()).Text;

            yield return new ThreadItemDoneEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_assistant_new",
                    ThreadId = respondingThread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "replacement-answer" }],
                },
            };

            await Task.CompletedTask;
        }
    }

    /// <summary>Retry-after-item rejects non-user-message targets instead of replaying from an invalid point in the thread.</summary>
    /// <intent>Protect destructive retry from treating assistant or system items as valid replay anchors.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.retry_after_item request for an assistant item throws an invalid operation exception during stream evaluation.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0010")]
    [Fact]
    public async Task ProcessAsync_ThreadsRetryAfterItem_Throws_WhenTargetIsNotUserMessage()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        ThreadMetadata thread = await SeedThreadAsync(store, context, "thr_retry_invalid");
        AssistantMessageItem assistantItem = new()
        {
            Id = "msg_assistant_target",
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Content = [new AssistantMessageContent { Text = "cannot retry from here" }],
        };
        await store.AddThreadItemAsync(thread.Id, assistantItem, context);

        DelegateServer server = new(store, attachmentStore: null, EmptyEventsAsync);
        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsRetryAfterItemRequest
            {
                Params = new ThreadRetryAfterItemParams
                {
                    ThreadId = thread.Id,
                    ItemId = assistantItem.Id,
                },
            }),
            context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DrainStreamingAsync(streaming));

        Assert.Contains("is not a user message", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Stream cancellation persists non-empty pending assistant content and an SDK hidden-context marker.</summary>
    /// <intent>Protect cancellation cleanup so partially emitted assistant content and the interruption marker survive for later turns.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Canceling a streaming threads.create request after a non-empty assistant item is added persists that partial item and appends an SDK hidden-context item.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0008")]
    [Fact]
    public async Task ProcessAsync_CancelledStream_PersistsNonEmptyPendingAssistantItem()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        DelegateServer server = new(store, attachmentStore: null, StreamPartialAssistantUntilCancelledAsync);

        using CancellationTokenSource cancellationSource = new();
        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsCreateRequest
            {
                Params = new ThreadCreateParams
                {
                    Input = new UserMessageInput
                    {
                        Content = [new UserMessageTextContent { Text = "hello" }],
                    },
                },
            }),
            context,
            cancellationSource.Token);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CancelAfterChunkAsync(streaming, cancellationSource, "msg_partial"));

        string threadId = await LoadSingleThreadIdAsync(store, context);
        Page<ThreadItem> persistedItems = await store.LoadThreadItemsAsync(threadId, null, 20, "asc", context);

        Assert.Contains(persistedItems.Data, item => item is AssistantMessageItem { Id: "msg_partial" });
        SdkHiddenContextItem marker = Assert.IsType<SdkHiddenContextItem>(Assert.Single(persistedItems.Data.OfType<SdkHiddenContextItem>()));
        Assert.Contains("cancelled the stream", marker.Content, StringComparison.OrdinalIgnoreCase);

        async IAsyncEnumerable<ThreadStreamEvent> StreamPartialAssistantUntilCancelledAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> respondingContext,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = inputUserMessage;
            _ = respondingContext;

            yield return new ThreadItemAddedEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_partial",
                    ThreadId = thread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [new AssistantMessageContent { Text = "partial answer" }],
                },
            };

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    /// <summary>Stream cancellation skips empty pending assistant content instead of persisting blank items.</summary>
    /// <intent>Protect cancellation cleanup from polluting persisted history with empty assistant placeholders.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Canceling a streaming request after an empty assistant item is added does not persist that assistant item, but still appends the SDK hidden-context marker.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0008")]
    [Fact]
    public async Task ProcessAsync_CancelledStream_SkipsEmptyPendingAssistantItem()
    {
        Dictionary<string, object?> context = new();
        InMemoryChatKitStore<Dictionary<string, object?>> store = new();
        DelegateServer server = new(store, attachmentStore: null, StreamEmptyAssistantUntilCancelledAsync);

        using CancellationTokenSource cancellationSource = new();
        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsCreateRequest
            {
                Params = new ThreadCreateParams
                {
                    Input = new UserMessageInput
                    {
                        Content = [new UserMessageTextContent { Text = "hello" }],
                    },
                },
            }),
            context,
            cancellationSource.Token);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CancelAfterChunkAsync(streaming, cancellationSource, "msg_empty_partial"));

        string threadId = await LoadSingleThreadIdAsync(store, context);
        Page<ThreadItem> persistedItems = await store.LoadThreadItemsAsync(threadId, null, 20, "asc", context);

        Assert.DoesNotContain(persistedItems.Data, item => item.Id == "msg_empty_partial");
        Assert.Single(persistedItems.Data.OfType<SdkHiddenContextItem>());

        async IAsyncEnumerable<ThreadStreamEvent> StreamEmptyAssistantUntilCancelledAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> respondingContext,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _ = inputUserMessage;
            _ = respondingContext;

            yield return new ThreadItemAddedEvent
            {
                Item = new AssistantMessageItem
                {
                    Id = "msg_empty_partial",
                    ThreadId = thread.Id,
                    CreatedAt = ChatKitClock.Now(),
                    Content = [],
                },
            };

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<ThreadStreamEvent> EmptyEventsAsync(
        ThreadMetadata thread,
        UserMessageItem? inputUserMessage,
        Dictionary<string, object?> context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = thread;
        _ = inputUserMessage;
        _ = context;
        _ = cancellationToken;
        await Task.CompletedTask;
        yield break;
    }

    private static async Task<List<string>> DrainStreamingAsync(StreamingResult streaming)
    {
        List<string> chunks = [];
        await foreach (byte[] chunk in streaming)
        {
            chunks.Add(Encoding.UTF8.GetString(chunk));
        }

        return chunks;
    }

    private static async Task CancelAfterChunkAsync(StreamingResult streaming, CancellationTokenSource cancellationSource, string itemId)
    {
        await using IAsyncEnumerator<byte[]> enumerator = streaming.GetAsyncEnumerator();
        while (await enumerator.MoveNextAsync())
        {
            string chunk = Encoding.UTF8.GetString(enumerator.Current);
            if (chunk.Contains(itemId, StringComparison.Ordinal))
            {
                cancellationSource.Cancel();
            }
        }
    }

    private static async Task<ThreadMetadata> SeedThreadAsync(
        InMemoryChatKitStore<Dictionary<string, object?>> store,
        Dictionary<string, object?> context,
        string threadId)
    {
        ThreadMetadata thread = new()
        {
            Id = threadId,
            CreatedAt = ChatKitClock.Now(),
            Title = threadId,
        };

        await store.SaveThreadAsync(thread, context);
        return thread;
    }

    private static async Task<string> LoadSingleThreadIdAsync(
        InMemoryChatKitStore<Dictionary<string, object?>> store,
        Dictionary<string, object?> context)
    {
        Page<ThreadMetadata> threads = await store.LoadThreadsAsync(10, null, "asc", context);
        return Assert.Single(threads.Data).Id;
    }

    private sealed class DelegateServer : ChatKitServer<Dictionary<string, object?>>
    {
        private readonly Func<ThreadMetadata, UserMessageItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>> responder;

        public DelegateServer(
            InMemoryChatKitStore<Dictionary<string, object?>> store,
            AttachmentStore<Dictionary<string, object?>>? attachmentStore,
            Func<ThreadMetadata, UserMessageItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>> responder)
            : base(store, attachmentStore)
        {
            this.responder = responder;
        }

        public override IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => responder(thread, inputUserMessage, context, cancellationToken);
    }

    private sealed class CustomActionServer : ChatKitServer<Dictionary<string, object?>>
    {
        private readonly Func<ThreadMetadata, ChatKitAction, WidgetItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>> onAction;
        private readonly Func<ThreadMetadata, ChatKitAction, WidgetItem?, Dictionary<string, object?>, CancellationToken, Task<SyncCustomActionResponse>> onSyncAction;

        public CustomActionServer(
            InMemoryChatKitStore<Dictionary<string, object?>> store,
            Func<ThreadMetadata, ChatKitAction, WidgetItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>>? onAction = null,
            Func<ThreadMetadata, ChatKitAction, WidgetItem?, Dictionary<string, object?>, CancellationToken, Task<SyncCustomActionResponse>>? onSyncAction = null)
            : base(store)
        {
            this.onAction = onAction ?? ((_, _, _, _, _) => EmptyEventsAsync(default!, default, default!, default));
            this.onSyncAction = onSyncAction ?? ((_, _, _, _, _) => Task.FromResult(new SyncCustomActionResponse()));
        }

        public override IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => EmptyEventsAsync(thread, inputUserMessage, context, cancellationToken);

        public override IAsyncEnumerable<ThreadStreamEvent> ActionAsync(
            ThreadMetadata thread,
            ChatKitAction action,
            WidgetItem? sender,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => onAction(thread, action, sender, context, cancellationToken);

        public override Task<SyncCustomActionResponse> SyncActionAsync(
            ThreadMetadata thread,
            ChatKitAction action,
            WidgetItem? sender,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => onSyncAction(thread, action, sender, context, cancellationToken);
    }

    private sealed class RecordingAttachmentStore : AttachmentStore<Dictionary<string, object?>>
    {
        public List<AttachmentCreateParams> CreateRequests { get; } = [];

        public List<string> DeletedAttachmentIds { get; } = [];

        public List<Attachment> CreatedAttachments { get; } = [];

        public override Task<Attachment> CreateAttachmentAsync(AttachmentCreateParams input, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            CreateRequests.Add(input);

            FileAttachment attachment = new()
            {
                Id = GenerateAttachmentId(input.MimeType, context),
                Name = input.Name,
                MimeType = input.MimeType,
                UploadDescriptor = new AttachmentUploadDescriptor
                {
                    Url = "https://uploads.contoso.test/chatkit",
                    Method = "PUT",
                },
            };
            CreatedAttachments.Add(attachment);
            return Task.FromResult<Attachment>(attachment);
        }

        public override Task DeleteAttachmentAsync(string attachmentId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            DeletedAttachmentIds.Add(attachmentId);
            return Task.CompletedTask;
        }
    }
}
