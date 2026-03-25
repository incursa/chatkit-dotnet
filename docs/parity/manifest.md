---
workbench:
  type: doc
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit
    - src/Incursa.OpenAI.ChatKit.AspNetCore
  pathHistory: []
  path: /docs/parity/manifest.md
---

# Included Parity Manifest

This repo tracks the translated server-side ChatKit surface from `openai/chatkit-python`. It does not attempt to absorb the entire upstream ecosystem.

## Included .NET surface

- [`Incursa.OpenAI.ChatKit`](../../src/Incursa.OpenAI.ChatKit/README.md)
  - ChatKit request envelopes
  - thread, item, workflow, attachment, and widget models
  - [`ChatKitServer<TContext>`](../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - store abstractions and in-memory store
  - widget diff and streaming helpers
  - agent interop helpers layered on `Incursa.OpenAI.Agents`
- [`Incursa.OpenAI.ChatKit.AspNetCore`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md)
  - `/chatkit` endpoint mapping
  - JSON and SSE response handling

## Excluded surface

- upstream client-only concerns
- non-ChatKit agent orchestration features
- any ChatKit functionality not yet represented in the maintained tests and sample

## Upstream docs to mirror

- quickstart and server setup guidance
- thread and item lifecycle behavior
- widget and streaming behavior
- custom action and attachment flows

## Upstream test families to mirror

- request routing and wire-shape tests
- thread lifecycle and item emission tests
- widget diff/streaming tests
- attachment and transcription tests
- hosted endpoint integration tests

## Current mapping

- core request and event serialization:
  - status: `covered`
  - [`src/Incursa.OpenAI.ChatKit/ChatKitJson.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitJson.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)
- server routing, store behavior, and widget updates:
  - status: `covered`
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../src/Incursa.OpenAI.ChatKit/Store.cs)
  - [`src/Incursa.OpenAI.ChatKit/Widgets.cs`](../../src/Incursa.OpenAI.ChatKit/Widgets.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)
- ASP.NET Core endpoint integration:
  - status: `covered`
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`](../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs)
- quickstart sample:
  - status: `covered`
  - [`samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs`](../../samples/Incursa.OpenAI.ChatKit.QuickstartSample/Program.cs)

## Translation rule

For the included surface, prefer:

- exact ChatKit wire compatibility
- minimal diffs tied directly to upstream behavior changes
- tests and docs updated alongside runtime changes
- keeping agent-runtime concerns in the agents repo unless ChatKit must surface them
