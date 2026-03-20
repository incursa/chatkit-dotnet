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

This roadmap captures the current translation state after comparing `chatkit-dotnet` with upstream `openai/chatkit-python`.

## Current Status

- The included server surface is already represented in the .NET core package and parity manifest.
- The latest upstream delta checked in `chatkit-python` adds `allowed_image_domains` to client-visible `ThreadMetadata`, and the .NET model already exposes that field.
- The existing work-item set covers the known wrapper parity gaps and the direct web-component host cleanup path.

## Near-Term Maintenance

- Keep `tools/upstream-sync` pointed at the upstream `chatkit-python` repository and treat each upstream commit as a translation candidate.
- For any upstream changes under `chatkit/server.py`, `chatkit/types.py`, `chatkit/store.py`, `chatkit/widgets.py`, `chatkit/actions.py`, or `chatkit/agents.py`, decide whether the included .NET surface changes.
- When the included surface changes, create a focused item under `docs/70-work/items/` with:
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
