# Incursa.OpenAI.ChatKit

[`Incursa.OpenAI.ChatKit`](README.md) is the core .NET runtime for ChatKit.

It contains the protocol models, request routing, thread and item primitives, streaming event types, store abstractions, and server base classes used to implement a ChatKit-compatible backend in .NET.

## Choose this package when

- you need the ChatKit protocol and server runtime in a non-ASP.NET Core host
- you want to own the HTTP layer yourself
- you are building stores, server logic, or tests against the ChatKit model
- you plan to pair it with [`Incursa.OpenAI.ChatKit.AspNetCore`](../Incursa.OpenAI.ChatKit.AspNetCore/README.md) for HTTP endpoints or Razor UI hosting

If you want ASP.NET Core endpoint mapping or Razor tag helpers, install [`Incursa.OpenAI.ChatKit.AspNetCore`](../Incursa.OpenAI.ChatKit.AspNetCore/README.md) as well.

## What this package includes

- ChatKit request, response, event, item, widget, and primitive models
- [`ChatKitServer<TContext>`](ChatKitServer.cs) for request routing and response generation
- [`ChatKitProcessResult`](ChatKitProcessResult.cs), [`StreamingResult`](ChatKitProcessResult.cs), and [`NonStreamingResult`](ChatKitProcessResult.cs)
- store abstractions and the in-memory implementation
- helper types for widgets and agent integration
- [`WidgetDefinition`](WidgetDefinitions.cs) loaders and `Build(...)` support for exported `.widget` files, their encoded widget payloads, schema validation, and Jinja-backed rendering into [`WidgetRoot`](Widgets.cs)

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

1. install [`Incursa.OpenAI.ChatKit`](README.md)
2. install [`Incursa.OpenAI.ChatKit.AspNetCore`](../Incursa.OpenAI.ChatKit.AspNetCore/README.md)
3. register your [`ChatKitServer<TContext>`](ChatKitServer.cs)
4. map [`MapChatKit<TServer, TContext>(...)`](../Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)

## Compatibility and scope

- Targets `.NET 10`
- Translates the maintained server-side surface from `openai/chatkit-python`
- Keeps the core runtime separate from ASP.NET Core-specific hosting concerns

## Related package

- [`Incursa.OpenAI.ChatKit.AspNetCore`](../Incursa.OpenAI.ChatKit.AspNetCore/README.md): ASP.NET Core endpoint mapping, Razor tag helpers, and packaged browser assets
