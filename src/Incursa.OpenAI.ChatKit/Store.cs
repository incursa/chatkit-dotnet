namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Defines the canonical item type identifiers used by ChatKit persistence and ID generation.
/// </summary>
public static class StoreItemTypes
{
    /// <summary>
    /// Identifies a thread record.
    /// </summary>
    public const string Thread = "thread";

    /// <summary>
    /// Identifies a message record.
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Identifies a client tool call record.
    /// </summary>
    public const string ToolCall = "tool_call";

    /// <summary>
    /// Identifies a workflow task record.
    /// </summary>
    public const string Task = "task";

    /// <summary>
    /// Identifies a workflow record.
    /// </summary>
    public const string Workflow = "workflow";

    /// <summary>
    /// Identifies an attachment record.
    /// </summary>
    public const string Attachment = "attachment";

    /// <summary>
    /// Identifies SDK-managed hidden context.
    /// </summary>
    public const string SdkHiddenContext = "sdk_hidden_context";
}

/// <summary>
/// Generates compact ChatKit identifiers for persisted objects.
/// </summary>
public static class ChatKitIdentifiers
{
    private static readonly IReadOnlyDictionary<string, string> Prefixes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [StoreItemTypes.Thread] = "thr",
        [StoreItemTypes.Message] = "msg",
        [StoreItemTypes.ToolCall] = "tc",
        [StoreItemTypes.Task] = "tsk",
        [StoreItemTypes.Workflow] = "wf",
        [StoreItemTypes.Attachment] = "atc",
        [StoreItemTypes.SdkHiddenContext] = "shcx",
    };

    /// <summary>
    /// Creates a new identifier for the specified ChatKit item type.
    /// </summary>
    /// <param name="itemType">The ChatKit item type.</param>
    /// <returns>A new compact identifier with the appropriate prefix.</returns>
    public static string Create(string itemType)
    {
        string prefix = Prefixes[itemType];
        return $"{prefix}_{Guid.NewGuid():N}"[..(prefix.Length + 9)];
    }
}

/// <summary>
/// Represents a missing ChatKit resource in the backing store.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Defines attachment-specific persistence operations required by the ChatKit server.
/// </summary>
/// <typeparam name="TContext">The application request context type.</typeparam>
public abstract class AttachmentStore<TContext>
{
    /// <summary>
    /// Generates a new attachment identifier.
    /// </summary>
    /// <param name="mimeType">The MIME type of the attachment being created.</param>
    /// <param name="context">The application request context.</param>
    /// <returns>A new attachment identifier.</returns>
    public virtual string GenerateAttachmentId(string mimeType, TContext context)
        => ChatKitIdentifiers.Create(StoreItemTypes.Attachment);

