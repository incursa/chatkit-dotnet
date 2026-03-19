---
id: TASK-0001
type: task
status: draft
priority: high
owner: null
created: 2026-03-18
updated: null
tags: [parity, aspnetcore, chatkit-js, client-tools]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md]
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

# TASK-0001 - Add ChatKit `onClientTool` parity to the ASP.NET Core wrapper

## Summary

The current ASP.NET Core wrapper does not expose ChatKit.js `onClientTool`, so browser-side client tools cannot be registered through the packaged Razor/runtime surface even though the underlying ChatKit packages support them.

## Upstream reference

`@openai/chatkit` exposes `onClientTool` on `ChatKitOptions`.

Example from upstream docs:

```ts
const chatkit = useChatKit({
  onClientTool: async ({name, params}) => {
    if (name === "get_selected_canvas_nodes") {
      const {project} = params;
      const nodes = myCanvas.getSelectedNodes(project);
      return {
        nodes: nodes.map((node) => ({id: node.id, kind: node.type})),
      };
    }
  },
});
```

Source material:

- `@openai/chatkit` type surface: `ChatKitOptions.onClientTool`
- `docs/guides/pass-extra-app-context-to-your-model.md`
- `docs/guides/update-client-during-response.md`

## Current .NET gap

The packaged config model only exposes a narrow subset of ChatKit.js options and has no way to bind a client tool callback into the browser runtime.

That means:

- the .NET tag helper cannot express the option
- the packaged runtime never passes `onClientTool` into `useChatKit(...)`
- teams using the Razor wrapper cannot use upstream client tool flows without replacing the packaged runtime entirely

## Implementation notes

- Decide the .NET-facing API shape for client tool callbacks.
- Prefer a host-page registration model that works in Razor without trying to serialize delegates through HTML attributes.
- Update the browser runtime so named callbacks can be resolved from the page and forwarded into `useChatKit(...)`.
- Document the contract for callback lookup, argument shape, and returned JSON payloads.
- Add tests for config serialization and browser callback wiring behavior.

## Acceptance criteria

- A Razor-based integration can register one or more client tool handlers without patching `chatkit.js`.
- The packaged runtime passes the resolved handler to `useChatKit({ onClientTool: ... })`.
- The callback receives `{ name, params }` and returns JSON-compatible data to ChatKit.
- Missing handlers fail with a clear browser error instead of silently no-oping.
- ASP.NET Core tests cover the new public surface and config path.
- README or sample documentation shows an end-to-end client tool example.
