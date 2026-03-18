---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
  pathHistory: []
  path: /docs/20-architecture/README.md
---

# Architecture

The repo is split into two maintained packages:

- `Incursa.OpenAI.ChatKit`
  - owns the ChatKit object model, request deserialization, store contracts, request routing, widget helpers, and agent interop adapters
- `Incursa.OpenAI.ChatKit.AspNetCore`
  - owns HTTP binding, `/chatkit` endpoint mapping, JSON response writing, and streamed SSE response emission

## Request flow

1. ASP.NET Core receives a `POST` to `/chatkit`.
2. The endpoint forwards the raw payload to `ChatKitServer<TContext>.ProcessAsync`.
3. The server deserializes the request, dispatches to the appropriate ChatKit operation, and uses the configured store.
4. The process result is returned either as:
   - a JSON payload for non-streaming operations
   - an async byte stream for streaming thread operations

## Translation rule

When upstream Python behavior changes:

- preserve the ChatKit wire contract first
- prefer additive helpers over structural refactors
- update tests and parity docs together
