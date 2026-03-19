---
id: TASK-0007
type: task
status: done
priority: low
owner: null
created: 2026-03-18
updated: 2026-03-18
tags: [parity, aspnetcore, chatkit-js, header]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/ChatKitHostClientConfig.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0007 - Add `header.title.enabled` parity to the ASP.NET Core wrapper

## Summary

The wrapper can configure header enabled state and header title text, but it cannot express the upstream `header.title.enabled` flag.

## Upstream reference

Published type surface:

```ts
header?: {
  enabled?: boolean;
  title?: {
    enabled?: boolean;
    text?: string;
  };
}
```

Representative usage inferred from the type surface:

```ts
const chatkit = useChatKit({
  header: {
    enabled: true,
    title: {
      enabled: false,
    },
  },
});
```

Source material:

- `@openai/chatkit` type surface: `HeaderOption.title.enabled`

## Current .NET gap

The wrapper exposes:

- `header-enabled`
- `header-title`

The wrapper does not expose:

- `header.title.enabled`

## Implementation notes

- Add a .NET config property for title enabled state.
- Decide whether the Razor attribute should be `header-title-enabled`.
- Update the host config model to include `header.title.enabled`.
- Add focused tag-helper tests for the new property.

## Acceptance criteria

- Site-wide options can configure `header.title.enabled`.
- The Razor tag helper can override `header.title.enabled` per instance.
- The packaged browser config includes `header.title.enabled` when set.
- Tests cover explicit true, explicit false, and omitted behavior.
