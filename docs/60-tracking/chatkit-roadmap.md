---
workbench:
  type: doc
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit
    - src/Incursa.OpenAI.ChatKit.AspNetCore
    - tools/upstream-sync
  pathHistory: []
  path: /docs/60-tracking/chatkit-roadmap.md
---

# ChatKit Translation Roadmap

This roadmap captures the current translation state after comparing `chatkit-dotnet` with upstream `openai/chatkit-js`.

## Current Status

- The tracked parity state now bootstraps against `openai/chatkit-js` rather than the older Python-oriented watcher metadata.
- A bootstrap review through upstream `chatkit-js` commit `d333da9c45f13511f32e557fce5b921469a69775` confirmed that the included .NET host surface still matches the supported `ChatKitOptions` projection.
- Recent upstream widget-contract deltas for `Widgets.BasicRoot` thread items, `Table` / `Table.Row` / `Table.Cell`, and `Card.border` are already representable through the existing generic .NET widget model and are now covered by explicit regression tests.
- The upstream DOM-event and imperative browser APIs remain outside the current canonical ASP.NET Core host surface and continue to be tracked as requirement gaps rather than silent parity claims.

## Near-Term Maintenance

- Keep [`tools/upstream-sync`](../../tools/upstream-sync/README.md) pointed at the upstream `chatkit-js` repository and treat each upstream commit as a translation candidate.
- For any upstream changes under `packages/chatkit/types/`, `packages/chatkit-react/src/`, or `packages/docs/src/content/docs/`, decide whether the included .NET surface changes.
- When the included surface changes, create a focused item under [`docs/70-work/items/`](../70-work/items) with:
  - the upstream reference
  - the current .NET gap
  - the acceptance criteria
  - the specific files that need to move

## Watchlist

- New client-visible fields on thread metadata, request envelopes, or stream events.
- Attachment and transcription behavior changes.
- Any ASP.NET Core wrapper option that changes the serialized `ChatKitOptions` shape or runtime host contract.
- New upstream docs or tests that affect the included surface and should be mirrored in .NET.

## Outcome

- No additional large, untracked parity gap was identified in this pass.
- Future upstream deltas should continue to become discrete work items rather than broad refactors.
