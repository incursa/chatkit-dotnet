using System.Text;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitServerRequestTests
{
    /// <summary>Thread listings honor the translated default page size and descending sort order when no cursor is provided.</summary>
    /// <intent>Protect the non-streaming thread list route from silently drifting away from the translated pagination contract.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.list request without an explicit limit passes the default page size to the store and returns the newest threads first.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0003")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0005")]
    [Fact]
    public async Task ProcessAsync_ThreadsList_UsesDefaultPageSizeAndDescendingOrder()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        DateTime createdAt = ChatKitClock.Now();
        ThreadMetadata older = CreateThread("thr_old", createdAt, "older");
        ThreadMetadata newer = CreateThread(
            "thr_new",
            createdAt.AddMinutes(1),
            "newer",
            new LockedStatus { Reason = "read-only" },
            ["images.contoso.test"]);

        await store.SaveThreadAsync(older, context);
        await store.SaveThreadAsync(newer, context);

        HookServer server = new(store);
        ThreadsListRequest request = new();

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        Page<Thread> page = ChatKitJson.Deserialize<Page<Thread>>(nonStreaming.Json)!;

        Assert.Equal(20, store.LastLoadThreadsLimit);
        Assert.Null(store.LastLoadThreadsAfter);
        Assert.Equal("desc", store.LastLoadThreadsOrder);
        Assert.False(page.HasMore);
        Assert.Null(page.After);
        Assert.Collection(
            page.Data,
            first =>
            {
                Assert.Equal("thr_new", first.Id);
                Assert.Equal("newer", first.Title);
                Assert.IsType<LockedStatus>(first.Status);
                Assert.Equal("images.contoso.test", Assert.Single(first.AllowedImageDomains!));
                Assert.Empty(first.Items.Data);
            },
            second =>
            {
                Assert.Equal("thr_old", second.Id);
                Assert.Equal("older", second.Title);
                Assert.IsType<ActiveStatus>(second.Status);
                Assert.Empty(second.Items.Data);
            });
    }

    /// <summary>Thread listings preserve explicit cursors and limits when callers request a smaller page.</summary>
    /// <intent>Protect the translated pagination contract from regressions in limit and cursor propagation.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.list request with an explicit limit and cursor returns the requested page and exposes the next-page cursor.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0003")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0005")]
    [Fact]
    public async Task ProcessAsync_ThreadsList_RespectsCursorAndLimit()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        DateTime createdAt = ChatKitClock.Now();
        ThreadMetadata first = CreateThread("thr_1", createdAt, "one");
        ThreadMetadata second = CreateThread("thr_2", createdAt.AddMinutes(1), "two");
        ThreadMetadata third = CreateThread("thr_3", createdAt.AddMinutes(2), "three");

        await store.SaveThreadAsync(first, context);
        await store.SaveThreadAsync(second, context);
        await store.SaveThreadAsync(third, context);

        HookServer server = new(store);
        ThreadsListRequest request = new()
        {
            Params = new ThreadListParams
            {
                Limit = 1,
                After = first.Id,
                Order = "asc",
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        Page<Thread> page = ChatKitJson.Deserialize<Page<Thread>>(nonStreaming.Json)!;

        Assert.Equal(1, store.LastLoadThreadsLimit);
        Assert.Equal(first.Id, store.LastLoadThreadsAfter);
        Assert.Equal("asc", store.LastLoadThreadsOrder);
        Assert.True(page.HasMore);
        Assert.Equal(second.Id, page.After);
        Assert.Collection(
            page.Data,
            item =>
            {
                Assert.Equal(second.Id, item.Id);
                Assert.Equal("two", item.Title);
                Assert.Empty(item.Items.Data);
            });
    }

    /// <summary>Adding a user message to an existing thread persists the user turn and streams the assistant response from the translated hook.</summary>
    /// <intent>Protect the add-user-message lane from losing the user payload or skipping store-backed persistence before the assistant turn begins.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.add_user_message request stores the user message, passes it into RespondAsync, and streams the assistant reply.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0004")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0005")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsAddUserMessage_PersistsUserMessageAndStreamsAssistantResponse()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        ThreadMetadata thread = CreateThread("thr_message", ChatKitClock.Now(), "message thread");
        await store.SaveThreadAsync(thread, context);

        UserMessageItem? observedUserMessage = null;
        HookServer server = new(
            store,
            respond: (respondingThread, inputUserMessage, respondingContext, cancellationToken) =>
            {
                _ = respondingThread;
                _ = respondingContext;
                _ = cancellationToken;
                observedUserMessage = inputUserMessage;
                return StreamAssistantReplyAsync(respondingThread);
            });

        ThreadsAddUserMessageRequest request = new()
        {
            Params = new ThreadAddUserMessageParams
            {
                ThreadId = thread.Id,
                Input = new UserMessageInput
                {
                    Content =
                    [
                        new UserMessageTextContent { Text = "follow up" },
                    ],
                },
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        List<string> chunks = await DrainStreamingAsync(streaming);
        Page<ThreadItem> items = await store.LoadThreadItemsAsync(thread.Id, null, 20, "asc", context);

        Assert.NotNull(observedUserMessage);
        Assert.Equal(thread.Id, observedUserMessage!.ThreadId);
        UserMessageTextContent observedContent = Assert.IsType<UserMessageTextContent>(Assert.Single(observedUserMessage.Content));
        Assert.Equal("follow up", observedContent.Text);
        Assert.StartsWith("msg_", observedUserMessage.Id);
        Assert.Collection(
            items.Data,
            first =>
            {
                UserMessageItem userMessage = Assert.IsType<UserMessageItem>(first);
                Assert.Equal(observedUserMessage.Id, userMessage.Id);
                UserMessageTextContent userContent = Assert.IsType<UserMessageTextContent>(Assert.Single(userMessage.Content));
                Assert.Equal("follow up", userContent.Text);
            },
            second =>
            {
                AssistantMessageItem assistantMessage = Assert.IsType<AssistantMessageItem>(second);
                Assert.Equal("assistant reply", Assert.Single(assistantMessage.Content).Text);
            });
        Assert.Contains(chunks, chunk => chunk.Contains("\"type\":\"stream_options\"", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Contains("\"type\":\"thread.item.done\"", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Contains("assistant reply", StringComparison.Ordinal));
    }

    /// <summary>Attached files and images are normalized to the destination thread before the owning user message is persisted.</summary>
    /// <intent>Protect the attachment lifecycle from losing thread ownership or saving the user turn before its attachments are materialized.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.add_user_message request with attachments rewrites the attachment thread ids, persists the attachments first, and then stores the user message.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0011")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ThreadsAddUserMessage_PersistsAttachmentsBeforeUserMessageAndRewritesThreadIds()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        ThreadMetadata thread = CreateThread("thr_attachments", ChatKitClock.Now(), "attachments");
        FileAttachment fileAttachment = new()
        {
            Id = "atc_file",
            Name = "report.pdf",
            MimeType = "application/pdf",
        };
        ImageAttachment imageAttachment = new()
        {
            Id = "atc_image",
            Name = "diagram.png",
            MimeType = "image/png",
            PreviewUrl = "https://cdn.contoso.test/diagram.png",
        };

        await store.SaveThreadAsync(thread, context);
        await store.SaveAttachmentAsync(fileAttachment, context);
        await store.SaveAttachmentAsync(imageAttachment, context);
        store.CallLog.Clear();

        HookServer server = new(
            store,
            respond: (respondingThread, inputUserMessage, respondingContext, cancellationToken) =>
            {
                _ = respondingContext;
                _ = cancellationToken;
                Assert.NotNull(inputUserMessage);
                return StreamAssistantReplyAsync(respondingThread);
            });

        ThreadsAddUserMessageRequest request = new()
        {
            Params = new ThreadAddUserMessageParams
            {
                ThreadId = thread.Id,
                Input = new UserMessageInput
                {
                    Content =
                    [
                        new UserMessageTextContent { Text = "with attachments" },
                    ],
                    Attachments =
                    [
                        fileAttachment.Id,
                        imageAttachment.Id,
                    ],
                },
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        _ = await DrainStreamingAsync(streaming);

        Page<ThreadItem> items = await store.LoadThreadItemsAsync(thread.Id, null, 20, "asc", context);
        UserMessageItem userMessage = Assert.IsType<UserMessageItem>(items.Data[0]);
        Assert.IsType<AssistantMessageItem>(items.Data[1]);
        Assert.Collection(
            userMessage.Attachments,
            attachment =>
            {
                FileAttachment persisted = Assert.IsType<FileAttachment>(attachment);
                Assert.Equal(thread.Id, persisted.ThreadId);
                Assert.Equal(fileAttachment.Id, persisted.Id);
            },
            attachment =>
            {
                ImageAttachment persisted = Assert.IsType<ImageAttachment>(attachment);
                Assert.Equal(thread.Id, persisted.ThreadId);
                Assert.Equal(imageAttachment.Id, persisted.Id);
            });
        Assert.Collection(
            store.CallLog,
            entry => Assert.Equal("SaveAttachment:atc_file", entry),
            entry => Assert.Equal("SaveAttachment:atc_image", entry),
            entry => Assert.StartsWith($"AddThreadItem:{thread.Id}:msg_", entry, StringComparison.Ordinal),
            entry => Assert.StartsWith($"AddThreadItem:{thread.Id}:msg_assistant", entry, StringComparison.Ordinal));
        Assert.Equal(thread.Id, (await store.LoadAttachmentAsync(fileAttachment.Id, context)).ThreadId);
        Assert.Equal(thread.Id, (await store.LoadAttachmentAsync(imageAttachment.Id, context)).ThreadId);
    }

    /// <summary>Thread metadata updates are persisted through the store boundary and returned as the updated public thread shape.</summary>
    /// <intent>Protect the update route from mutating thread titles without writing through the storage boundary.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.update request saves the updated title and returns the updated thread payload.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0005")]
    [Fact]
    public async Task ProcessAsync_ThreadsUpdate_PersistsUpdatedTitle()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        ThreadMetadata thread = CreateThread("thr_update", ChatKitClock.Now(), "before");
        await store.SaveThreadAsync(thread, context);

        HookServer server = new(store);
        ThreadsUpdateRequest request = new()
        {
            Params = new ThreadUpdateParams
            {
                ThreadId = thread.Id,
                Title = "after",
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        Thread updated = ChatKitJson.Deserialize<Thread>(nonStreaming.Json)!;
        ThreadMetadata persisted = await store.LoadThreadAsync(thread.Id, context);

        Assert.Equal(thread.Id, updated.Id);
        Assert.Equal("after", updated.Title);
        Assert.Empty(updated.Items.Data);
        Assert.Equal("after", persisted.Title);
    }

    /// <summary>Thread deletion removes the thread and its items instead of leaving stale conversation state behind.</summary>
    /// <intent>Protect the delete route from orphaning items after the owning thread has been removed.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing a threads.delete request removes the thread from the store and returns an empty JSON object.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0005")]
    [Fact]
    public async Task ProcessAsync_ThreadsDelete_RemovesThreadAndItems()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        ThreadMetadata thread = CreateThread("thr_delete", ChatKitClock.Now(), "delete me");
        await store.SaveThreadAsync(thread, context);
        await store.AddThreadItemAsync(
            thread.Id,
            new UserMessageItem
            {
                Id = "msg_delete",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content =
                [
                    new UserMessageTextContent { Text = "goodbye" },
                ],
            },
            context);

        HookServer server = new(store);
        ThreadsDeleteRequest request = new()
        {
            Params = new ThreadDeleteParams
            {
                ThreadId = thread.Id,
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        await Assert.ThrowsAsync<NotFoundException>(() => store.LoadThreadAsync(thread.Id, context));
        Page<ThreadItem> remainingItems = await store.LoadThreadItemsAsync(thread.Id, null, 20, "asc", context);

        Assert.Equal("{}", Encoding.UTF8.GetString(nonStreaming.Json));
        Assert.Empty(remainingItems.Data);
    }

    /// <summary>Feedback requests hand their thread and item identifiers to the explicit extension hook instead of hard-coding repository-specific behavior.</summary>
    /// <intent>Protect the feedback route from bypassing the server extension point that owns application-specific feedback handling.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an items.feedback request forwards the thread id, item ids, and feedback kind to AddFeedbackAsync and returns an empty object.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_ItemsFeedback_InvokesFeedbackHookAndReturnsEmptyObject()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        string? observedThreadId = null;
        IReadOnlyList<string>? observedItemIds = null;
        string? observedKind = null;
        HookServer server = new(
            store,
            addFeedback: (threadId, itemIds, feedback, respondingContext, cancellationToken) =>
            {
                _ = respondingContext;
                _ = cancellationToken;
                observedThreadId = threadId;
                observedItemIds = itemIds.ToArray();
                observedKind = feedback;
                return Task.CompletedTask;
            });

        ItemsFeedbackRequest request = new()
        {
            Params = new ItemFeedbackParams
            {
                ThreadId = "thr_feedback",
                ItemIds =
                [
                    "msg_1",
                    "msg_2",
                ],
                Kind = "thumbs_up",
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);

        Assert.Equal("{}", Encoding.UTF8.GetString(nonStreaming.Json));
        Assert.Equal("thr_feedback", observedThreadId);
        Assert.Equal(["msg_1", "msg_2"], observedItemIds);
        Assert.Equal("thumbs_up", observedKind);
    }

    /// <summary>Malformed transcription payloads fail fast before the transcription hook is invoked.</summary>
    /// <intent>Protect the transcription route from accepting invalid base64 audio data as if it were valid input.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an input.transcribe request with an invalid base64 payload throws a format exception and does not call TranscribeAsync.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_InputTranscribe_Throws_WhenAudioBase64IsInvalid()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        bool transcribeCalled = false;
        HookServer server = new(
            store,
            transcribe: (audioInput, respondingContext, cancellationToken) =>
            {
                _ = audioInput;
                _ = respondingContext;
                _ = cancellationToken;
                transcribeCalled = true;
                return Task.FromResult(new TranscriptionResult { Text = "unexpected" });
            });

        InputTranscribeRequest request = new()
        {
            Params = new InputTranscribeParams
            {
                AudioBase64 = "not-base64",
                MimeType = "audio/wav",
            },
        };

        await Assert.ThrowsAsync<FormatException>(() => server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request),
            context));

        Assert.False(transcribeCalled);
    }

    /// <summary>Audio transcription requests decode the audio payload, normalize the MIME type, and pass both values into the explicit transcription hook.</summary>
    /// <intent>Protect the transcription route from losing the uploaded bytes or bypassing the application-owned transcription service.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an input.transcribe request decodes the base64 audio input, forwards it to TranscribeAsync, and serializes the returned text.</behavior>
    [Trait("Category", "Positive")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_InputTranscribe_DecodesAudioAndInvokesTranscribeHook()
    {
        Dictionary<string, object?> context = new();
        RecordingStore store = new();
        byte[] audioBytes =
        [
            0x01,
            0x23,
            0x45,
            0x67,
        ];
        AudioInput? observedAudioInput = null;
        HookServer server = new(
            store,
            transcribe: (audioInput, respondingContext, cancellationToken) =>
            {
                _ = respondingContext;
                _ = cancellationToken;
                observedAudioInput = audioInput;
                return Task.FromResult(new TranscriptionResult { Text = "transcribed text" });
            });

        InputTranscribeRequest request = new()
        {
            Params = new InputTranscribeParams
            {
                AudioBase64 = Convert.ToBase64String(audioBytes),
                MimeType = "audio/wav; codecs=1",
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request), context);

        NonStreamingResult nonStreaming = Assert.IsType<NonStreamingResult>(result);
        TranscriptionResult transcription = ChatKitJson.Deserialize<TranscriptionResult>(nonStreaming.Json)!;

        Assert.NotNull(observedAudioInput);
        Assert.Equal(audioBytes, observedAudioInput!.Data);
        Assert.Equal("audio/wav; codecs=1", observedAudioInput.MimeType);
        Assert.Equal("audio/wav", observedAudioInput.MediaType);
        Assert.Equal("transcribed text", transcription.Text);
    }

    /// <summary>Audio transcription requests fail fast when a server does not override the transcription extension point.</summary>
    /// <intent>Protect the transcription route from implying a default transcription implementation where none exists.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>Processing an input.transcribe request against a server that has not overridden TranscribeAsync throws the base not-implemented exception.</behavior>
    [Trait("Category", "Negative")]
    [Trait("Requirement", "REQ-CHATKIT-CORE-0012")]
    [Fact]
    public async Task ProcessAsync_InputTranscribe_Throws_WhenTranscribeAsyncIsNotOverridden()
    {
        HookServer server = new(new RecordingStore());
        InputTranscribeRequest request = new()
        {
            Params = new InputTranscribeParams
            {
                AudioBase64 = Convert.ToBase64String([0x01]),
                MimeType = "audio/wav",
            },
        };

        NotImplementedException exception = await Assert.ThrowsAsync<NotImplementedException>(() => server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request),
            new Dictionary<string, object?>()));

        Assert.Contains("TranscribeAsync must be overridden", exception.Message, StringComparison.Ordinal);
    }

    private static ThreadMetadata CreateThread(
        string id,
        DateTime createdAt,
        string title,
        ThreadStatus? status = null,
        List<string>? allowedImageDomains = null)
        => new()
        {
            Id = id,
            CreatedAt = createdAt,
            Title = title,
            Status = status ?? new ActiveStatus(),
            AllowedImageDomains = allowedImageDomains,
        };

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

    private static async IAsyncEnumerable<ThreadStreamEvent> StreamAssistantReplyAsync(
        ThreadMetadata thread,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ThreadItemDoneEvent
        {
            Item = new AssistantMessageItem
            {
                Id = "msg_assistant",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content =
                [
                    new AssistantMessageContent { Text = "assistant reply" },
                ],
            },
        };

        _ = cancellationToken;
        await Task.CompletedTask;
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

    private sealed class HookServer : ChatKitServer<Dictionary<string, object?>>
    {
        private readonly Func<ThreadMetadata, UserMessageItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>>? respond;
        private readonly Func<string, IReadOnlyList<string>, string, Dictionary<string, object?>, CancellationToken, Task>? addFeedback;
        private readonly Func<AudioInput, Dictionary<string, object?>, CancellationToken, Task<TranscriptionResult>>? transcribe;

        public HookServer(
            RecordingStore store,
            Func<ThreadMetadata, UserMessageItem?, Dictionary<string, object?>, CancellationToken, IAsyncEnumerable<ThreadStreamEvent>>? respond = null,
            Func<string, IReadOnlyList<string>, string, Dictionary<string, object?>, CancellationToken, Task>? addFeedback = null,
            Func<AudioInput, Dictionary<string, object?>, CancellationToken, Task<TranscriptionResult>>? transcribe = null)
            : base(store)
        {
            this.respond = respond;
            this.addFeedback = addFeedback;
            this.transcribe = transcribe;
        }

        public override IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => respond is null
                ? EmptyEventsAsync(thread, inputUserMessage, context, cancellationToken)
                : respond(thread, inputUserMessage, context, cancellationToken);

        public override Task AddFeedbackAsync(
            string threadId,
            IReadOnlyList<string> itemIds,
            string feedback,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => addFeedback is null
                ? Task.CompletedTask
                : addFeedback(threadId, itemIds, feedback, context, cancellationToken);

        public override Task<TranscriptionResult> TranscribeAsync(
            AudioInput audioInput,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
            => transcribe is null
                ? base.TranscribeAsync(audioInput, context, cancellationToken)
                : transcribe(audioInput, context, cancellationToken);
    }

    private sealed class RecordingStore : ChatKitStore<Dictionary<string, object?>>
    {
        private readonly InMemoryChatKitStore<Dictionary<string, object?>> inner = new();

        public List<string> CallLog { get; } = [];

        public int? LastLoadThreadsLimit { get; private set; }

        public string? LastLoadThreadsAfter { get; private set; }

        public string? LastLoadThreadsOrder { get; private set; }

        public override string GenerateThreadId(Dictionary<string, object?> context)
            => inner.GenerateThreadId(context);

        public override string GenerateItemId(string itemType, ThreadMetadata thread, Dictionary<string, object?> context)
            => inner.GenerateItemId(itemType, thread, context);

        public override Task<ThreadMetadata> LoadThreadAsync(string threadId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.LoadThreadAsync(threadId, context, cancellationToken);

        public override Task SaveThreadAsync(ThreadMetadata thread, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.SaveThreadAsync(thread, context, cancellationToken);

        public override Task<Page<ThreadItem>> LoadThreadItemsAsync(string threadId, string? after, int limit, string order, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.LoadThreadItemsAsync(threadId, after, limit, order, context, cancellationToken);

        public override Task SaveAttachmentAsync(Attachment attachment, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
        {
            CallLog.Add($"SaveAttachment:{attachment.Id}");
            return inner.SaveAttachmentAsync(attachment, context, cancellationToken);
        }

        public override Task<Attachment> LoadAttachmentAsync(string attachmentId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.LoadAttachmentAsync(attachmentId, context, cancellationToken);

        public override Task DeleteAttachmentAsync(string attachmentId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.DeleteAttachmentAsync(attachmentId, context, cancellationToken);

        public override Task<Page<ThreadMetadata>> LoadThreadsAsync(int limit, string? after, string order, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
        {
            LastLoadThreadsLimit = limit;
            LastLoadThreadsAfter = after;
            LastLoadThreadsOrder = order;
            return inner.LoadThreadsAsync(limit, after, order, context, cancellationToken);
        }

        public override Task AddThreadItemAsync(string threadId, ThreadItem item, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
        {
            CallLog.Add($"AddThreadItem:{threadId}:{item.Id}");
            return inner.AddThreadItemAsync(threadId, item, context, cancellationToken);
        }

        public override Task SaveItemAsync(string threadId, ThreadItem item, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.SaveItemAsync(threadId, item, context, cancellationToken);

        public override Task<ThreadItem> LoadItemAsync(string threadId, string itemId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.LoadItemAsync(threadId, itemId, context, cancellationToken);

        public override Task DeleteThreadAsync(string threadId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.DeleteThreadAsync(threadId, context, cancellationToken);

        public override Task DeleteThreadItemAsync(string threadId, string itemId, Dictionary<string, object?> context, CancellationToken cancellationToken = default)
            => inner.DeleteThreadItemAsync(threadId, itemId, context, cancellationToken);
    }
}
