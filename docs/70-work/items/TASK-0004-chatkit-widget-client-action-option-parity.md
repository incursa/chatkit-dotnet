---
id: TASK-0004
type: task
status: completed
priority: high
owner: null
created: 2026-03-18
updated: 2026-03-18
tags: [parity, aspnetcore, chatkit-js, widgets]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md, specs/libraries/chatkit-core.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/ChatKitHostClientConfig.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.tsx
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0004 - Add full ChatKit `widgets` option parity to the ASP.NET Core wrapper

## Summary

The ASP.NET Core wrapper currently supports only a local forwarding toggle for widget actions. It does not expose the upstream `widgets.onAction` callback model used for client-handled widget actions.

## Upstream reference

Example from upstream docs:

```ts
const chatkit = useChatKit({
  widgets: {
    onAction: async (action, widgetItem) => {
      if (action.type === "save_profile") {
        const result = await saveProfile(action.payload);
        await chatkit.sendCustomAction(
          {
            type: "save_profile_complete",
            payload: { ...result, user_id: action.payload.user_id },
          },
          widgetItem.id,
        );
      }
    },
  },
});
```

Source material:

- `docs/guides/build-interactive-responses-with-widgets.md`
- `docs/concepts/actions.md`
- `@openai/chatkit` type surface: `ChatKitOptions.widgets`

## Current .NET gap

The wrapper exposes `widgetActions.forwardToEndpoint`, which is a local .NET convenience, but not the upstream `widgets.onAction` browser callback surface.

That means:

- client-handled widget actions cannot be registered through the packaged UI
- hosted API mode cannot use `widgets.onAction`
- the current wrapper surface diverges from upstream widget action guidance

## Implementation notes

- Preserve the current local forwarding helper, but add first-class `widgets.onAction` support.
- Decide how the wrapper resolves client callbacks from Razor pages.
- Make sure local endpoint forwarding and client callbacks can coexist without ambiguous precedence.
- Document the supported patterns for server-only, client-only, and client-then-server widget actions.

## Acceptance criteria

- The packaged runtime can pass `widgets.onAction` into `useChatKit(...)`.
- Razor integrations can register a client widget action handler without replacing `chatkit.js`.
- The local `action-endpoint` forwarding path keeps working for existing integrations.
- Precedence and coexistence rules are documented and tested.
- Hosted API mode supports client-handled widget actions.
