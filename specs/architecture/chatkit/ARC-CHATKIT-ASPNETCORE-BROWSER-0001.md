# ARC-CHATKIT-ASPNETCORE-BROWSER-0001 - ChatKit ASP.NET Core Browser Runtime Mirror Architecture

## Purpose

Describe how the packaged browser runtime preserves upstream web-component parity while keeping the ASP.NET Core package layered around a config-only Razor host.

## Satisfies

- `REQ-CHATKIT-ASPNETCORE-BROWSER-0001`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0002`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0003`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0004`

## Design Summary

The browser runtime is the seam between the server-rendered host `div` and the upstream `<openai-chatkit>` custom element. At mount time it parses serialized config, creates one inner ChatKit element, waits for the custom element definition when necessary, and applies translated `ChatKitOptions` without moving option-precedence or endpoint concerns into the browser.

The outer Razor host remains the stable DOM anchor for MVC and Razor apps, but the runtime mirrors the upstream browser control and event surface by proxying imperative methods and re-dispatching `chatkit.*` events on that outer host. This lets page code treat the rendered host as the integration point without pretending the ASP.NET Core package has its own second UI runtime.

Callback registries remain explicit dotted lookups against `window`, with payload validation concentrated in the small helper modules for client tools, entities, and widget actions. Mounting stays idempotent through `data-incursa-chatkit-mounted`, and failures are surfaced visibly inside the host so broken pages do not fail silently.

## Key Components

- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/wwwroot/chatkit/chatkit.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/wwwroot/chatkit/chatkit.js)

## Constraints

- The runtime must preserve the server-rendered host `div` contract instead of replacing it with a different public DOM shape.
- Callback registries must stay explicit and fail loudly when configuration points at missing or invalid browser handlers.
- The outer host must preserve the upstream web-component control and event surface instead of swallowing it behind the wrapper `div`.
- Mounting must tolerate `customElements.whenDefined(...)` timing without double-initializing hosts.
- Runtime failures must be visible in-page because many host apps will not have separate browser bootstrap diagnostics.

## Risks

- If the upstream `chatkit-js` event or method inventory changes and the local mirror does not follow, the outer host will drift from the supported browser contract.
- If callback lookup or payload validation weakens, the page can appear configured correctly while failing only after user interaction.
- If runtime mutation via mirrored methods is treated as equivalent to server-rendered precedence, consumers can conflate initial host config with later browser-driven control changes.
