---
workbench:
  type: spec
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit
    - src/Incursa.OpenAI.ChatKit.AspNetCore
  pathHistory: []
  path: /docs/10-product/README.md
---

# Product

## Core concept

This repository provides a `.NET 10` ChatKit server implementation aligned to the upstream Python ChatKit library.

The maintained product surface is:

- core ChatKit request, item, event, widget, and attachment models
- thread and item routing via `ChatKitServer<TContext>`
- in-memory store abstractions for threads, items, and attachments
- ASP.NET Core hosting for the `/chatkit` endpoint
- agent interop helpers layered on top of `Incursa.OpenAI.Agents`

## Operational stance

`Incursa.OpenAI.ChatKit` is intentionally narrow:

- preserve ChatKit wire compatibility first
- keep .NET-facing APIs idiomatic where that does not change wire behavior
- keep agent orchestration features in `Incursa.OpenAI.Agents`
- keep repo governance, parity docs, and upstream sync automation in this repo so the translation remains maintainable

## Included surface

- thread request routing
- item, attachment, and transcription dispatch hooks
- widget streaming and diff helpers
- ASP.NET Core endpoint mapping and SSE emission
- sample and tests for the translated server-side slice

## Deferred surface

- any upstream ChatKit behavior not yet represented in `docs/parity/manifest.md`
- browser-only or client SDK concerns
- unrelated agents runtime features that already belong to the agents repo

The current inclusion boundary is tracked in:

- `docs/parity/manifest.md`
- `docs/parity/maintenance-checklist.md`
