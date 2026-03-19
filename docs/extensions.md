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

```cshtml
@addTagHelper *, Incursa.OpenAI.ChatKit.AspNetCore

<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key" />
```

Use `<incursa-chatkit-api>` when your application hosts its own ChatKit server through `MapChatKit(...)`. Use `<incursa-chatkit-hosted>` when the browser should use OpenAI-hosted ChatKit through session and action endpoints. The generic `<incursa-chatkit>` tag helper now throws and requires callers to choose one of these explicit modes.

Direct API mode requires a domain key. Set it either through `AddOpenAIChatKitApi(...)` defaults or with the `domain-key` tag-helper attribute.

Client tool handlers use a browser-side registry lookup instead of serialized delegates. Register an object on the page, then reference it from the host tag helper:

```html
<script>
  window.chatkitClientTools = {
    async get_selected_canvas_nodes({ name, params }) {
      return {
        nodes: myCanvas.getSelectedNodes(params.project).map((node) => ({
          id: node.id,
          kind: node.type,
        })),
      };
    },
  };
</script>
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key"
    client-tool-handlers="window.chatkitClientTools" />
```

Each handler receives the upstream ChatKit tool-call payload `{ name, params }`. The object keys must match the server-side `ClientToolCall.name` values, and each handler must return JSON-compatible data.

Entity handlers follow the same browser-registry pattern:

```html
<script>
  window.chatkitEntities = {
    async onTagSearch(query) {
      return searchDocuments(query).map((document) => ({
        id: document.id,
        title: document.title,
        group: "Documents",
        interactive: true,
        data: {
          source: "document",
        },
      }));
    },
    onClick(entity) {
      openDocument(entity.id);
    },
    async onRequestPreview(entity) {
      return {
        preview: buildEntityPreview(entity),
      };
    },
  };
</script>
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key"
    entity-handlers="window.chatkitEntities"
    entity-show-composer-menu="true" />
```

The runtime validates the returned entity array and preview payload shape before forwarding them into `setOptions(...)`. Submitted tags arrive on the server as `UserMessageTagContent` entries so application code can map them back to domain objects.

When you need to refresh the packaged browser runtime after updating the frontend npm dependencies:

```bash
cd src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime
npm install
npm run build
```

## References

- `samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs`
- `tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`
