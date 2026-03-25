---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
  pathHistory: []
  path: /docs/00-overview/README.md
---

# Overview

`chatkit-dotnet` is the maintained .NET translation of the server-side ChatKit surface from `openai/chatkit-python`.

The repository currently centers on two production packages:

- [`Incursa.OpenAI.ChatKit`](../../src/Incursa.OpenAI.ChatKit/README.md)
  - the protocol model, request router, stream event model, store abstractions, widget helpers, and agent interop helpers
- [`Incursa.OpenAI.ChatKit.AspNetCore`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md)
  - the ASP.NET Core adapter for HTTP POST handling, SSE response writing, Razor tag helpers, and packaged browser assets

There is also a placeholder `Incursa.OpenAI.Jinja` project in the tree, but it is not part of the main ChatKit runtime described in these docs.

## What this repo is trying to preserve

- upstream ChatKit request and event wire compatibility
- a small, reviewable .NET translation layer instead of a broad redesign
- a clear split between core runtime concerns and ASP.NET Core hosting concerns
- enough tests to lock the public surface and critical protocol behavior

## Main entry points

If you are trying to understand the codebase quickly, start here:

1. [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
   - central request dispatcher and streaming/non-streaming split
2. [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../src/Incursa.OpenAI.ChatKit/Store.cs)
   - persistence contract for threads, items, and attachments
3. [`src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs)
   - supported request types and their parameter envelopes
4. [`src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs`](../../src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs)
   - stream event types produced during assistant responses
5. [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)
   - HTTP adapter that maps raw JSON to the core runtime
6. [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers)
   - Razor-hosted browser integration and host config serialization

## Documentation map

- [`../quickstart.md`](../quickstart.md)
  - build, test, and run the sample
- [`../20-architecture/README.md`](../20-architecture/README.md)
  - package boundaries, request lifecycle, persistence model, and streaming behavior
- [`../30-contracts/README.md`](../30-contracts/README.md)
  - supported request operations, response shapes, UI contracts, and operational notes
- [`../30-contracts/chatkit-tag-helper.md`](../30-contracts/chatkit-tag-helper.md)
  - explicit tag helper behavior and browser host configuration rules
- [`../extensions.md`](../extensions.md)
  - ASP.NET Core integration details and hosting mode guidance

## Current scope boundary

The maintained surface in this repository is server-side ChatKit plus its ASP.NET Core wrapper. The repo is not trying to be:

- a full replacement for upstream frontend implementation work
- a general OpenAI agents SDK
- a persistence product with a built-in production database provider
- a full-stack application template with authentication, authorization, and session issuance already solved

## What to watch when auditing behavior

The most useful places to look for gaps are the handoff points:

- request type supported by [`ChatKitRequest`](../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs), but not yet exercised end to end
- stream events emitted by `RespondAsync`, but not persisted or filtered as expected
- option values accepted by Razor tag helpers, but constrained more tightly than the service configuration suggests
- upstream ChatKit changes that affect wire shape, not just implementation detail
