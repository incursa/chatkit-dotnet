---
id: TASK-0006
type: task
status: done
priority: medium
owner: null
created: 2026-03-18
updated: 2026-03-18
tags: [parity, aspnetcore, chatkit-js, composer]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md, specs/libraries/chatkit-core.md]
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

# TASK-0006 - Add composer tools, models, and dictation parity to the ASP.NET Core wrapper

## Summary

The upstream ChatKit composer supports tool selection, model selection, and dictation, but the ASP.NET Core wrapper currently exposes only the composer placeholder.

## Upstream reference

Tool picker example from upstream docs:

```ts
const chatkit = useChatKit({
  composer: {
    tools: [
      {
        id: "summarize",
        icon: "book-open",
        label: "Summarize",
        placeholderOverride: "Summarize the current page or document.",
      },
    ],
  },
});
```

Model picker example from upstream docs:

```ts
const chatkit = useChatKit({
  composer: {
    models: [
      { id: "gpt-4.1-mini", label: "Fast", description: "Answers right away" },
      { id: "gpt-4.1", label: "Quality", description: "All rounder", default: true },
    ],
  },
});
```

Dictation example from upstream docs:

```ts
const chatkit = useChatKit({
  composer: {
    dictation: {
      enabled: true,
    },
  },
});
```

Source material:

- `docs/guides/let-users-pick-tools-and-models.md`
- `docs/guides/accept-rich-user-input.md`
- `@openai/chatkit` type surface: `ComposerOption.tools`
- `@openai/chatkit` type surface: `ComposerOption.models`
- `@openai/chatkit` type surface: `ComposerOption.dictation`

## Current .NET gap

The wrapper supports:

- `composer.placeholder`

The wrapper does not support:

- `composer.tools`
- `composer.models`
- `composer.dictation`

## Implementation notes

- Add .NET models for tool picker entries and model picker entries.
- Ensure server-side docs mention how selected tool/model values flow into `inference_options`.
- Add a simple dictation enable flag and document the `ChatKitServer.TranscribeAsync(...)` prerequisite.
- Extend tests to verify the emitted client config shape.

## Acceptance criteria

- The ASP.NET Core wrapper can serialize tool-picker options into the browser config.
- The ASP.NET Core wrapper can serialize model-picker options into the browser config.
- The ASP.NET Core wrapper can enable dictation in the packaged UI.
- Docs explain the server-side prerequisites for tool/model handling and dictation.
- Tests cover all three new composer feature groups.
