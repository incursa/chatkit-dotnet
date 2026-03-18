namespace Incursa.OpenAI.ChatKit;

public static class StoreItemTypes
{
    public const string Thread = "thread";
    public const string Message = "message";
    public const string ToolCall = "tool_call";
    public const string Task = "task";
    public const string Workflow = "workflow";
    public const string Attachment = "attachment";
    public const string SdkHiddenContext = "sdk_hidden_context";
}

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

    public static string Create(string itemType)
    {
        string prefix = Prefixes[itemType];
        return $"{prefix}_{Guid.NewGuid():N}"[..(prefix.Length + 9)];
    }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public abstract class AttachmentStore<TContext>
{
    public virtual string GenerateAttachmentId(string mimeType, TContext context)
        => ChatKitIdentifiers.Create(StoreItemTypes.Attachment);

    public virtual Task<Attachment> CreateAttachmentAsync(AttachmentCreateParams input, TContext context, CancellationToken cancellationToken = default)
        => throw new NotImplementedException($"{GetType().Name} must override CreateAttachmentAsync to support two-phase file upload.");

    public abstract Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);
}

public abstract class ChatKitStore<TContext>
{
    public virtual string GenerateThreadId(TContext context)
        => ChatKitIdentifiers.Create(StoreItemTypes.Thread);

    public virtual string GenerateItemId(string itemType, ThreadMetadata thread, TContext context)
        => ChatKitIdentifiers.Create(itemType);

    public abstract Task<ThreadMetadata> LoadThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default);

    public abstract Task SaveThreadAsync(ThreadMetadata thread, TContext context, CancellationToken cancellationToken = default);

    public abstract Task<Page<ThreadItem>> LoadThreadItemsAsync(string threadId, string? after, int limit, string order, TContext context, CancellationToken cancellationToken = default);

    public abstract Task SaveAttachmentAsync(Attachment attachment, TContext context, CancellationToken cancellationToken = default);

    public abstract Task<Attachment> LoadAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);

    public abstract Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default);

    public abstract Task<Page<ThreadMetadata>> LoadThreadsAsync(int limit, string? after, string order, TContext context, CancellationToken cancellationToken = default);

    public abstract Task AddThreadItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default);

    public abstract Task SaveItemAsync(string threadId, ThreadItem item, TContext context, CancellationToken cancellationToken = default);

    public abstract Task<ThreadItem> LoadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default);

    public abstract Task DeleteThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default);

    public abstract Task DeleteThreadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default);
}

public sealed class InMemoryChatKitStore<TContext> : ChatKitStore<TContext>
{
    private readonly Dictionary<string, ThreadMetadata> threads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ThreadItem>> items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Attachment> attachments = new(StringComparer.Ordinal);

    public override Task<ThreadMetadata> LoadThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(threads.TryGetValue(threadId, out ThreadMetadata? thread)
            ? thread
            : throw new NotFoundException($"Thread {threadId} not found"));

    public override Task SaveThreadAsync(ThreadMetadata thread, TContext context, CancellationToken cancellationToken = default)
    {
        threads[thread.Id] = thread;
        return Task.CompletedTask;
    }

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

    public override Task SaveAttachmentAsync(Attachment attachment, TContext context, CancellationToken cancellationToken = default)
    {
        attachments[attachment.Id] = attachment;
        return Task.CompletedTask;
    }

    public override Task<Attachment> LoadAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(attachments.TryGetValue(attachmentId, out Attachment? attachment)
            ? attachment
            : throw new NotFoundException($"Attachment {attachmentId} not found"));

    public override Task DeleteAttachmentAsync(string attachmentId, TContext context, CancellationToken cancellationToken = default)
    {
        attachments.Remove(attachmentId);
        return Task.CompletedTask;
    }

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

    public override Task DeleteThreadAsync(string threadId, TContext context, CancellationToken cancellationToken = default)
    {
        threads.Remove(threadId);
        items.Remove(threadId);
        return Task.CompletedTask;
    }

    public override Task DeleteThreadItemAsync(string threadId, string itemId, TContext context, CancellationToken cancellationToken = default)
    {
        if (items.TryGetValue(threadId, out List<ThreadItem>? threadItems))
        {
            threadItems.RemoveAll(x => string.Equals(x.Id, itemId, StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }
}
