using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Implements the core ChatKit protocol pipeline for a specific application context.
/// </summary>
/// <typeparam name="TContext">The application request context type.</typeparam>
public abstract class ChatKitServer<TContext>
{
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatKitServer{TContext}"/> class.
    /// </summary>
    /// <param name="store">The thread and item store backing the server.</param>
    /// <param name="attachmentStore">The optional attachment store used for upload workflows.</param>
    protected ChatKitServer(ChatKitStore<TContext> store, AttachmentStore<TContext>? attachmentStore = null)
    {
        Store = store;
        AttachmentStore = attachmentStore;
    }

    /// <summary>
    /// Gets the thread store backing the server.
    /// </summary>
    protected ChatKitStore<TContext> Store { get; }

    /// <summary>
    /// Gets the optional attachment store backing the server.
    /// </summary>
    protected AttachmentStore<TContext>? AttachmentStore { get; }

    /// <summary>
    /// Produces the assistant response stream for a user message.
    /// </summary>
    /// <param name="thread">The target thread metadata.</param>
    /// <param name="inputUserMessage">The newly added user message, when one triggered the response.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async sequence of ChatKit stream events.</returns>
    public abstract IAsyncEnumerable<ThreadStreamEvent> RespondAsync(ThreadMetadata thread, UserMessageItem? inputUserMessage, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records user feedback for previously emitted thread items.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="itemIds">The item identifiers receiving feedback.</param>
    /// <param name="feedback">The feedback kind submitted by the client.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public virtual Task AddFeedbackAsync(string threadId, IReadOnlyList<string> itemIds, string feedback, TContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Transcribes audio input into text.
    /// </summary>
    /// <param name="audioInput">The audio payload to transcribe.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transcription result.</returns>
    public virtual Task<TranscriptionResult> TranscribeAsync(AudioInput audioInput, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TranscribeAsync must be overridden to support input.transcribe.");

    /// <summary>
    /// Handles an asynchronous custom action emitted by the client.
    /// </summary>
    /// <param name="thread">The target thread metadata.</param>
    /// <param name="action">The client action payload.</param>
    /// <param name="sender">The widget item that triggered the action, when available.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async sequence of ChatKit stream events.</returns>
    public virtual IAsyncEnumerable<ThreadStreamEvent> ActionAsync(ThreadMetadata thread, ChatKitAction action, WidgetItem? sender, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("ActionAsync must be overridden to react to actions.");

    /// <summary>
    /// Handles a synchronous custom action emitted by the client.
    /// </summary>
    /// <param name="thread">The target thread metadata.</param>
    /// <param name="action">The client action payload.</param>
    /// <param name="sender">The widget item that triggered the action, when available.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The synchronous custom action response.</returns>
    public virtual Task<SyncCustomActionResponse> SyncActionAsync(ThreadMetadata thread, ChatKitAction action, WidgetItem? sender, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("SyncActionAsync must be overridden to react to sync actions.");

    /// <summary>
    /// Returns stream options for the current response.
    /// </summary>
    /// <param name="thread">The target thread metadata.</param>
    /// <param name="context">The application request context.</param>
    /// <returns>The stream options to emit before streaming begins.</returns>
    public virtual StreamOptions GetStreamOptions(ThreadMetadata thread, TContext context)
        => new() { AllowCancel = true };

    /// <summary>
    /// Handles cleanup when a response stream is cancelled.
    /// </summary>
    /// <param name="thread">The target thread metadata.</param>
    /// <param name="pendingItems">Items that had been started but not yet completed.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public virtual async Task HandleStreamCancelledAsync(ThreadMetadata thread, IReadOnlyList<ThreadItem> pendingItems, TContext context, CancellationToken cancellationToken = default)
    {
        foreach (AssistantMessageItem item in pendingItems.OfType<AssistantMessageItem>())
        {
            bool isEmpty = item.Content.Count == 0 || item.Content.All(x => string.IsNullOrWhiteSpace(x.Text));
            if (!isEmpty)
            {
                await Store.AddThreadItemAsync(thread.Id, item, context, cancellationToken).ConfigureAwait(false);
            }
        }

        await Store.AddThreadItemAsync(
            thread.Id,
            new SdkHiddenContextItem
            {
                Id = Store.GenerateItemId(StoreItemTypes.SdkHiddenContext, thread, context),
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = "The user cancelled the stream. Stop responding to the prior request.",
            },
            context,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes a JSON request string and returns either a streaming or non-streaming ChatKit result.
    /// </summary>
    /// <param name="request">The JSON request string.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed ChatKit result.</returns>
    public Task<ChatKitProcessResult> ProcessAsync(string request, TContext context, CancellationToken cancellationToken = default)
        => ProcessAsync(Encoding.UTF8.GetBytes(request), context, cancellationToken);

    /// <summary>
    /// Processes a UTF-8 encoded request payload and returns either a streaming or non-streaming ChatKit result.
    /// </summary>
    /// <param name="request">The UTF-8 encoded request payload.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed ChatKit result.</returns>
    public async Task<ChatKitProcessResult> ProcessAsync(byte[] request, TContext context, CancellationToken cancellationToken = default)
    {
        ChatKitRequest parsedRequest = ChatKitJson.DeserializeRequest(request);
        // The transport shape is part of the ChatKit contract: some operations must
        // stream incremental events while others return a single JSON document.
        if (IsStreamingRequest(parsedRequest))
        {
            return new StreamingResult(ProcessStreamingAsync(parsedRequest, context, cancellationToken));
        }

        return new NonStreamingResult(await ProcessNonStreamingAsync(parsedRequest, context, cancellationToken).ConfigureAwait(false));
    }

    private static bool IsStreamingRequest(ChatKitRequest request)
        => request is ThreadsCreateRequest
            or ThreadsAddUserMessageRequest
            or ThreadsAddClientToolOutputRequest
            or ThreadsRetryAfterItemRequest
            or ThreadsCustomActionRequest;

    private async Task<byte[]> ProcessNonStreamingAsync(ChatKitRequest request, TContext context, CancellationToken cancellationToken)
    {
        switch (request)
        {
            case ThreadsGetByIdRequest getById:
                {
                    Thread thread = await LoadFullThreadAsync(getById.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    return Serialize(ToThreadResponse(thread));
                }
            case ThreadsListRequest listRequest:
                {
                    Page<ThreadMetadata> threads = await Store.LoadThreadsAsync(
                        listRequest.Params.Limit ?? DefaultPageSize,
                        listRequest.Params.After,
                        listRequest.Params.Order,
                        context,
                        cancellationToken).ConfigureAwait(false);

                    return Serialize(new Page<Thread>
                    {
                        Data = threads.Data.Select(ToThreadResponse).ToList(),
                        After = threads.After,
                        HasMore = threads.HasMore,
                    });
                }
            case ItemsFeedbackRequest feedbackRequest:
                await AddFeedbackAsync(feedbackRequest.Params.ThreadId, feedbackRequest.Params.ItemIds, feedbackRequest.Params.Kind, context, cancellationToken).ConfigureAwait(false);
                return Serialize(new JsonObject());
            case AttachmentsCreateRequest createAttachment:
                {
                    Attachment attachment = await GetAttachmentStore().CreateAttachmentAsync(createAttachment.Params, context, cancellationToken).ConfigureAwait(false);
                    await Store.SaveAttachmentAsync(attachment, context, cancellationToken).ConfigureAwait(false);
                    return Serialize(attachment);
                }
            case AttachmentsDeleteRequest deleteAttachment:
                await GetAttachmentStore().DeleteAttachmentAsync(deleteAttachment.Params.AttachmentId, context, cancellationToken).ConfigureAwait(false);
                await Store.DeleteAttachmentAsync(deleteAttachment.Params.AttachmentId, context, cancellationToken).ConfigureAwait(false);
                return Serialize(new JsonObject());
            case InputTranscribeRequest transcribeRequest:
                {
                    AudioInput audio = new()
                    {
                        Data = Convert.FromBase64String(transcribeRequest.Params.AudioBase64),
                        MimeType = transcribeRequest.Params.MimeType,
                    };
                    return Serialize(await TranscribeAsync(audio, context, cancellationToken).ConfigureAwait(false));
                }
            case ItemsListRequest itemsList:
                {
                    Page<ThreadItem> items = await Store.LoadThreadItemsAsync(
                        itemsList.Params.ThreadId,
                        itemsList.Params.After,
                        itemsList.Params.Limit ?? DefaultPageSize,
                        itemsList.Params.Order,
                        context,
                        cancellationToken).ConfigureAwait(false);

                    // Hidden context is part of the server-side conversation state, not the
                    // client-visible history. It stays in storage but is filtered on reads.
                    items = items with { Data = items.Data.Where(x => x is not HiddenContextItem && x is not SdkHiddenContextItem).ToList() };
                    return Serialize(items);
                }
            case ThreadsUpdateRequest updateRequest:
                {
                    ThreadMetadata thread = await Store.LoadThreadAsync(updateRequest.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    thread.Title = updateRequest.Params.Title;
                    await Store.SaveThreadAsync(thread, context, cancellationToken).ConfigureAwait(false);
                    return Serialize(ToThreadResponse(thread));
                }
            case ThreadsDeleteRequest deleteRequest:
                await Store.DeleteThreadAsync(deleteRequest.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                return Serialize(new JsonObject());
            case ThreadsSyncCustomActionRequest syncActionRequest:
                return Serialize(await ProcessSyncCustomActionAsync(syncActionRequest, context, cancellationToken).ConfigureAwait(false));
            default:
                throw new InvalidOperationException($"Unsupported non-streaming request type {request.GetType().Name}.");
        }
    }

    private async IAsyncEnumerable<byte[]> ProcessStreamingAsync(ChatKitRequest request, TContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (ThreadStreamEvent @event in ProcessStreamingEventsAsync(request, context, cancellationToken).ConfigureAwait(false))
        {
            byte[] payload = Serialize(@event);
            // The core runtime emits preformatted SSE chunks so hosts can forward them
            // directly without re-serializing individual event objects.
            yield return Encoding.UTF8.GetBytes($"data: {Encoding.UTF8.GetString(payload)}\n\n");
        }
    }

    private async IAsyncEnumerable<ThreadStreamEvent> ProcessStreamingEventsAsync(ChatKitRequest request, TContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        switch (request)
        {
            case ThreadsCreateRequest createRequest:
                {
                    Thread thread = new()
                    {
                        Id = Store.GenerateThreadId(context),
                        CreatedAt = ChatKitClock.Now(),
                        Items = new Page<ThreadItem>(),
                    };
                    await Store.SaveThreadAsync(thread, context, cancellationToken).ConfigureAwait(false);
                    yield return new ThreadCreatedEvent { Thread = ToThreadResponse(thread) };
                    UserMessageItem userMessage = await BuildUserMessageItemAsync(createRequest.Params.Input, thread, context, cancellationToken).ConfigureAwait(false);
                    await foreach (ThreadStreamEvent @event in ProcessNewThreadItemRespondAsync(thread, userMessage, context, cancellationToken).ConfigureAwait(false))
                    {
                        yield return @event;
                    }
                    yield break;
                }
            case ThreadsAddUserMessageRequest addUserMessage:
                {
                    ThreadMetadata thread = await Store.LoadThreadAsync(addUserMessage.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    UserMessageItem userMessage = await BuildUserMessageItemAsync(addUserMessage.Params.Input, thread, context, cancellationToken).ConfigureAwait(false);
                    await foreach (ThreadStreamEvent @event in ProcessNewThreadItemRespondAsync(thread, userMessage, context, cancellationToken).ConfigureAwait(false))
                    {
                        yield return @event;
                    }
                    yield break;
                }
            case ThreadsAddClientToolOutputRequest toolOutput:
                {
                    ThreadMetadata thread = await Store.LoadThreadAsync(toolOutput.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    // Upstream ChatKit resumes from the latest pending client tool call.
                    // This implementation mirrors that by only looking at the newest item.
                    Page<ThreadItem> items = await Store.LoadThreadItemsAsync(thread.Id, null, 1, "desc", context, cancellationToken).ConfigureAwait(false);
                    ClientToolCallItem? toolCall = items.Data.OfType<ClientToolCallItem>().FirstOrDefault(x => string.Equals(x.Status, "pending", StringComparison.Ordinal));
                    if (toolCall is null)
                    {
                        throw new InvalidOperationException($"Last thread item in {thread.Id} was not a pending ClientToolCallItem.");
                    }

                    toolCall.Output = toolOutput.Params.Result;
                    toolCall.Status = "completed";
                    await Store.SaveItemAsync(thread.Id, toolCall, context, cancellationToken).ConfigureAwait(false);

                    await foreach (ThreadStreamEvent @event in ProcessEventsAsync(thread, context, ct => RespondAsync(thread, null, context, ct), cancellationToken).ConfigureAwait(false))
                    {
                        yield return @event;
                    }
                    yield break;
                }
            case ThreadsRetryAfterItemRequest retryRequest:
                {
                    ThreadMetadata thread = await Store.LoadThreadAsync(retryRequest.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    List<ThreadItem> itemsToRemove = [];
                    UserMessageItem? userMessage = null;
                    // Retry-after-item is destructive: everything after the target message is
                    // discarded before the assistant turn is replayed from that message.
                    await foreach (ThreadItem item in PaginateThreadItemsReverseAsync(retryRequest.Params.ThreadId, context, cancellationToken).ConfigureAwait(false))
                    {
                        if (string.Equals(item.Id, retryRequest.Params.ItemId, StringComparison.Ordinal))
                        {
                            userMessage = item as UserMessageItem ?? throw new InvalidOperationException($"Item {retryRequest.Params.ItemId} is not a user message.");
                            break;
                        }

                        itemsToRemove.Add(item);
                    }

                    if (userMessage is not null)
                    {
                        foreach (ThreadItem item in itemsToRemove)
                        {
                            await Store.DeleteThreadItemAsync(retryRequest.Params.ThreadId, item.Id, context, cancellationToken).ConfigureAwait(false);
                        }

                        await foreach (ThreadStreamEvent @event in ProcessEventsAsync(thread, context, ct => RespondAsync(thread, userMessage, context, ct), cancellationToken).ConfigureAwait(false))
                        {
                            yield return @event;
                        }
                    }
                    yield break;
                }
            case ThreadsCustomActionRequest actionRequest:
                {
                    ThreadMetadata thread = await Store.LoadThreadAsync(actionRequest.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
                    WidgetItem? sender = null;
                    if (!string.IsNullOrWhiteSpace(actionRequest.Params.ItemId))
                    {
                        sender = await Store.LoadItemAsync(actionRequest.Params.ThreadId, actionRequest.Params.ItemId, context, cancellationToken).ConfigureAwait(false) as WidgetItem
                            ?? throw new InvalidOperationException("threads.custom_action requires a widget sender item.");
                    }

                    await foreach (ThreadStreamEvent @event in ProcessEventsAsync(thread, context, ct => ActionAsync(thread, actionRequest.Params.Action, sender, context, ct), cancellationToken).ConfigureAwait(false))
                    {
                        yield return @event;
                    }
                    yield break;
                }
            default:
                throw new InvalidOperationException($"Unsupported streaming request type {request.GetType().Name}.");
        }
    }

    private async Task<SyncCustomActionResponse> ProcessSyncCustomActionAsync(ThreadsSyncCustomActionRequest request, TContext context, CancellationToken cancellationToken)
    {
        ThreadMetadata thread = await Store.LoadThreadAsync(request.Params.ThreadId, context, cancellationToken).ConfigureAwait(false);
        WidgetItem? sender = null;
        if (!string.IsNullOrWhiteSpace(request.Params.ItemId))
        {
            sender = await Store.LoadItemAsync(request.Params.ThreadId, request.Params.ItemId, context, cancellationToken).ConfigureAwait(false) as WidgetItem
                ?? throw new InvalidOperationException("threads.sync_custom_action requires a widget sender item.");
        }

        return await SyncActionAsync(thread, request.Params.Action, sender, context, cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<ThreadStreamEvent> ProcessNewThreadItemRespondAsync(ThreadMetadata thread, UserMessageItem item, TContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (Attachment attachment in item.Attachments)
        {
            // Attachments are persisted before the user item so later server logic can
            // treat the message payload as fully materialized conversation state.
            await Store.SaveAttachmentAsync(attachment, context, cancellationToken).ConfigureAwait(false);
        }

        await Store.AddThreadItemAsync(thread.Id, item, context, cancellationToken).ConfigureAwait(false);
        yield return new ThreadItemDoneEvent { Item = item };

        await foreach (ThreadStreamEvent @event in ProcessEventsAsync(thread, context, ct => RespondAsync(thread, item, context, ct), cancellationToken).ConfigureAwait(false))
        {
            yield return @event;
        }
    }

    private async IAsyncEnumerable<ThreadStreamEvent> ProcessEventsAsync(
        ThreadMetadata thread,
        TContext context,
        Func<CancellationToken, IAsyncEnumerable<ThreadStreamEvent>> stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        yield return new StreamOptionsEvent { StreamOptions = GetStreamOptions(thread, context) };

        // Items can be announced before they are finalized. Keep them in-memory so
        // cancellation handling can decide what partial work should survive.
        ConcurrentDictionary<string, ThreadItem> pendingItems = new(StringComparer.Ordinal);
        IAsyncEnumerator<ThreadStreamEvent> enumerator = stream(cancellationToken).GetAsyncEnumerator(cancellationToken);
        ErrorEvent? error = null;

        try
        {
            while (true)
            {
                ThreadStreamEvent @event;
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    @event = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    // Cancellation is not just a transport concern. The store is updated so
                    // later turns can see that the previous response was interrupted.
                    await HandleStreamCancelledAsync(thread, pendingItems.Values.ToList(), context, cancellationToken).ConfigureAwait(false);
                    throw;
                }
                catch (CustomStreamException ex)
                {
                    error = new ErrorEvent { Code = "custom", Message = ex.Message, AllowRetry = ex.AllowRetry };
                    break;
                }
                catch (StreamException ex)
                {
                    error = new ErrorEvent { Code = ex.Code, Message = ex.Message, AllowRetry = ex.AllowRetry };
                    break;
                }
                catch (Exception)
                {
                    error = new ErrorEvent { Code = ErrorCodes.StreamError, AllowRetry = true };
                    break;
                }

                switch (@event)
                {
                    case ThreadItemAddedEvent added:
                        pendingItems[added.Item.Id] = added.Item;
                        break;
                    case ThreadItemDoneEvent done:
                        await Store.AddThreadItemAsync(thread.Id, done.Item, context, cancellationToken).ConfigureAwait(false);
                        pendingItems.TryRemove(done.Item.Id, out _);
                        break;
                    case ThreadItemRemovedEvent removed:
                        await Store.DeleteThreadItemAsync(thread.Id, removed.ItemId, context, cancellationToken).ConfigureAwait(false);
                        pendingItems.TryRemove(removed.ItemId, out _);
                        break;
                    case ThreadItemReplacedEvent replaced:
                        await Store.SaveItemAsync(thread.Id, replaced.Item, context, cancellationToken).ConfigureAwait(false);
                        pendingItems.TryRemove(replaced.Item.Id, out _);
                        break;
                }

                // Hidden context is intentionally persisted but never echoed back to the UI.
                bool swallow = @event is ThreadItemDoneEvent { Item: HiddenContextItem or SdkHiddenContextItem };
                if (!swallow)
                {
                    yield return @event;
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        if (error is not null)
        {
            yield return error;
        }
    }

    private async Task<UserMessageItem> BuildUserMessageItemAsync(UserMessageInput input, ThreadMetadata thread, TContext context, CancellationToken cancellationToken)
    {
        List<Attachment> attachments = [];
        foreach (string attachmentId in input.Attachments)
        {
            Attachment attachment = await Store.LoadAttachmentAsync(attachmentId, context, cancellationToken).ConfigureAwait(false);
            Attachment updated = attachment switch
            {
                // Attachments can be uploaded before a thread exists. Once they are attached
                // to a concrete user message, rewrite the thread id so storage stays consistent.
                FileAttachment file => file with { ThreadId = thread.Id },
                ImageAttachment image => image with { ThreadId = thread.Id },
                _ => attachment,
            };
            attachments.Add(updated);
        }

        return new UserMessageItem
        {
            Id = Store.GenerateItemId(StoreItemTypes.Message, thread, context),
            ThreadId = thread.Id,
            CreatedAt = ChatKitClock.Now(),
            Content = input.Content,
            Attachments = attachments,
            QuotedText = input.QuotedText,
            InferenceOptions = input.InferenceOptions,
        };
    }

    private async Task<Thread> LoadFullThreadAsync(string threadId, TContext context, CancellationToken cancellationToken)
    {
        ThreadMetadata threadMeta = await Store.LoadThreadAsync(threadId, context, cancellationToken).ConfigureAwait(false);
        Page<ThreadItem> threadItems = await Store.LoadThreadItemsAsync(threadId, null, DefaultPageSize, "asc", context, cancellationToken).ConfigureAwait(false);
        return new Thread
        {
            Id = threadMeta.Id,
            Title = threadMeta.Title,
            CreatedAt = threadMeta.CreatedAt,
            Status = threadMeta.Status,
            AllowedImageDomains = threadMeta.AllowedImageDomains,
            Metadata = threadMeta.Metadata,
            Items = threadItems,
        };
    }

    private async IAsyncEnumerable<ThreadItem> PaginateThreadItemsReverseAsync(string threadId, TContext context, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? after = null;
        while (true)
        {
            Page<ThreadItem> items = await Store.LoadThreadItemsAsync(threadId, after, DefaultPageSize, "desc", context, cancellationToken).ConfigureAwait(false);
            foreach (ThreadItem item in items.Data)
            {
                yield return item;
            }

            if (!items.HasMore)
            {
                break;
            }

            after = items.After;
        }
    }

    private static byte[] Serialize<T>(T obj)
        => ChatKitJson.SerializeToUtf8Bytes(obj);

    private static Thread ToThreadResponse(ThreadMetadata thread)
        => thread is Thread fullThread
            ? new Thread
            {
                Id = fullThread.Id,
                Title = fullThread.Title,
                CreatedAt = fullThread.CreatedAt,
                Status = fullThread.Status,
                AllowedImageDomains = fullThread.AllowedImageDomains,
                Metadata = fullThread.Metadata,
                // Internal context remains available to the server but is stripped from the
                // public thread payload so clients only see user-facing conversation items.
                Items = fullThread.Items with { Data = fullThread.Items.Data.Where(x => x is not HiddenContextItem && x is not SdkHiddenContextItem).ToList() },
            }
            : new Thread
            {
                Id = thread.Id,
                Title = thread.Title,
                CreatedAt = thread.CreatedAt,
                Status = thread.Status,
                AllowedImageDomains = thread.AllowedImageDomains,
                Metadata = thread.Metadata,
                Items = new Page<ThreadItem>(),
            };

    private AttachmentStore<TContext> GetAttachmentStore()
        => AttachmentStore ?? throw new InvalidOperationException("AttachmentStore is not configured.");
}
