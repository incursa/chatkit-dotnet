---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs
  pathHistory: []
  path: /docs/quickstart.md
---

# Quickstart

[`Incursa.OpenAI.ChatKit`](../src/Incursa.OpenAI.ChatKit/README.md) provides the server-side pieces needed to expose a ChatKit-compatible endpoint from ASP.NET Core.

## Before you begin

- `.NET 10` SDK from `global.json`
- this repo checked out locally

## Build and test

```bash
dotnet restore
dotnet build Incursa.OpenAI.ChatKit.slnx -c Release
dotnet test Incursa.OpenAI.ChatKit.slnx -c Release
```

## Run the sample

```bash
dotnet run --project samples/Incursa.OpenAI.ChatKit.QuickstartSample/Incursa.OpenAI.ChatKit.QuickstartSample.csproj
```

The sample:

- creates an in-memory ChatKit store
- registers a `ChatKitServer<Dictionary<string, object?>>`
- maps `/chatkit`
- returns a simple assistant message through the ChatKit response pipeline

## Minimal setup

```csharp
using Incursa.OpenAI.ChatKit;
using Incursa.OpenAI.ChatKit.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DemoChatKitServer>();

WebApplication app = builder.Build();
app.MapChatKit<DemoChatKitServer, Dictionary<string, object?>>(
    "/chatkit",
    _ => new Dictionary<string, object?>());

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
                Content = [new AssistantMessageContent { Text = "Hello, world!" }],
            },
        };

        await Task.CompletedTask;
    }
}
```

## Next steps

- Continue with the ASP.NET Core hosting details in [extensions.md](extensions.md).
- Review the translated scope in [parity/manifest.md](parity/manifest.md).
- Run the quality lanes before opening changes.
