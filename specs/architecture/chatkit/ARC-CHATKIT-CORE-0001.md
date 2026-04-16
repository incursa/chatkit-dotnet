# ARC-CHATKIT-CORE-0001 - ChatKit Core Runtime Boundary Architecture

## Purpose

Describe how the core ChatKit package keeps translated request handling, persistence orchestration, widget behavior, and extension points behind a small public facade.

## Satisfies

- `REQ-CHATKIT-CORE-0001`
- `REQ-CHATKIT-CORE-0002`
- `REQ-CHATKIT-CORE-0003`
- `REQ-CHATKIT-CORE-0004`
- `REQ-CHATKIT-CORE-0005`
- `REQ-CHATKIT-CORE-0006`
- `REQ-CHATKIT-CORE-0007`
- `REQ-CHATKIT-CORE-0008`
- `REQ-CHATKIT-CORE-0009`
- `REQ-CHATKIT-CORE-0010`
- `REQ-CHATKIT-CORE-0011`
- `REQ-CHATKIT-CORE-0012`
- `REQ-CHATKIT-CORE-0013`
- `REQ-CHATKIT-CORE-0014`

## Design Summary

The core package centers all protocol entry through `ChatKitServer<TContext>`. `ProcessAsync(...)` converts incoming JSON into typed `ChatKitRequest` values, constrains processing to the approved request inventory, decides the transport mode, and routes into either synchronous JSON production or streaming event production.

Persistence is delegated to [`ChatKitStore<TContext>`](../../../src/Incursa.OpenAI.ChatKit/Store.cs) and [`AttachmentStore<TContext>`](../../../src/Incursa.OpenAI.ChatKit/Store.cs), so the base server owns orchestration, hidden-context visibility rules, attachment materialization, client-tool continuation, and destructive retry behavior without owning storage technology. Streaming events are processed through a small reducer that tracks pending items, persists completed state transitions, swallows hidden-context completion events, and records cancellation follow-up in the store.

Widget behavior is split between runtime diff helpers and exported widget-definition loading and rendering. Assistant behavior and external integrations stay behind explicit virtual methods so the base server remains a framework surface rather than an application implementation.

## Key Components

- [`src/Incursa.OpenAI.ChatKit/ChatKitServer.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitServer.cs)
- [`src/Incursa.OpenAI.ChatKit/Store.cs`](../../../src/Incursa.OpenAI.ChatKit/Store.cs)
- [`src/Incursa.OpenAI.ChatKit/ChatKitJson.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitJson.cs)
- [`src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitRequests.cs)
- [`src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs`](../../../src/Incursa.OpenAI.ChatKit/ChatKitEvents.cs)
- [`src/Incursa.OpenAI.ChatKit/Widgets.cs`](../../../src/Incursa.OpenAI.ChatKit/Widgets.cs)
- [`src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs`](../../../src/Incursa.OpenAI.ChatKit/WidgetDefinitions.cs)
- [`src/Incursa.OpenAI.ChatKit/WidgetDefinitionRendering.cs`](../../../src/Incursa.OpenAI.ChatKit/WidgetDefinitionRendering.cs)

## Constraints

- The approved request inventory and the streaming versus non-streaming split are contractual and must not drift based on transport convenience.
- Hidden-context items are part of persisted conversation state but not part of client-visible history.
- Cancellation is stateful because partially emitted items and SDK hidden context affect later turns.
- `threads.add_client_tool_output` resumes only from the newest pending client tool call item.
- `threads.retry_after_item` is destructive for later items and therefore remains a high-risk path for future verification work.
- Attachment create and delete flows are only legal when an attachment store is configured.

## Risks

- If future changes bypass the store boundary, persistence semantics will fragment across server implementations.
- If hidden-context filtering changes in only one response path, server-visible and client-visible history will diverge in inconsistent ways.
- If client-tool continuation or destructive retry drift from the documented behavior, browser-triggered recovery flows will become non-deterministic.
- If more behavior is added to the base server without matching requirements, the package can drift back toward broad undocumented scenario coverage.
