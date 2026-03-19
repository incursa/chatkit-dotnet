# Incursa.OpenAI.ChatKit

`Incursa.OpenAI.ChatKit` is the core .NET runtime for ChatKit.

It contains the protocol models, request routing, thread and item primitives, streaming event types, store abstractions, and server base classes used to implement a ChatKit-compatible backend in .NET.

## Choose this package when

- you need the ChatKit protocol and server runtime in a non-ASP.NET Core host
- you want to own the HTTP layer yourself
- you are building stores, server logic, or tests against the ChatKit model
- you plan to pair it with `Incursa.OpenAI.ChatKit.AspNetCore` for HTTP endpoints or Razor UI hosting

If you want ASP.NET Core endpoint mapping or Razor tag helpers, install `Incursa.OpenAI.ChatKit.AspNetCore` as well.

## What this package includes

- ChatKit request, response, event, item, widget, and primitive models
- `ChatKitServer<TContext>` for request routing and response generation
- `ChatKitProcessResult`, `StreamingResult`, and `NonStreamingResult`
- store abstractions and the in-memory implementation
- helper types for widgets and agent integration

## Minimal example

```csharp
using Incursa.OpenAI.ChatKit;

InMemoryChatKitStore<Dictionary<string, object?>> store = new();
DemoChatKitServer server = new(store);

byte[] requestBytes = ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(new ThreadsGetByIdRequest
{
    Params = new ThreadGetByIdParams
    {
        ThreadId = "thr_1",
    },
});

ChatKitProcessResult result = await server.ProcessAsync(
    requestBytes,
    new Dictionary<string, object?>(),
    CancellationToken.None);

public sealed class DemoChatKitServer : ChatKitServer<Dictionary<string, object?>>
{
    public DemoChatKitServer(InMemoryChatKitStore<Dictionary<string, object?>> store)
        : base(store)
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
                Id = "msg_1",
                ThreadId = thread.Id,
                CreatedAt = ChatKitClock.Now(),
                Content = [new AssistantMessageContent { Text = "Hello from ChatKit." }],
            },
        };

        await Task.CompletedTask;
    }
}
```

## Pairing with ASP.NET Core

For ASP.NET Core hosts:

1. install `Incursa.OpenAI.ChatKit`
2. install `Incursa.OpenAI.ChatKit.AspNetCore`
3. register your `ChatKitServer<TContext>`
4. map `MapChatKit<TServer, TContext>(...)`

## Compatibility and scope

- Targets `.NET 10`
- Translates the maintained server-side surface from `openai/chatkit-python`
- Keeps the core runtime separate from ASP.NET Core-specific hosting concerns

## Related package

- `Incursa.OpenAI.ChatKit.AspNetCore`: ASP.NET Core endpoint mapping, Razor tag helpers, and packaged browser assets
