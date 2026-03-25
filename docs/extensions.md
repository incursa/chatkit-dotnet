---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs
  pathHistory: []
  path: /docs/extensions.md
---

# ASP.NET Core Hosting

[`Incursa.OpenAI.ChatKit.AspNetCore`](../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md) adds two kinds of integration on top of the core runtime:

- HTTP endpoint mapping for ChatKit protocol requests
- Razor-based browser hosting for the ChatKit UI shell

## Endpoint hosting

Register your server and map the endpoint:

```csharp
using Incursa.OpenAI.ChatKit;
using Incursa.OpenAI.ChatKit.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DemoChatKitServer>();

WebApplication app = builder.Build();
app.MapChatKit<DemoChatKitServer, Dictionary<string, object?>>(
    "/chatkit",
    httpContext => new Dictionary<string, object?>(StringComparer.Ordinal));

app.Run();
```

[`MapChatKit(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs) does only four things:

1. buffer the request body
2. create the per-request context
3. invoke [`ChatKitServer<TContext>.ProcessAsync(...)`](../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
4. write JSON or SSE to the response

You still own:

- auth
- authz
- request scoping beyond the context factory
- error policy outside of ChatKit stream errors

## Browser hosting modes

There are two explicit browser modes.

### Direct API mode

Use [`AddOpenAIChatKitApi(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs) and [`<incursa-chatkit-api>`](30-contracts/chatkit-tag-helper.md) when the browser should call a ChatKit API endpoint directly.

```csharp
builder.Services.AddOpenAIChatKitApi(
    "/chatkit",
    "contoso-domain-key",
    options =>
    {
        options.DefaultHeight = "760px";
        options.Locale = "en";
    });
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/chatkit"
    domain-key="contoso-domain-key"
    height="760px" />
```

Operational notes:

- the helper requires `api-url`
- the helper requires a non-empty domain key
- upload strategy settings only apply in this mode

### Hosted session mode

Use [`AddOpenAIChatKitHosted(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs) and [`<incursa-chatkit-hosted>`](30-contracts/chatkit-tag-helper.md) when the browser should fetch a ChatKit client secret from your local app.

```csharp
builder.Services.AddOpenAIChatKitHosted(options =>
{
    options.SessionEndpoint = "/api/chatkit/session";
    options.ActionEndpoint = "/api/chatkit/action";
    options.ForwardWidgetActions = true;
});
```

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    action-endpoint="/api/chatkit/action" />
```

Operational notes:

- the helper requires `session-endpoint`
- `action-endpoint` is required only when widget forwarding is enabled
- direct API settings are disallowed in this mode

## Service registration methods

### [`AddOpenAIChatKit(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)

Use this for neutral shared defaults without committing to a browser transport mode yet.

### [`AddOpenAIChatKitHosted(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)

Use this to clear direct API defaults and configure hosted session mode.

### [`AddOpenAIChatKitApi(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)

Use this to configure direct browser API mode.

Current implementation detail:

- the method signature accepts `string? domainKey`, but the implementation throws if it is null or whitespace

## Asset responsibilities

[`<incursa-chatkit-assets>`](30-contracts/chatkit-tag-helper.md) is responsible for:

- packaged CSS
- upstream ChatKit web component script from the CDN
- the local runtime bootstrap module that reads serialized config from Razor output

If a layout and view both render the helper during one request, duplicates are suppressed.

## Recommended integration pattern

For most apps:

1. keep the core assistant logic in a [`ChatKitServer<TContext>`](../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs) subclass
2. make `TContext` a real request model, not a loose dictionary, once the app has stable auth and tenant needs
3. map the ChatKit endpoint with [`MapChatKit(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)
4. choose exactly one Razor host mode per page
5. centralize repeated UI defaults in [`AddOpenAIChatKitHosted(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs) or [`AddOpenAIChatKitApi(...)`](../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)

## Current gaps outside this package

These are intentionally not solved by the ASP.NET Core package:

- issuing hosted ChatKit session secrets
- handling widget actions in your own controller or endpoint layer
- production attachment storage
- authentication of direct browser API calls
- application-specific persistence choices
