---
id: TASK-0005
type: task
status: draft
priority: high
owner: null
created: 2026-03-18
updated: null
tags: [parity, aspnetcore, chatkit-js, attachments]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md, specs/libraries/chatkit-core.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/ChatKitHostClientConfig.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.tsx
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0005 - Add ChatKit composer attachment parity to the ASP.NET Core wrapper

## Summary

The core .NET library already understands `attachments.create`, `attachments.delete`, attachment stores, and `input.transcribe`, but the ASP.NET Core wrapper does not expose the upstream ChatKit.js attachment client configuration needed to turn the feature on in the packaged UI.

## Upstream reference

Example client setup from upstream docs:

```ts
const chatkit = useChatKit({
  api: {
    url: "/chatkit",
    domainKey: "local-dev",
    uploadStrategy: {
      type: "direct",
      uploadUrl: "/files",
    },
  },
  composer: {
    attachments: {
      enabled: true,
      maxCount: 10,
    },
  },
});
```

Alternative upstream upload strategy:

```ts
api: {
  url: "/chatkit",
  domainKey: "local-dev",
  uploadStrategy: {
    type: "two_phase",
  },
}
```

Source material:

- `docs/guides/accept-rich-user-input.md`
- `@openai/chatkit` type surface: `ComposerOption.attachments`
- `@openai/chatkit` type surface: `CustomApiConfig.uploadStrategy`

## Current .NET gap

There is no wrapper surface for:

- `composer.attachments`
- `api.uploadStrategy`

Without those settings, the packaged UI cannot enable attachment uploads even though the core .NET server surface already supports the underlying protocol.

## Implementation notes

- Add .NET config models for composer attachment limits and accepted file types.
- Add .NET config models for direct and two-phase upload strategies.
- Decide how those models are represented in Razor attributes vs app-level options.
- Document the server-side prerequisites clearly:
  - `AttachmentStore<TContext>`
  - upload endpoint for direct uploads, or attachment creation flow for two-phase uploads
  - authorization guidance for attachment access
- Add coverage for both direct and two-phase config serialization.

## Acceptance criteria

- The ASP.NET Core wrapper can enable `composer.attachments`.
- The packaged runtime can forward `api.uploadStrategy` for custom API mode.
- Direct upload and two-phase upload shapes are both representable in .NET config.
- Docs show an ASP.NET Core example for at least one upload strategy.
- Tests cover serialization for attachment and upload strategy configuration.
