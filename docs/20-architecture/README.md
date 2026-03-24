---
workbench:
  type: guide
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs
    - src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs
  pathHistory: []
  path: /docs/20-architecture/README.md
---

# Architecture

The repo is intentionally split into a protocol/runtime package and an ASP.NET Core adapter package.

## Package boundaries

### `Incursa.OpenAI.ChatKit`

This package owns:

- protocol envelopes and JSON serialization
- thread, item, attachment, widget, and workflow object models
- `ChatKitServer<TContext>` request routing
- streaming event production and persistence rules
- store abstractions and the in-memory store
- widget diff helpers and exported widget loading
- lightweight agent interop helpers

This package does not own HTTP, Razor, or browser asset delivery.

### `Incursa.OpenAI.ChatKit.AspNetCore`

This package owns:

- `MapChatKit<TServer, TContext>(...)` endpoint mapping
- writing `application/json` responses for non-streaming operations
- writing `text/event-stream` responses for streaming operations
- DI registration for browser host defaults
- Razor tag helpers and packaged browser assets

This package does not implement assistant logic or persistence itself. It is a thin transport and UI-hosting layer over the core runtime.

## Core runtime lifecycle

### 1. Request enters the runtime

The core entry point is `ChatKitServer<TContext>.ProcessAsync(...)`.

It:

1. deserializes a UTF-8 JSON request into a `ChatKitRequest`
2. classifies the request as streaming or non-streaming
3. routes it to the matching operation
4. returns either:
   - `NonStreamingResult` with a JSON payload
   - `StreamingResult` with SSE-ready byte chunks

### 2. Request type decides transport behavior

Streaming requests are currently:

- `threads.create`
- `threads.add_user_message`
- `threads.add_client_tool_output`
- `threads.retry_after_item`
- `threads.custom_action`

Everything else is processed synchronously and returned as JSON.

### 3. Store-backed state is loaded or mutated

`ChatKitServer<TContext>` is stateful only through `ChatKitStore<TContext>` and the optional `AttachmentStore<TContext>`.

The store abstraction is responsible for:

- generating and loading thread metadata
- loading and saving thread items
- saving and deleting attachments
- thread and item pagination

The server base class handles orchestration, but it expects persistence semantics from the store implementation.

### 4. Application code emits assistant behavior

The application-specific implementation point is `RespondAsync(...)`.

Your server subclass is expected to:

- inspect the current thread and optional new user message
- run model or business logic
- emit `ThreadStreamEvent` values in ChatKit order

Optional extension points include:

- `AddFeedbackAsync(...)`
- `TranscribeAsync(...)`
- `ActionAsync(...)`
- `SyncActionAsync(...)`
- `GetStreamOptions(...)`
- `HandleStreamCancelledAsync(...)`

## Streaming event model

The runtime distinguishes between:

- `ThreadStreamEvent`
  - top-level SSE events such as thread creation, item updates, progress updates, notices, and errors
- `ThreadItemUpdate`
  - nested incremental updates for message parts, widgets, workflows, and generated images

Important persistence rules in `ProcessEventsAsync(...)`:

- `ThreadItemAddedEvent`
  - marks an item as pending in memory only
- `ThreadItemDoneEvent`
  - persists the item to the store
- `ThreadItemRemovedEvent`
  - deletes the item from the store
- `ThreadItemReplacedEvent`
  - overwrites the stored item
- hidden context items are persisted, but their `thread.item.done` events are not forwarded to the client

## Cancellation behavior

The runtime has explicit cancellation cleanup.

If a stream is cancelled:

- partially emitted assistant message items are persisted only if they are not empty
- the runtime appends an `SdkHiddenContextItem` telling later turns that the user cancelled the previous response

That behavior matters if downstream assistant logic replays conversation history.

## Item visibility model

Two item types are treated as internal-only:

- `HiddenContextItem`
- `SdkHiddenContextItem`

They are filtered out when:

- loading items for `items.list`
- returning a thread from `threads.get_by_id`
- returning a paged thread list

This means the persisted record can be richer than the client-visible history.

## Retry model

`threads.retry_after_item` walks backward through the thread, deletes all later items, then replays `RespondAsync(...)` starting from the retained user message.

Operationally, this means:

- retry is destructive for all later items in the thread
- the target item must be a `UserMessageItem`
- the replay behavior depends entirely on the current server implementation and store state

## Attachment model

Attachments are split across two abstractions:

- `ChatKitStore<TContext>`
  - keeps attachment records available for later lookup
- `AttachmentStore<TContext>`
  - handles the create/delete workflow for the external attachment system

The base server only enables `attachments.create` and `attachments.delete` when an attachment store is configured.

## ASP.NET Core request flow

`MapChatKit<TServer, TContext>(...)` is intentionally thin:

1. reads the entire POST body into memory
2. creates a per-request context via `contextFactory`
3. calls `server.ProcessAsync(...)`
4. writes either JSON or SSE to the response body

There is no built-in:

- authentication
- authorization
- model binding beyond raw body forwarding
- custom error translation beyond whatever the server returns or throws

## Razor-hosted browser flow

The Razor integration has three pieces:

1. `AddOpenAIChatKit*` service registration
   - stores shared host defaults in `ChatKitAspNetCoreOptions`
2. `<incursa-chatkit-assets>`
   - emits CSS, the upstream CDN component script, and the local bootstrap module
3. `<incursa-chatkit-api>` or `<incursa-chatkit-hosted>`
   - emits a host `<div>` with serialized JSON config in `data-incursa-chatkit-config`

The generic `<incursa-chatkit>` tag helper intentionally fails at render time to force an explicit hosting mode choice.

## Architectural constraints worth documenting

- upstream Python behavior is the source of truth for protocol behavior
- the repo keeps transport concerns separate from core orchestration
- the runtime favors small hand-written mapping code over heavy framework abstraction
- some option surfaces are broader than the tests currently exercise, so tests and docs are part of the conformance boundary

## Related notes

- [File-backed widget rendering](widget-file-backed-widgets.md)
