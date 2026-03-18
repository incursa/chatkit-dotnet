---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
    - samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs
  pathHistory: []
  path: /docs/extensions.md
---

# ASP.NET Core Hosting

`Incursa.OpenAI.ChatKit.AspNetCore` owns the HTTP adapter for the translated ChatKit server surface.

## API surface

- `MapChatKit<TServer, TContext>(IEndpointRouteBuilder, string, Func<HttpContext, TContext>)`

The endpoint extension:

- resolves the registered `ChatKitServer<TContext>`
- builds the request context from the current `HttpContext`
- forwards the raw request payload to the core server
- writes either JSON or streamed SSE output back to the client

## Minimal host sample

```csharp
builder.Services.AddSingleton<DemoChatKitServer>();

WebApplication app = builder.Build();
app.MapChatKit<DemoChatKitServer, Dictionary<string, object?>>(
    "/chatkit",
    _ => new Dictionary<string, object?>());
```

## Hosting guidance

- keep context creation in the composition root
- keep ChatKit transport handling in the ASP.NET Core package
- keep request routing and store behavior in the core package
- use integration tests to validate wire behavior across the endpoint boundary

## References

- `samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs`
- `tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`
