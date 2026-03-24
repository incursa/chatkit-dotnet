---
workbench:
  type: reference
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs
  pathHistory: []
  path: /docs/30-contracts/chatkit-tag-helper.md
---

# ChatKit Tag Helpers

The Razor integration exposes three different host tag helpers and one asset tag helper.

## Asset helper

### `<incursa-chatkit-assets>`

This helper emits:

- `/_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit/chatkit.css`
- `https://cdn.platform.openai.com/deployments/chatkit/chatkit.js`
- `/_content/Incursa.OpenAI.ChatKit.AspNetCore/chatkit/chatkit.js`

Behavior notes:

- each asset is emitted at most once per Razor rendering context
- CSS, CDN script, and local module emission can be toggled independently
- if nothing remains to emit, the helper suppresses output

## Host helpers

### `<incursa-chatkit>`

This is intentionally not a real mode.

It exists to fail fast and tell callers to pick one of the explicit modes:

- `<incursa-chatkit-api>`
- `<incursa-chatkit-hosted>`

### `<incursa-chatkit-api>`

Use this when the browser should talk directly to a ChatKit-compatible API endpoint.

Current enforced rules:

- `api-url` is required
- `domain-key` is required after option resolution
- `session-endpoint` is not allowed
- `action-endpoint` is not allowed

### `<incursa-chatkit-hosted>`

Use this when the browser should obtain hosted ChatKit credentials from a local session endpoint.

Current enforced rules:

- `api-url` is not allowed
- `domain-key` is not allowed
- `session-endpoint` is required
- `action-endpoint` is required when widget forwarding is enabled

## Shared host output

Both explicit host helpers render a `<div>` with:

- `data-incursa-chatkit-host="true"`
- `data-incursa-chatkit-config="{...json...}"`
- `class="incursa-chatkit-host ..."`
- optional `id`
- inline height styles when configured

If config building fails, the helper renders:

- `data-incursa-chatkit-error="true"`
- content text `ChatKit failed to initialize.`

## Config precedence

The current precedence model is:

1. explicit tag helper attributes
2. `ChatKitAspNetCoreOptions`
3. hard-coded helper defaults where present

Examples:

- height falls back to `DefaultHeight`, which defaults to `720px`
- hosted session endpoint falls back to `/api/chatkit/session`
- hosted action endpoint falls back to `/api/chatkit/action` when forwarding is enabled

## Callback-related options

The serialized browser config may include lookup paths for:

- `clientToolHandlers`
- `entityHandlers`
- `widgetActionHandler`
- header left/right action callbacks

These are string paths that the browser runtime resolves against `window`.

## Validation rules worth knowing

The helpers enforce several paired-value rules:

- header actions require both an icon and a callback path
- start prompts must have a label and prompt content
- start prompt content must be either:
  - a string
  - a sequence of `UserMessageContent`
- composer tools require `id`, `label`, and `icon`
- composer models require `id` and `label`
- disclaimer config is omitted if text is missing, even if `highContrast` is set

## Widget action forwarding model

`WidgetActionHandler` and `ForwardWidgetActions` are meant to coexist.

Current behavior:

- if `forward-widget-actions="false"`, no action endpoint is emitted
- if both a client handler and forwarding are enabled, the browser gets both pieces of config
- the intended runtime contract is client handling first, endpoint forwarding second

## Practical guidance

Use service-level defaults for repeated visual settings, but still assume explicit mode attributes are required by the current implementation for the mode selector itself:

- API mode: set `api-url`
- hosted mode: set `session-endpoint`

That distinction matters because the service options are broader than the render-time validation rules.
