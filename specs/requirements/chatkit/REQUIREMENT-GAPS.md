---
workbench:
  type: specification
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/
    - scripts/quality/validate-library-traceability.ps1
  pathHistory: []
  path: /specs/requirements/chatkit/REQUIREMENT-GAPS.md
---

# ChatKit Requirement Gaps

This ledger tracks open questions, uncovered requirement slices, and migration work for the ChatKit SpecTrace corpus.

## Open Gaps

- [`ChatKitServer<TContext>`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs) now has direct tests for request inventory, streaming classification, hidden-context visibility, cancellation cleanup, client-tool continuation, destructive retry, list/update/delete, feedback, transcription, and attachment materialization ordering, but attachment lookup failure semantics and attachment-store error propagation still deserve dedicated scenarios.
- The current canonical suite still does not define exact locale, theme-value, or icon-enum acceptance semantics for the .NET host surface. The package currently serializes caller-provided strings and relies on the browser runtime to honor or reject those values, so a later decision is needed on whether those constraints should become explicit .NET requirements or remain upstream browser concerns.
- The repository still has documented `REPO-*` scenarios in [`docs/testing/generated/stats.json`](../../../docs/testing/generated/stats.json) for upstream sync and release automation. Decide whether those scenarios will become a future `SPEC-CHATKIT-REPO` slice or remain repo-quality evidence outside canonical SpecTrace.
- The current quality tooling still centers on `LIB-*` scenario IDs. Until that tooling is updated, the repo has two traceability layers: canonical `REQ-*` requirements here and compatibility `LIB-*` mappings in [`../../libraries/library-conformance-matrix.md`](../../libraries/library-conformance-matrix.md).
- This repository still lacks canonical ChatKit `WI-*` work-item artifacts. Add them when a concrete delivery slice needs end-to-end traceability rather than backfilling synthetic work items.

## Closed Gaps

- `missing-canonical-chatkit-spec-suite` is closed. The repository now has canonical `SPEC-CHATKIT-CORE` and `SPEC-CHATKIT-ASPNETCORE` artifacts under [`specs/requirements/chatkit/`](README.md) instead of relying only on broad prose in [`specs/libraries/`](../../libraries/).
- `requirements-too-coarse-for-upstream-js-and-host-surface` is closed. The canonical suite now splits the ChatKit requirements into narrower request, lifecycle, hosting, normalization, and DOM-contract clauses instead of relying on a small number of broad requirements.
- `aspnetcore-browser-runtime-parity-slice` is closed. The repository now has [`SPEC-CHATKIT-ASPNETCORE-BROWSER`](SPEC-CHATKIT-ASPNETCORE-BROWSER.md), [`ARC-CHATKIT-ASPNETCORE-BROWSER-0001`](../../architecture/chatkit/ARC-CHATKIT-ASPNETCORE-BROWSER-0001.md), and [`VER-CHATKIT-ASPNETCORE-BROWSER-0001`](../../verification/chatkit/VER-CHATKIT-ASPNETCORE-BROWSER-0001.md) covering packaged browser-runtime callback resolution, mirrored host methods and `chatkit.*` events, mount behavior, and direct executable proof.

## How To Use

- Add a gap here before implementation when behavior changes but no owning `REQ-*` clause exists yet.
- Keep each note short, actionable, and tied to the owning `SPEC-*` slice.
- Close gaps here only after the owning requirement, architecture, and verification surfaces exist or the scope is intentionally de-scoped.
