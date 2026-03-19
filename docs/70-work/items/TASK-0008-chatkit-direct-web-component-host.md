---
id: TASK-0008
type: task
status: done
priority: medium
owner: null
created: 2026-03-18
updated: 2026-03-18
tags: [parity, aspnetcore, chatkit-js, web-components]
related:
  specs: [specs/libraries/chatkit-aspnetcore.md]
  adrs: []
  files:
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.tsx
    - src/Incursa.OpenAI.ChatKit.AspNetCore/README.md
    - tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs
  prs: []
  issues: []
  branches: []
---

# TASK-0008 - Replace the React bootstrap with a direct ChatKit web-component host

## Summary

The ASP.NET Core wrapper currently mounts ChatKit through a React runtime even though `chatkit-js` already exposes a first-class `<openai-chatkit>` web component with an imperative `setOptions(...)` API. For the Razor/tag-helper integration, React adds an extra layer without contributing meaningful state management.

## Problem statement

The current design increases moving parts in three places:

- the packaged browser runtime depends on React and `react-dom`
- the host boot path has to bridge Razor config into React props before it reaches ChatKit
- debugging wrapper issues requires separating app bugs from React-host bugs and ChatKit runtime bugs

For an ASP.NET Core tag helper, the simpler shape is:

- render the host element
- create `<openai-chatkit>`
- call `setOptions(...)`
- bind events directly

## Implementation notes

- Keep the existing server-side tag-helper surface unless a parity gap forces an API adjustment.
- Replace the current React entrypoint with a minimal vanilla JS bootstrap.
- Preserve self-hosted API and hosted deployment flows.
- Verify that event wiring and height/layout behavior still match the current wrapper contract.
- Revisit tests that currently assume a React-backed runtime and keep changes scoped to the wrapper entrypoint and config serialization path.

## Acceptance criteria

- The packaged client runtime no longer depends on React for the ChatKit host path.
- The wrapper still supports both hosted and self-hosted API configurations.
- Existing Razor tag-helper options continue to serialize into valid ChatKit options.
- Focused tests cover the direct web-component bootstrap path and any changed runtime assumptions.
