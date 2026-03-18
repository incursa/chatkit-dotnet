using Incursa.OpenAI.ChatKit;
using Incursa.OpenAI.ChatKit.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DemoChatKitServer>();

WebApplication app = builder.Build();
app.MapChatKit<DemoChatKitServer, Dictionary<string, object?>>("/chatkit", _ => new Dictionary<string, object?>(StringComparer.Ordinal));
app.Run();

public sealed class DemoChatKitServer : ChatKitServer<Dictionary<string, object?>>
{
    public DemoChatKitServer()
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
                Content =
                [
                    new AssistantMessageContent
                    {
                        Text = "Hello, world!",
                    },
                ],
            },
        };

        await Task.CompletedTask;
    }
}
