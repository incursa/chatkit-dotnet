---
workbench:
  type: doc
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js
  pathHistory: []
  path: /docs/30-contracts/chatkit-tag-helper.md
---

# ChatKit Tag Helpers

`Incursa.OpenAI.ChatKit.AspNetCore` ships Razor tag helpers that mount the upstream `<openai-chatkit>` web component and feed it a serialized `ChatKitOptions` object.

This guide documents the wrapper surface exposed by the .NET package, not the upstream JavaScript package directly. The goal is parity where the wrapper can serialize the same option shape, while keeping the API idiomatic for Razor.

## What you can do

- mount ChatKit from a Razor layout, page, or view without writing page bootstrap code
- choose between custom API mode and OpenAI-hosted mode explicitly
- configure layout, theme, header, history, start screen, composer, disclaimer, entities, and thread-item actions
- wire browser-side client tool handlers, entity handlers, and widget action handlers through dotted lookup paths
- use the packaged CSS and JS assets to mount the upstream web component consistently

## Tags

- `<incursa-chatkit-assets />`
- `<incursa-chatkit-api />`
- `<incursa-chatkit-hosted />`
- `<incursa-chatkit />` throws by design and exists only to force callers onto one of the explicit mode helpers

## Typical setup

```cshtml
@addTagHelper *, Incursa.OpenAI.ChatKit.AspNetCore

<incursa-chatkit-assets />
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key" />
```

The asset tag helper renders the packaged browser runtime. The host tag helper writes a `data-incursa-chatkit-config` payload that the runtime reads, converts into `ChatKitOptions`, and passes to `<openai-chatkit>.setOptions(...)`.

## Choose a mode

### Custom API mode

Use `<incursa-chatkit-api>` when your ASP.NET Core app hosts the ChatKit endpoint itself.

```cshtml
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key" />
```

This mode:

- requires `api-url`
- requires `domain-key`
- does not allow `session-endpoint`
- does not allow `action-endpoint`
- can still render client-side widget handlers

### OpenAI-hosted mode

Use `<incursa-chatkit-hosted>` when the browser should fetch a client secret from your local session endpoint and optionally forward widget actions to your local action endpoint.

```cshtml
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    action-endpoint="/api/chatkit/action" />
```

This mode:

- requires `session-endpoint`
- forbids `api-url`
- forbids `domain-key`
- requires `action-endpoint` only when widget forwarding is enabled

## Theme

The wrapper supports the same top-level theme entry point as the JS package:

- `theme`
- `theme-radius`
- `theme-density`
- `theme-base-size`
- `theme-font-family`
- `theme-font-family-mono`
- `theme-color-grayscale-hue`
- `theme-color-grayscale-tint`
- `theme-color-grayscale-shade`
- `theme-color-accent-primary`
- `theme-color-accent-level`
- `theme-color-surface-background`
- `theme-color-surface-foreground`

Example:

```cshtml
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key"
    theme="dark"
    theme-base-size="16"
    theme-font-family="Inter"
    theme-font-family-mono="IBM Plex Mono"
    theme-color-accent-primary="#8B5CF6"
    theme-color-accent-level="2"
    theme-color-surface-background="#111111"
    theme-color-surface-foreground="#F5F5F5" />
```

For richer typography, use `ChatKitAspNetCoreOptions.Theme.Typography`:

```csharp
builder.Services.AddOpenAIChatKitApi("/api/chatkit", "contoso-domain-key", options =>
{
    options.Theme.Typography.BaseSize = 16;
    options.Theme.Typography.FontFamily = "Inter";
    options.Theme.Typography.FontSources.Add(new ChatKitFontSource
    {
        Family = "Inter",
        Src = "/fonts/inter.woff2",
        Display = "swap"
    });
});
```

## Header

The wrapper exposes the upstream header toggles plus callback-backed action buttons.

Available attributes:

- `header-enabled`
- `header-title`
- `header-title-enabled`
- `header-left-action-icon`
- `header-left-action-handler`
- `header-right-action-icon`
- `header-right-action-handler`

Example:

