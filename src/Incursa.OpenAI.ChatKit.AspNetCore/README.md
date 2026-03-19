# Incursa.OpenAI.ChatKit.AspNetCore

`Incursa.OpenAI.ChatKit.AspNetCore` is the ASP.NET Core integration package for `Incursa.OpenAI.ChatKit`.

It adds:

- `MapChatKit<TServer, TContext>(...)` for HTTP and SSE endpoint handling
- `AddOpenAIChatKitApi(...)` and `AddOpenAIChatKitHosted(...)` for explicit frontend hosting modes
- Razor tag helpers for mounting the ChatKit frontend from MVC or Razor views
- packaged CSS and JavaScript assets under `_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit`
- options for shared UI defaults across a site or application

## Install

Use this package together with the core runtime package:

```bash
dotnet add package Incursa.OpenAI.ChatKit
dotnet add package Incursa.OpenAI.ChatKit.AspNetCore
```

## What this package is for

- exposing a ChatKit-compatible endpoint from ASP.NET Core
- rendering the ChatKit frontend from Razor layouts, pages, or views
- shipping a repo-managed browser runtime without hand-written page bootstrapping

## What this package is not for

- replacing the core `Incursa.OpenAI.ChatKit` runtime
- generating ChatKit UI assets during normal `dotnet build`
- forcing a specific MVC or Razor Pages structure on your app

## Endpoint mapping

Register your server and map the ChatKit endpoint:

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
```

## Razor UI wrapper

Register shared UI defaults for a custom ChatKit API endpoint:

```csharp
builder.Services.AddOpenAIChatKitApi("/api/chatkit", configure: options =>
{
    options.DefaultHeight = "760px";
    options.StartScreen.Greeting = "How can I help today?";
    options.Theme.ColorScheme = "dark";
    options.Disclaimer.Text = "AI may make mistakes. Verify important details.";
    options.Disclaimer.HighContrast = true;
    options.EntityHandlers = "window.chatkitEntities";
    options.Entities.ShowComposerMenu = true;
});
```

Then use the tag helpers from a layout or view:

```cshtml
@addTagHelper *, Incursa.OpenAI.ChatKit.AspNetCore

<incursa-chatkit-assets />

<incursa-chatkit-api
    id="workspace-assistant"
    class="chatkit-page"
    api-url="/api/chatkit"
    disclaimer-text="AI may make mistakes. Verify important details."
    disclaimer-high-contrast="true">
</incursa-chatkit-api>
```

Use `<incursa-chatkit-api>` when your application maps a custom ChatKit endpoint with `MapChatKit(...)`. This is the explicit wrapper for self-hosted or custom API integrations.

For example, if your app maps a protected ChatKit endpoint at `/api/chatkit`, keep that route on the server side:

```csharp
app.MapChatKit<MyChatKitServer, MyChatKitContext>(
    "/api/chatkit",
    MyChatKitContext.FromHttpContext)
    .RequireAuthorization();
```

Then point the Razor host at that API endpoint:

```cshtml
<incursa-chatkit-api
    api-url="/api/chatkit" />
```

## Client tool handlers

To expose ChatKit `onClientTool` through the Razor wrapper, register a browser-side object whose keys match your server-side `ClientToolCall.name` values, then point the tag helper at that object:

```html
<script>
  window.chatkitClientTools = {
    async get_selected_canvas_nodes({ name, params }) {
      const nodes = myCanvas.getSelectedNodes(params.project);
      return {
        nodes: nodes.map((node) => ({ id: node.id, kind: node.type }))
      };
    }
  };
</script>
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    client-tool-handlers="window.chatkitClientTools">
</incursa-chatkit-api>
```

`client-tool-handlers` accepts a dotted browser lookup path such as `window.chatkitClientTools` or `app.chatkit.clientTools`. Each resolved handler receives the upstream ChatKit tool-call object `{ name, params }` and must return JSON-compatible data. If the lookup path or named tool handler is missing, the packaged runtime throws a clear browser error instead of silently ignoring the tool call.

## Entity handlers

To expose ChatKit `entities` through the Razor wrapper, register a browser-side object with the upstream callback names and point the tag helper at that registry:

```html
<script>
  window.chatkitEntities = {
    async onTagSearch(query) {
      return searchDocuments(query).map((document) => ({
        id: document.id,
        title: document.title,
        group: "Documents",
        icon: "document",
        interactive: true,
        data: {
          source: "document"
        }
      }));
    },
    onClick(entity) {
      openDocument(entity.id);
    },
    async onRequestPreview(entity) {
      return {
        preview: buildEntityPreview(entity)
      };
    }
  };