    /// <summary>
    /// Creates a new attachment and returns the resulting descriptor.
    /// </summary>
    /// <param name="input">The attachment creation request.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created attachment descriptor.</returns>
    public virtual Task<Attachment> CreateAttachmentAsync(AttachmentCreateParams input, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException($"{GetType().Name} must override CreateAttachmentAsync to support two-phase file upload.");

    /// <summary>
    /// Deletes an attachment from the backing store.
    /// </summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines thread and item persistence operations required by the ChatKit server.
/// </summary>
/// <typeparam name="TContext">The application request context type.</typeparam>
public abstract class ChatKitStore<TContext>
{
    /// <summary>
    /// Generates a new thread identifier.
    /// </summary>
    /// <param name="context">The application request context.</param>
    /// <returns>A new thread identifier.</returns>
    public virtual string GenerateThreadId(TContext context)
        => ChatKitIdentifiers.Create(StoreItemTypes.Thread);

    /// <summary>
    /// Generates a new item identifier for the given thread item type.
    /// </summary>
    /// <param name="itemType">The ChatKit item type.</param>
    /// <param name="thread">The owning thread metadata.</param>
    /// <param name="context">The application request context.</param>
    /// <returns>A new item identifier.</returns>
    public virtual string GenerateItemId(string itemType, ThreadMetadata thread, TContext context)
        => ChatKitIdentifiers.Create(itemType);

    /// <summary>
    /// Loads thread metadata by identifier.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded thread metadata.</returns>
    public abstract Task<ThreadMetadata> LoadThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves thread metadata.
    /// </summary>
    /// <param name="thread">The thread metadata to save.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task SaveThreadAsync(ThreadMetadata thread, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a page of thread items.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="after">The optional pagination cursor.</param>
    /// <param name="limit">The page size limit.</param>
    /// <param name="order">The sort order.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of thread items.</returns>
    public abstract Task<Page<ThreadItem>> LoadThreadItemsAsync(string threadId, string? after, int limit, string order, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an attachment descriptor.
    /// </summary>
    /// <param name="attachment">The attachment to save.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task SaveAttachmentAsync(Attachment attachment, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an attachment descriptor by identifier.
    /// </summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded attachment.</returns>
    public abstract Task<Attachment> LoadAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an attachment descriptor.
    /// </summary>
    /// <param name="attachmentId">The attachment identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a page of threads.
    /// </summary>
    /// <param name="limit">The page size limit.</param>
    /// <param name="after">The optional pagination cursor.</param>
    /// <param name="order">The sort order.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of thread metadata.</returns>
    public abstract Task<Page<ThreadMetadata>> LoadThreadsAsync(int limit, string? after, string order, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a new item to a thread.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="item">The item to append.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task AddThreadItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a thread item by replacing any existing item with the same identifier.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="item">The item to save.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task SaveItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a thread item by identifier.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded thread item.</returns>
    public abstract Task<ThreadItem> LoadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a thread and its items.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task DeleteThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific item from a thread.
    /// </summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="context">The application request context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public abstract Task DeleteThreadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides an in-memory implementation of <see cref="ChatKitStore{TContext}"/> for tests and local experimentation.
/// </summary>
/// <typeparam name="TContext">The application request context type.</typeparam>
public sealed class InMemoryChatKitStore<TContext> : ChatKitStore<TContext>
{
    private readonly Dictionary<string, ThreadMetadata> threads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ThreadItem>> items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Attachment> attachments = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override Task<ThreadMetadata> LoadThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(threads.TryGetValue(threadId, out ThreadMetadata? thread)
            ? thread
            : throw new NotFoundException($"Thread {threadId} not found"));

    /// <inheritdoc />
    public override Task SaveThreadAsync(ThreadMetadata thread, TContext context, CancellationToken cancellationToken = default)
    {
        threads[thread.Id] = thread;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<Page<ThreadItem>> LoadThreadItemsAsync(string threadId, string? after, int limit, string order, TContext context, CancellationToken cancellationToken = default)
    {
        List<ThreadItem> page = items.TryGetValue(threadId, out List<ThreadItem>? threadItems)
            ? threadItems.OrderBy(x => x.CreatedAt).ToList()
            : [];

        if (string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase))
        {
            page.Reverse();
        }

        int start = 0;
        if (!string.IsNullOrWhiteSpace(after))
        {
            int index = page.FindIndex(x => string.Equals(x.Id, after, StringComparison.Ordinal));
            if (index >= 0)
            {
                start = index + 1;
            }
        }

        List<ThreadItem> data = page.Skip(start).Take(limit).ToList();
        bool hasMore = start + limit < page.Count;
        string? nextAfter = hasMore && data.Count > 0 ? data[^1].Id : null;

        return Task.FromResult(new Page<ThreadItem>
        {
            Data = data,
            HasMore = hasMore,
            After = nextAfter,
        });
    }

    /// <inheritdoc />
    public override Task SaveAttachmentAsync(Attachment attachment, TContext context, CancellationToken cancellationToken = default)
    {
        attachments[attachment.Id] = attachment;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<Attachment> LoadAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(attachments.TryGetValue(attachmentId, out Attachment? attachment)
            ? attachment
            : throw new NotFoundException($"Attachment {attachmentId} not found"));

    /// <inheritdoc />
    public override Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default)
    {
        attachments.Remove(attachmentId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<Page<ThreadMetadata>> LoadThreadsAsync(int limit, string? after, string order, TContext context, CancellationToken cancellationToken = default)
    {
        List<ThreadMetadata> page = threads.Values.OrderBy(x => x.CreatedAt).ToList();
        if (string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase))
        {
            page.Reverse();
        }

        int start = 0;
        if (!string.IsNullOrWhiteSpace(after))
        {
            int index = page.FindIndex(x => string.Equals(x.Id, after, StringComparison.Ordinal));
            if (index >= 0)
            {
                start = index + 1;
            }
        }

        List<ThreadMetadata> data = page.Skip(start).Take(limit).ToList();
        bool hasMore = start + limit < page.Count;
        string? nextAfter = hasMore && data.Count > 0 ? data[^1].Id : null;

        return Task.FromResult(new Page<ThreadMetadata>
        {
            Data = data,
            HasMore = hasMore,
            After = nextAfter,
        });
    }

    /// <inheritdoc />
    public override Task AddThreadItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default)
    {
        if (!items.TryGetValue(threadId, out List<ThreadItem>? threadItems))
        {
            threadItems = [];
            items[threadId] = threadItems;
        }

        threadItems.Add(item);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task SaveItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default)
    {
        if (!items.TryGetValue(threadId, out List<ThreadItem>? threadItems))
        {
            threadItems = [];
            items[threadId] = threadItems;
        }

        int index = threadItems.FindIndex(x => string.Equals(x.Id, item.Id, StringComparison.Ordinal));
        if (index < 0)
        {
            threadItems.Add(item);
        }
        else
        {
            threadItems[index] = item;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<ThreadItem> LoadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default)
    {
        if (items.TryGetValue(threadId, out List<ThreadItem>? threadItems))
        {
            ThreadItem? item = threadItems.FirstOrDefault(x => string.Equals(x.Id, itemId, StringComparison.Ordinal));
            if (item is not null)
            {
                return Task.FromResult(item);
            }
        }

        throw new NotFoundException($"Item {itemId} not found in thread {threadId}");
    }

    /// <inheritdoc />
    public override Task DeleteThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default)
    {
        threads.Remove(threadId);
        items.Remove(threadId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task DeleteThreadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default)
    {
        if (items.TryGetValue(threadId, out List<ThreadItem>? threadItems))
        {
            threadItems.RemoveAll(x => string.Equals(x.Id, itemId, StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }
}
