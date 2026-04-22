# VER-CHATKIT-ASPNETCORE-BROWSER-0001 - ChatKit ASP.NET Core Browser Runtime Verification

## Verification Method

Execution and inspection of the node-based packaged runtime tests plus the maintained browser-host contract documentation.

## Verifies

- `REQ-CHATKIT-ASPNETCORE-BROWSER-0001`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0002`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0003`
- `REQ-CHATKIT-ASPNETCORE-BROWSER-0004`

## Scope

This verification slice covers direct proof for packaged browser-runtime option projection, browser callback resolution and payload validation, host control and event mirroring, multi-host mount behavior, and visible runtime failure rendering. It intentionally complements rather than replaces the render-time host and endpoint proof in [`VER-CHATKIT-ASPNETCORE-0001`](VER-CHATKIT-ASPNETCORE-0001.md).

## Evidence

- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/runtimeHost.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.js)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/clientToolHandlers.test.mjs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entityHandlers.test.mjs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/widgetActionHandlers.test.mjs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/src/entry.test.mjs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/README.md`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md)
- [`docs/30-contracts/chatkit-tag-helper.md`](../../../docs/30-contracts/chatkit-tag-helper.md)

## Status Summary

The packaged runtime now has direct executable proof for config translation, callback resolution, widget-action ordering, mirrored imperative host methods, mirrored `chatkit.*` events, and deferred custom-element definition handling. The main remaining gap is full browser end-to-end proof against the real upstream custom element in a live page; this verification slice still relies on the focused node harness rather than a browser automation layer.
