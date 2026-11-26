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

# ChatKit Translation Maintenance Policy

This document defines how `chatkit-dotnet` tracks and evaluates changes from upstream `openai/chatkit-js`.

## Supported boundary

- The parity process bootstraps from `openai/chatkit-js`, the behavioral source of truth for the translated surface.
- The .NET host covers the documented request, event, widget, and `ChatKitOptions` projection used by the ASP.NET Core package.
- Upstream DOM-event and imperative browser APIs are outside the canonical ASP.NET Core host surface unless a focused requirement and translation explicitly adopt them.
- Generic widget modeling does not imply support for every upstream browser API; consumer-facing support claims must map to documented .NET behavior and focused tests.

## Maintenance workflow

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

Upstream deltas become discrete work items rather than broad parity claims. Each item records the upstream reference, .NET impact, acceptance criteria, and affected files; one-time review outcomes belong in the work item or pull request rather than this policy.