```cshtml
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    header-enabled="true"
    header-title="Workspace assistant"
    header-left-action-icon="sidebar-left"
    header-left-action-handler="window.chatkit.onHeaderAction" />
```

Header action handlers are resolved from the browser at runtime using dotted lookup paths such as `window.chatkit.onHeaderAction` or `app.chatkit.onHeaderAction`.

## Start screen

Use the start screen for greetings and starter prompts.

Available attributes:

- `greeting`
- `starter-prompts`

Examples:

```csharp
builder.Services.AddOpenAIChatKitHosted(options =>
{
    options.StartScreen.Greeting = "How can I help today?";
    options.StartScreen.Prompts.Add(new ChatKitStartPrompt
    {
        Label = "Summarize",
        Prompt = "Summarize the latest contract changes.",
        Icon = "document"
    });
});
```

```csharp
builder.Services.AddOpenAIChatKitHosted(options =>
{
    options.StartScreen.Prompts.Add(new ChatKitStartPrompt
    {
        Label = "Rich prompt",
        Prompt = new UserMessageContent[]
        {
            new UserMessageTextContent { Text = "Review " },
            new UserMessageTagContent
            {
                Id = "doc-1",
                Text = "Q2 Planning Doc"
            }
        },
        Icon = "book-open"
    });
});
```

## Composer

The wrapper surfaces the same composer groups the JS package exposes:

- `placeholder`
- `composer-attachments-enabled`
- `composer-attachments-max-size`
- `composer-attachments-max-count`
- `composer-dictation-enabled`
- `client-tool-handlers`
- `entity-handlers`
- `entity-show-composer-menu`

The .NET options model also exposes composer attachments, tools, models, and dictation defaults:

```csharp
builder.Services.AddOpenAIChatKitApi("/api/chatkit", "contoso-domain-key", options =>
{
    options.Composer.Placeholder = "Ask the assistant";
    options.Composer.Attachments.Enabled = true;
    options.Composer.Attachments.MaxSize = 4 * 1024 * 1024;
    options.Composer.Attachments.Accept = new Dictionary<string, string[]>
    {
        ["application/pdf"] = [".pdf"]
    };
    options.Composer.Tools.Add(new ChatKitComposerTool
    {
        Id = "summarize",
        Label = "Summarize",
        Icon = "book-open"
    });
    options.Composer.Models.Add(new ChatKitComposerModel
    {
        Id = "gpt-4.1",
        Label = "Quality",
        Default = true
    });
    options.Composer.Dictation.Enabled = true;
});
```

## Client tools

Use `client-tool-handlers` to point at a browser object map:

```html
<script>
  window.chatkitClientTools = {
    async get_selected_canvas_nodes({ name, params }) {
      return {
        nodes: myCanvas.getSelectedNodes(params.project).map((node) => ({
          id: node.id,
          kind: node.type
        }))
      };
    }
  };
</script>
```

```cshtml
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key"
    client-tool-handlers="window.chatkitClientTools" />
```

The runtime resolves the path at call time and dispatches by `toolCall.name`.

## Entities

Use `entity-handlers` to supply tag search, click, and preview callbacks:

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
          source: "document"
        }
      }));
    }
  };
</script>
```

```cshtml
<incursa-chatkit-api
    api-url="/api/chatkit"
    domain-key="contoso-domain-key"
    entity-handlers="window.chatkitEntities"
    entity-show-composer-menu="true" />
```

The runtime validates entity search results and preview envelopes before forwarding them to the web component.

## Widget actions

Use `widget-action-handler` for client-side handling of widget actions and `forward-widget-actions` to control server forwarding.

```cshtml
<incursa-chatkit-hosted
    session-endpoint="/api/chatkit/session"
    action-endpoint="/api/chatkit/action"
    widget-action-handler="window.chatkitOnWidgetAction"
    forward-widget-actions="false" />
```

Supported patterns:

- client only: set `widget-action-handler` and disable forwarding
- server only: omit `widget-action-handler` and leave forwarding enabled
- client then server: set both `widget-action-handler` and `action-endpoint`

## Related docs

- [ASP.NET Core hosting](../extensions.md)
- [Quickstart](../quickstart.md)
