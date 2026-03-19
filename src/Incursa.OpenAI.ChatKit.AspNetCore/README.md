# Incursa.OpenAI.ChatKit.AspNetCore

`Incursa.OpenAI.ChatKit.AspNetCore` is the ASP.NET Core integration package for `Incursa.OpenAI.ChatKit`.

It adds:

- `MapChatKit<TServer, TContext>(...)` for HTTP and SSE endpoint handling
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

Register shared UI defaults:

```csharp
builder.Services.AddIncursaOpenAIChatKitAspNetCore(options =>
{
    options.DefaultHeight = "760px";
    options.StartScreen.Greeting = "How can I help today?";
    options.Theme.ColorScheme = "dark";
});
```

Then use the tag helpers from a layout or view:

```cshtml
@addTagHelper *, Incursa.OpenAI.ChatKit.AspNetCore

<incursa-chatkit-assets />

<incursa-chatkit
    id="workspace-assistant"
    class="chatkit-page"
    session-endpoint="/api/chatkit/session"
    action-endpoint="/api/chatkit/action">
</incursa-chatkit>
```

If your application uses the conventional local endpoints, the host tag helper can infer them automatically:

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit />
```

## Hosted API mode

If the frontend should connect directly to a hosted ChatKit API instead of local session and action endpoints:

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit
    api-url="https://example.contoso.com/chatkit"
    domain-key="your-domain-key">
</incursa-chatkit>
```

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
