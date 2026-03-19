---
id: TASK-0003
type: task
status: draft
priority: high
owner: null
created: 2026-03-18
updated: null
tags: [parity, aspnetcore, chatkit-js, entities]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md, specs/libraries/chatkit-core.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/ChatKitHostClientConfig.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.tsx
    - src/Incursa.OpenAI.ChatKit/ChatKitPrimitives.cs
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0003 - Add ChatKit `entities` parity to the ASP.NET Core wrapper

## Summary

The upstream ChatKit client supports entity tagging, click handlers, and preview callbacks, but the ASP.NET Core wrapper has no way to configure or forward `entities`.

## Upstream reference

Example from upstream docs:

```ts
const chatkit = useChatKit({
  entities: {
    onTagSearch: async (query: string) => {
      return [
        {
          id: "article_123",
          title: "The Future of AI",
          group: "Trending",
          icon: "globe",
          interactive: true,
          data: { type: "article" }
        },
      ];
    },
    showComposerMenu: true,
    onClick: (entity) => navigateToEntity(entity.id),
    onRequestPreview: async (entity) => ({ preview: buildPreview(entity) }),
  },
});
```

Source material:

- `docs/guides/accept-rich-user-input.md`
- `docs/concepts/entities.md`
- `docs/guides/add-annotations.md`
- `@openai/chatkit` type surface: `ChatKitOptions.entities`

## Current .NET gap

There is no way to configure:

- `entities.onTagSearch`
- `entities.showComposerMenu`
- `entities.onClick`
- `entities.onRequestPreview`

That blocks upstream @-mention flows and interactive entity previews for users of the packaged ASP.NET Core UI.

## Implementation notes

- Design a Razor-friendly callback registration model similar to client tools.
- Expose `showComposerMenu` and other serializable fields directly in .NET options.
- Support browser callbacks for `onTagSearch`, `onClick`, and `onRequestPreview`.
- Validate the expected shape of entity objects and preview payloads.
- Add docs showing both the client setup and the server-side tag conversion path.

## Acceptance criteria

- The ASP.NET Core wrapper can configure `entities.showComposerMenu`.
- Host pages can register `onTagSearch`, `onClick`, and `onRequestPreview`.
- The packaged runtime forwards the `entities` object to `useChatKit(...)`.
- A documented sample shows end-to-end entity tagging setup.
- Tests cover config serialization and callback registration behavior.
