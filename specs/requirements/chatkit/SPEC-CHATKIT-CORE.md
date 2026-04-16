# SPEC-CHATKIT-CORE - Incursa.OpenAI.ChatKit Core Runtime and Public Surface

## Purpose

Define the canonical public API, translated request model, persistence, streaming, widget, and extensibility requirements for the core ChatKit runtime package.

## Scope

This specification covers the consumer-facing API surface of `Incursa.OpenAI.ChatKit`, the translated ChatKit request inventory and discriminator shape, the streaming versus non-streaming transport split, store-backed thread, item, and attachment lifecycle behavior, hidden-context visibility rules, stream cancellation cleanup, client-tool continuation, destructive retry, explicit server extension points, and widget definition and widget diff behavior.

## Context

This repository is a .NET translation of the upstream ChatKit contract. The behavioral lineage for request handling, thread and message workflows, attachments, retry, feedback, and custom actions comes from the upstream `chatkit-js` browser contract and is translated into the core .NET runtime together with Incursa-owned widget and hosting extensions.

## Core Surface Inventory

- `ChatKitServer<TContext>` owns request orchestration, request-kind classification, result transport selection, streaming event persistence, client-tool continuation, and destructive retry orchestration.
- `ChatKitStore<TContext>` and `AttachmentStore<TContext>` define the persistence and external attachment boundaries.
- `ChatKitJson`, `ChatKitRequest`, `ThreadStreamEvent`, and related item and primitive models define the protocol payload surface.
- Widget definition and diff helpers are part of the public core package surface because downstream integrations load and render `.widget` assets directly.

## Upstream Source Lineage

- Upstream behavioral lineage was reviewed primarily against `chatkit-js/packages/chatkit/types/index.d.ts`.
- The upstream quickstart, customization, and quick-reference docs under `chatkit-js/packages/docs/src/content/docs/` were used to capture the browser-facing message, retry, feedback, attachment, custom-action, and initial-thread concepts that this repository translates into .NET requirements.

## REQ-CHATKIT-CORE-0001 Keep the public core surface bounded to the approved ChatKit facade

