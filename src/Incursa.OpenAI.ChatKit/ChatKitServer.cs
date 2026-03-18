using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.ChatKit;

public abstract class ChatKitServer<TContext>
{
    private const int DefaultPageSize = 20;

    protected ChatKitServer(ChatKitStore<TContext> store, AttachmentStore<TContext>? attachmentStore = null)
    {
        Store = store;
        AttachmentStore = attachmentStore;
    }

    protected ChatKitStore<TContext> Store { get; }

    protected AttachmentStore<TContext>? AttachmentStore { get; }

    public abstract IAsyncEnumerable<ThreadStreamEvent> RespondAsync(ThreadMetadata thread, UserMessageItem? inputUserMessage, TContext context, CancellationToken cancellationToken = default);

    public virtual Task AddFeedbackAsync(string threadId, IReadOnlyList<string> itemIds, string feedback, TContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public virtual Task<TranscriptionResult> TranscribeAsync(AudioInput audioInput, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TranscribeAsync must be overridden to support input.transcribe.");

    public virtual IAsyncEnumerable<ThreadStreamEvent> ActionAsync(ThreadMetadata thread, ChatKitAction action, WidgetItem? sender, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("ActionAsync must be overridden to react to actions.");

    public virtual Task<SyncCustomActionResponse> SyncActionAsync(ThreadMetadata thread, ChatKitAction action, WidgetItem? sender, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("SyncActionAsync must be overridden to react to sync actions.");

    public virtual StreamOptions GetStreamOptions(ThreadMetadata thread, TContext context)
        => new() { AllowCancel = true };

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

    public Task<ChatKitProcessResult> ProcessAsync(string request, TContext context, CancellationToken cancellationToken = default)
        => ProcessAsync(Encoding.UTF8.GetBytes(request), context, cancellationToken);

    public async Task<ChatKitProcessResult> ProcessAsync(byte[] request, TContext context, CancellationToken cancellationToken = default)
    {
        ChatKitRequest parsedRequest = ChatKitJson.DeserializeRequest(request);
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