</script>
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    entity-handlers="window.chatkitEntities"
    entity-show-composer-menu="true">
</incursa-chatkit-api>
```

`entity-handlers` accepts a dotted browser lookup path such as `window.chatkitEntities` or `app.chatkit.entityHandlers`. The runtime looks for optional `onTagSearch(query)`, `onClick(entity)`, and `onRequestPreview(entity)` functions on that object and validates the returned entity list and preview payload shape before passing them to ChatKit.

When the user submits tagged content, the core server receives `UserMessageTagContent` entries alongside regular text content. That lets you convert upstream `@` tags directly into your server-side domain model:

```csharp
using System.Text.Json.Nodes;
using Incursa.OpenAI.ChatKit;

IReadOnlyList<UserMessageTagContent> tags = input.Content
    .OfType<UserMessageTagContent>()
    .ToList();

foreach (UserMessageTagContent tag in tags)
{
    string? source = tag.Data.TryGetValue("source", out JsonNode? value)
        ? value?.GetValue<string>()
        : null;

    // Map the submitted entity tag into your own search, retrieval, or routing flow.
}
```

## OpenAI-hosted mode

If the frontend should use OpenAI-hosted ChatKit through local session and action endpoints, register hosted mode explicitly and use `<incursa-chatkit-hosted>`:

```csharp
builder.Services.AddOpenAIChatKitHosted(options =>
{
    options.SessionEndpoint = "/api/chatkit/session";
    options.ActionEndpoint = "/api/chatkit/action";
});
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    action-endpoint="/api/chatkit/action">
</incursa-chatkit-hosted>
```

`<incursa-chatkit-hosted>` is the explicit wrapper for the hosted `getClientSecret` flow. Use it when your app issues the browser a ChatKit client secret instead of exposing a custom ChatKit API endpoint directly.

## Widget actions

To expose upstream `widgets.onAction` through the Razor wrapper, register a browser-side function and point the tag helper at that lookup path:

```html
<script>
  window.chatkitOnWidgetAction = async (action, widgetItem) => {
    if (action.type !== "save_profile") {
      return;
    }

    await saveProfile(action.payload, widgetItem.id);
  };
</script>
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    widget-action-handler="window.chatkitOnWidgetAction"
    forward-widget-actions="false">
</incursa-chatkit-hosted>
```

`widget-action-handler` accepts a dotted browser lookup path such as `window.chatkitOnWidgetAction` or `app.chatkit.widgets.onAction`. The runtime resolves that function each time a widget action fires and passes the upstream `(action, widgetItem)` arguments unchanged.

Supported patterns:

- Server-only: omit `widget-action-handler` and keep widget forwarding enabled so actions post to `action-endpoint`.
- Client-only: set `widget-action-handler` and disable forwarding with `forward-widget-actions="false"`.
- Client-then-server: set both `widget-action-handler` and `action-endpoint`. The runtime invokes the client callback first and forwards the same action to the endpoint only if the client callback succeeds.

In direct API mode (`<incursa-chatkit-api>`), `widget-action-handler` still works for client-handled widget actions. Local `action-endpoint` forwarding remains unavailable in that mode.

## Updating packaged UI assets

This package intentionally carries both:

- editable frontend source in `ClientApp/chatkit-runtime`
- generated package assets in `wwwroot/chatkit`

When `@openai/chatkit-react` or related npm dependencies need to move forward:

```bash
cd src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime
npm install
npm run build
```

Commit both the dependency file changes and the regenerated files under `wwwroot/chatkit`.

## Related package

- `Incursa.OpenAI.ChatKit`: core ChatKit models, routing, stores, and server runtime
