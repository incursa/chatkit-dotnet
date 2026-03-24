---
workbench:
  type: reference
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs
    - src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs
    - src/Incursa.OpenAI.ChatKit/ChatKitPrimitives.cs
    - src/Incursa.OpenAI.ChatKit/ChatKitItems.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs
  pathHistory: []
  path: /docs/30-contracts/README.md
---

# Contracts

This section documents the main runtime contracts exposed by the translated ChatKit surface.

## Request operations

Supported request discriminators currently defined in `ChatKitRequest` are:

### Thread operations

- `threads.get_by_id`
  - load a single thread and its first page of visible items
- `threads.create`
  - create a new thread, add the initial user message, and stream the assistant turn
- `threads.list`
  - page thread metadata
- `threads.add_user_message`
  - append a user message and stream the assistant turn
- `threads.add_client_tool_output`
  - complete the most recent pending client tool call and continue the assistant turn
- `threads.custom_action`
  - invoke async widget action handling and stream events
- `threads.sync_custom_action`
  - invoke sync widget action handling and return JSON
- `threads.retry_after_item`
  - delete later items and replay after a specific user message
- `threads.update`
  - update the thread title
- `threads.delete`
  - delete the thread

### Item operations

- `items.feedback`
  - submit feedback for one or more item ids
- `items.list`
  - page visible items for a thread

### Attachment operations

- `attachments.create`
  - create an attachment via the configured attachment store
- `attachments.delete`
  - delete an attachment via the configured attachment store

### Input operations

- `input.transcribe`
  - send base64 audio to `TranscribeAsync(...)`

## Result contracts

### Non-streaming

Non-streaming operations return `NonStreamingResult` and produce `application/json` at the ASP.NET Core boundary.

Examples:

- `threads.get_by_id`
- `threads.list`
- `items.list`
- `threads.update`
- `threads.delete`
- `threads.sync_custom_action`

### Streaming

Streaming operations return `StreamingResult`, which is already encoded as SSE `data:` lines. The ASP.NET Core adapter writes them as `text/event-stream`.

The core runtime currently serializes each event as:

```text
data: {json-event}

```

There is no separate event name field today; the event discriminator lives in the JSON payload.

## Thread and item contracts

Important model groups in the core package:

- `ThreadMetadata` and `Thread`
  - thread identity, title, timestamps, status, metadata, and item page
- `ThreadItem`
  - base type for persisted conversation items
- `UserMessageItem`
- `AssistantMessageItem`
- `ClientToolCallItem`
- `WidgetItem`
- `GeneratedImageItem`
- `TaskItem`
- `WorkflowItem`
- `EndOfTurnItem`
- `HiddenContextItem`
- `SdkHiddenContextItem`

Operationally:

- hidden context items are persisted but filtered from client-facing responses
- assistant turns are not persisted just because they were emitted; they are persisted when a `ThreadItemDoneEvent` arrives

## Stream event contracts

Main top-level stream events:

- `ThreadCreatedEvent`
- `ThreadUpdatedEvent`
- `ThreadItemAddedEvent`
- `ThreadItemUpdatedEvent`
- `ThreadItemDoneEvent`
- `ThreadItemRemovedEvent`
- `ThreadItemReplacedEvent`
- `StreamOptionsEvent`
- `ProgressUpdateEvent`
- `ClientEffectEvent`
- `ErrorEvent`
- `NoticeEvent`

Nested update types used inside item updates:

- assistant content part add/delta/annotation/done
- widget streaming text delta
- widget root replacement
- widget component replacement
- workflow task add/update
- generated image update

## Persistence contracts

`ChatKitStore<TContext>` is the persistence boundary. A store implementation must support:

- thread id generation
- item id generation
- thread create/load/update/delete
- item create/load/update/delete
- thread and item pagination
- attachment save/load/delete

The built-in `InMemoryChatKitStore<TContext>` is suitable for tests and samples, not production durability.

## ASP.NET Core service contracts

Public ASP.NET Core registrations:

- `AddOpenAIChatKit(...)`
  - register shared browser host defaults
- `AddOpenAIChatKitHosted(...)`
  - configure hosted browser mode using local session/action endpoints
- `AddOpenAIChatKitApi(...)`
  - configure direct browser API mode using `apiUrl` and `domainKey`
- `MapChatKit<TServer, TContext>(...)`
  - map the POST transport for the core ChatKit server

## Browser host option contracts

The browser-facing options surface in `ChatKitAspNetCoreOptions` currently includes:

- endpoint selection
  - `ApiUrl`, `DomainKey`, `SessionEndpoint`, `ActionEndpoint`
- host defaults
  - `DefaultHeight`, `Locale`, `FrameTitle`, `InitialThread`
- callback lookup paths
  - `ClientToolHandlers`, `EntityHandlers`, `WidgetActionHandler`
- forwarding policy
  - `ForwardWidgetActions`
- theme
  - color scheme, typography, grayscale, accent, surface, radius, density
- header
  - enabled flag, title, left/right actions
- history
  - enabled, rename, delete
- start screen
  - greeting and prompts
- composer
  - placeholder, attachments, tools, models, dictation
- upload strategy
  - type and optional upload URL
- disclaimer
  - text and high-contrast flag
- entities
  - composer menu visibility
- thread item actions
  - feedback and retry flags

## Important operational notes

These behaviors are easy to miss when reading only the option types:

- `AddOpenAIChatKitApi(...)` requires a non-empty `domainKey` even though the method signature allows `string?`
- `<incursa-chatkit-api>` requires the `api-url` attribute at render time
- `<incursa-chatkit-hosted>` requires the `session-endpoint` attribute at render time
- hosted mode only emits `action-endpoint` when widget forwarding is enabled
- API mode ignores session and action endpoints entirely
- disclaimer config is omitted unless text is present
- header actions require both an icon and a callback lookup path
- upload strategies other than `direct` drop `uploadUrl`

Those rules are not just documentation choices; they are enforced by the current implementation and tests.

## Related notes

- [Widget file-backed rendering contract](widget-file-backed-widgets.md)
