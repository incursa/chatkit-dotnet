# Incursa.OpenAI.ChatKit.AspNetCore

`Incursa.OpenAI.ChatKit.AspNetCore` provides ASP.NET Core hosting helpers, Razor tag helpers, and packaged browser assets for `Incursa.OpenAI.ChatKit`.

## What this package contains

- `MapChatKit<TServer, TContext>(...)` for HTTP endpoint and SSE response handling
- `<incursa-chatkit-assets />` to emit the packaged CSS, package runtime, and ChatKit CDN script
- `<incursa-chatkit />` to render a browser mount point with serialized host configuration
- `ChatKitAspNetCoreOptions` and `AddIncursaOpenAIChatKitAspNetCore(...)` for shared UI defaults

## Registration

```csharp
builder.Services.AddIncursaOpenAIChatKitAspNetCore(options =>
{
    options.DefaultHeight = "760px";
    options.StartScreen.Greeting = "How can I help today?";
    options.Theme.ColorScheme = "dark";
});
```

You can also bind the options from configuration:

```csharp
builder.Services.AddIncursaOpenAIChatKitAspNetCore(
    builder.Configuration.GetSection("ChatKit"));
```

## Razor usage

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

If you use the conventional local endpoints, the host tag helper can infer them automatically:

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit />
```

## Hosted API mode

The packaged frontend can also connect directly to a hosted ChatKit API:

```cshtml
<incursa-chatkit-assets />
<incursa-chatkit
    api-url="https://example.contoso.com/chatkit"
    domain-key="your-domain-key">
</incursa-chatkit>
```

## Updating the packaged UI assets

The package includes both generated assets under `wwwroot/chatkit` and the source used to rebuild them under `ClientApp/chatkit-runtime`.

To update the packaged UI when `@openai/chatkit-react` or related frontend dependencies change:

```bash
cd src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime
npm install
npm run build
```

Commit the updated dependency files and the regenerated files under `wwwroot/chatkit`.
