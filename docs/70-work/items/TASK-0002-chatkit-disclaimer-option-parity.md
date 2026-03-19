---
id: TASK-0002
type: task
status: draft
priority: low
owner: null
created: 2026-03-18
updated: null
tags: [parity, aspnetcore, chatkit-js, disclaimer]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/ChatKitHostClientConfig.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.tsx
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0002 - Add ChatKit `disclaimer` option parity to the ASP.NET Core wrapper

## Summary

The upstream ChatKit.js option surface supports a disclaimer block below the composer, but the ASP.NET Core wrapper does not currently expose it.

## Upstream reference

`@openai/chatkit` exposes:

```ts
disclaimer?: {
  text: string;
  highContrast?: boolean;
}
```

Representative usage inferred from the published type surface:

```ts
const chatkit = useChatKit({
  disclaimer: {
    text: "AI may make mistakes. Verify important details.",
    highContrast: true,
  },
});
```

Source material:

- `@openai/chatkit` type surface: `ChatKitOptions.disclaimer`

## Current .NET gap

There is no .NET option, tag-helper attribute, or browser config member for disclaimer text or high-contrast rendering.

## Implementation notes

- Add a .NET options model for disclaimer settings.
- Decide whether the Razor surface should support both site-wide defaults and per-instance overrides.
- Pass the disclaimer object through the packaged runtime unchanged.
- Add coverage for omitted vs configured disclaimer values.

## Acceptance criteria

- `ChatKitAspNetCoreOptions` can configure disclaimer text and high-contrast mode.
- The `<incursa-chatkit>` surface can override the configured disclaimer per instance.
- The packaged runtime forwards `disclaimer` to `useChatKit(...)`.
- Tests cover serialization and null-handling behavior.
- Docs include a simple example showing disclaimer usage.
