# SPEC-CHATKIT-ASPNETCORE-BROWSER - Incursa.OpenAI.ChatKit.AspNetCore Browser Runtime Host Mirror

## Purpose

Define the canonical browser-runtime requirements for the packaged ChatKit bootstrap that turns a Razor-rendered host `div` into a supported mirror of the upstream `<openai-chatkit>` web component.

## Scope

This specification covers the handwritten browser runtime under [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/), including ChatKit option projection, browser callback resolution, host control and event mirroring, multi-host mounting, and visible runtime failure behavior. It does not redefine tag-helper serialization rules or endpoint behavior, which remain owned by [`SPEC-CHATKIT-ASPNETCORE`](SPEC-CHATKIT-ASPNETCORE.md).

## Context

The ASP.NET Core package renders config-only host elements from Razor, then relies on a small repo-managed browser runtime to create and configure the upstream ChatKit custom element. That runtime is the browser parity boundary for this package: if it only passes `setOptions(...)` through and hides the wrapped element's control and event surface, the .NET wrapper drifts from the upstream `chatkit-js` contract even when its serialized config is correct.

## Runtime Surface Inventory

- [`runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js) parses the serialized host config, creates the inner `<openai-chatkit>` element, and bridges host behavior.
- [`clientToolHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js), [`entityHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js), and [`widgetActionHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js) resolve browser registries and validate callback payloads.
- [`entry.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js) mounts every Razor-rendered host on page load.
- [`wwwroot/chatkit/chatkit.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/wwwroot/chatkit/chatkit.js) packages the same runtime behavior for consumers.

## Upstream Source Lineage

- The browser runtime was reviewed against the upstream `chatkit-js` web-component and React wrapper contracts, primarily `packages/chatkit/types/index.d.ts`, `packages/chatkit-react/src/ChatKit.tsx`, `packages/chatkit-react/src/useChatKit.ts`, and `packages/docs/src/content/docs/quick-reference/use-chatkit.mdx`.
- The local runtime does not re-create the upstream UI. It preserves parity by configuring the upstream element directly and by mirroring its supported control and event surface on the outer Razor host.

## REQ-CHATKIT-ASPNETCORE-BROWSER-0001 Translate rendered host config into supported ChatKit browser options

The packaged browser runtime MUST parse each rendered `data-incursa-chatkit-config` payload, create one inner `<openai-chatkit>` element, translate the supported hosted or direct-API settings into `ChatKitOptions`, and apply those options immediately when the custom element is already defined or after `customElements.whenDefined("openai-chatkit")` resolves when it is not.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-BROWSER-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-BROWSER-0001`
- Source Refs:
  - [`runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
  - [`entry.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs)
  - Upstream: `packages/chatkit/types/index.d.ts`

## REQ-CHATKIT-ASPNETCORE-BROWSER-0002 Resolve browser callback registries explicitly and preserve validated client-side behavior

The packaged browser runtime MUST resolve dotted browser lookup paths against the page global for client-tool, entity, header-action, and widget-action callbacks, MUST throw clear runtime errors when configured handlers are missing or invalid, MUST validate entity search and preview payloads before passing them to ChatKit, and MUST invoke client widget handling before optional endpoint forwarding when both are configured.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-BROWSER-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-BROWSER-0001`
- Source Refs:
  - [`runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
  - [`clientToolHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js)
  - [`entityHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js)
  - [`widgetActionHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js)
  - [`clientToolHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.test.mjs)
  - [`entityHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.test.mjs)
  - [`widgetActionHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.test.mjs)
  - [`entry.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs)
  - Upstream: `packages/chatkit/types/index.d.ts`

## REQ-CHATKIT-ASPNETCORE-BROWSER-0003 Mirror the upstream web-component control and event surface on the rendered Razor host

After mounting the inner `<openai-chatkit>` element, the packaged browser runtime MUST expose `setOptions(...)`, `focusComposer()`, `setThreadId(...)`, `sendUserMessage(...)`, `setComposerValue(...)`, `fetchUpdates()`, `sendCustomAction(...)`, `showHistory()`, and `hideHistory()` on the rendered host element, and it MUST re-dispatch the upstream `chatkit.ready`, `chatkit.error`, `chatkit.effect`, `chatkit.deeplink`, `chatkit.response.start`, `chatkit.response.end`, `chatkit.thread.change`, `chatkit.thread.load.start`, `chatkit.thread.load.end`, `chatkit.tool.change`, and `chatkit.log` events from the inner element onto that outer host.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-BROWSER-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-BROWSER-0001`
- Source Refs:
  - [`runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
  - [`entry.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs)
  - Upstream: `packages/chatkit/types/index.d.ts`
  - Upstream: `packages/chatkit-react/src/ChatKit.tsx`
  - Upstream: `packages/chatkit-react/src/useChatKit.ts`
  - Upstream: `packages/docs/src/content/docs/quick-reference/use-chatkit.mdx`

## REQ-CHATKIT-ASPNETCORE-BROWSER-0004 Mount each rendered host once and surface runtime initialization failures visibly

The packaged browser runtime MUST discover every `data-incursa-chatkit-host` element on the page, MUST skip hosts already marked as mounted, MUST mark successful mounts so later scans do not double-initialize them, and MUST render visible `ChatKit error: ...` text inside the host when mounting fails instead of failing silently.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-BROWSER-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-BROWSER-0001`
- Source Refs:
  - [`runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
  - [`entry.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js)
  - [`entry.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs)

## Scope Exclusions

This specification does not redefine endpoint mapping, DI registration, tag-helper render-time validation, or browser-config precedence. Those behaviors remain owned by [`SPEC-CHATKIT-ASPNETCORE`](SPEC-CHATKIT-ASPNETCORE.md). It also does not define the upstream React hook API as a .NET surface; it only governs the packaged browser runtime that preserves the web-component control and event contract on the rendered Razor host.
