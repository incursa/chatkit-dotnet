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

`Incursa.OpenAI.ChatKit.AspNetCore` owns both the HTTP adapter for the translated ChatKit server surface and the Razor/UI wrapper that packages ChatKit browser assets.

## API surface

- `MapChatKit<TServer, TContext>(IEndpointRouteBuilder, string, Func<HttpContext, TContext>)`
- `AddIncursaOpenAIChatKitAspNetCore(IServiceCollection, Action<ChatKitAspNetCoreOptions>?)`
- `AddIncursaOpenAIChatKitAspNetCore(IServiceCollection, IConfiguration)`
- `AddIncursaOpenAIChatKitAspNetCoreApi(IServiceCollection, string, string?, Action<ChatKitAspNetCoreOptions>?)`
- `AddIncursaOpenAIChatKitAspNetCoreHosted(IServiceCollection, Action<ChatKitAspNetCoreOptions>?)`
- `<incursa-chatkit-assets />`
- `<incursa-chatkit-api />`
- `<incursa-chatkit-hosted />`

The endpoint extension:

- resolves the registered `ChatKitServer<TContext>`
- builds the request context from the current `HttpContext`
- forwards the raw request payload to the core server
- writes either JSON or streamed SSE output back to the client

The Razor wrapper:

- emits packaged CSS and JavaScript from `_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit`
- serializes host configuration into `data-incursa-chatkit-config`
- mounts the upstream `<openai-chatkit>` web component without per-page bootstrapping code
- supports both conventional local endpoints and direct browser ChatKit API connections
- is documented in detail in [ChatKit tag helpers](30-contracts/chatkit-tag-helper.md)

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
- keep Razor UI defaults in `ChatKitAspNetCoreOptions`
- include `<incursa-chatkit-assets />` once per rendered page or layout
- use integration tests to validate wire behavior across the endpoint boundary

## Razor usage

For full tag-helper examples, supported attributes, and feature-specific patterns, see [ChatKit tag helpers](30-contracts/chatkit-tag-helper.md).

When you need to refresh the packaged browser runtime after updating the frontend npm dependencies:

```bash
cd src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime
npm install
npm run build
```

## References

- `samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs`
- `tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`