The library MUST expose the consumer-facing ChatKit protocol, persistence, event, widget, and server abstractions required by this specification, and it MUST treat the approved public API analyzer surface as the compatibility baseline for future changes rather than implicitly widening the package through accidental public types or members.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/PublicAPI.Shipped.txt`](../../../src/Incursa.OpenAI.ChatKit/PublicAPI.Shipped.txt)
  - [`src/Incursa.OpenAI.ChatKit/PublicAPI.Unshipped.txt`](../../../src/Incursa.OpenAI.ChatKit/PublicAPI.Unshipped.txt)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## REQ-CHATKIT-CORE-0002 Serialize ChatKit envelopes and discriminators with the translated protocol shape

The library MUST serialize and deserialize ChatKit requests, items, events, and related payloads using the translated ChatKit JSON shape, including the request-envelope `type` discriminator and the concrete payload forms required by the server entry point.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitJson.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitJson.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## REQ-CHATKIT-CORE-0003 Support the approved request inventory and reject unsupported request kinds

The runtime MUST accept the translated request inventory of `threads.get_by_id`, `threads.create`, `threads.list`, `threads.add_user_message`, `threads.add_client_tool_output`, `threads.custom_action`, `threads.sync_custom_action`, `threads.retry_after_item`, `items.feedback`, `attachments.delete`, `attachments.create`, `input.transcribe`, `items.list`, `threads.update`, and `threads.delete`, and it MUST reject request kinds outside that inventory instead of treating them as implicit no-ops.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0004 Classify request results by transport mode without mixing streaming and JSON flows

`ChatKitServer<TContext>.ProcessAsync(...)` MUST deserialize the incoming request, classify `threads.create`, `threads.add_user_message`, `threads.add_client_tool_output`, `threads.retry_after_item`, and `threads.custom_action` as streaming operations, classify the remaining supported requests as non-streaming operations, and return either a JSON payload or an SSE-ready byte sequence without fabricating the other transport shape for that operation.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0005 Keep thread, item, and attachment persistence behind the store boundary

The runtime MUST treat `ChatKitStore<TContext>` as the owning boundary for thread metadata, thread items, attachments, pagination, and persistence mutation operations, and it MUST route core lifecycle changes through that boundary instead of persisting state in ad hoc server-side caches.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../../src/Incursa.OpenAI.ChatKit/Store.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## REQ-CHATKIT-CORE-0006 Keep hidden-context items persisted but absent from client-visible history

The runtime MUST persist hidden-context items as part of server-visible conversation state, but it MUST exclude `HiddenContextItem` and `SdkHiddenContextItem` values from serialized thread and item responses returned to clients, including full-thread loads and item-list responses.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../../src/Incursa.OpenAI.ChatKit/Store.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0007 Persist streaming item mutations through the event reducer

When processing streaming events, the runtime MUST keep announced-but-incomplete items in memory, persist `ThreadItemDoneEvent` items through `AddThreadItemAsync(...)`, persist `ThreadItemReplacedEvent` items through `SaveItemAsync(...)`, persist `ThreadItemRemovedEvent` operations through `DeleteThreadItemAsync(...)`, and suppress hidden-context completion events from the client stream.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0008 Persist cancellation follow-up instead of silently discarding interrupted responses

When a streaming response is cancelled, the runtime MUST persist any non-empty pending assistant message items approved for survival and MUST append the SDK hidden-context cancellation marker that tells later turns the prior response was interrupted, rather than silently discarding all partial work.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0009 Resume client tool calls only from the latest pending tool item

For `threads.add_client_tool_output`, the runtime MUST load the newest thread item, require that item to be a pending `ClientToolCallItem`, write the provided result back to that item with completed status, persist the updated item, and resume assistant response generation from that continuation point.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0010 Retry-after-item must destructively replay from the selected user message

For `threads.retry_after_item`, the runtime MUST walk thread items in reverse order, require the selected item to resolve to a `UserMessageItem`, delete later items from the store before replay, and then regenerate the assistant turn from that user message rather than attempting a partial in-place patch of later conversation state.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0011 Gate attachment operations on explicit attachment-store support and persist them consistently

Attachment creation and deletion MUST require an `AttachmentStore<TContext>`, MUST persist created attachments through both the external attachment store and the ChatKit store boundary, MUST delete attachments from both boundaries on removal, and MUST persist message attachments before their owning user message is added to a thread so later server logic sees fully materialized conversation state.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../../src/Incursa.OpenAI.ChatKit/Store.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)

## REQ-CHATKIT-CORE-0012 Keep assistant behavior and external services behind explicit server extension points

The core runtime MUST own orchestration while leaving assistant response generation, feedback handling, transcription, custom actions, synchronous custom actions, stream options, cancellation follow-up, and external attachment operations behind the explicit `ChatKitServer<TContext>` and `AttachmentStore<TContext>` extension points instead of hard-coding repo-specific implementations into the base server.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
  - [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../../src/Incursa.OpenAI.ChatKit/Store.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## REQ-CHATKIT-CORE-0013 Emit incremental widget updates only when the widget contract allows it

The widget update pipeline MUST emit compact delta updates for compatible streaming text changes and MUST fall back to replacing the widget root when the before and after widget state cannot be represented safely as an incremental delta.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/Widgets.cs`](../../../src/Incursa.OpenAI.ChatKit/Widgets.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## REQ-CHATKIT-CORE-0014 Load and validate exported widget definitions before rendering them

The widget-definition pipeline MUST support loading exported `.widget` payloads from file or stream, validate the schema-bearing document before use, and build a `WidgetRoot` through the Jinja-backed rendering runtime only from accepted definition input.

Trace:
- Satisfied By: `ARC-CHATKIT-CORE-0001`
- Verified By: `VER-CHATKIT-CORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs`](../../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)
  - [`src/Incursa.OpenAI.ChatKit/WidgetDefinitionRendering.cs`](../../../src/Incursa.OpenAI.ChatKit/WidgetDefinitionRendering.cs)
  - [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)

## Scope Exclusions

HTTP transport, dependency-injection registration, Razor rendering, and browser asset delivery are not part of this specification. Those behaviors are owned by [`SPEC-CHATKIT-ASPNETCORE`](SPEC-CHATKIT-ASPNETCORE.md).
