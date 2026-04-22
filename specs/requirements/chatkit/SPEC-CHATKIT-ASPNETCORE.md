# SPEC-CHATKIT-ASPNETCORE - Incursa.OpenAI.ChatKit.AspNetCore Hosting and Public Surface

## Purpose

Define the canonical public API, endpoint transport, DI registration, Razor host, and serialized browser-config requirements for the ASP.NET Core ChatKit adapter package.

## Scope

This specification covers the consumer-facing ASP.NET Core API surface, endpoint mapping via `MapChatKit<TServer, TContext>(...)`, package-level service registration defaults, packaged asset emission, explicit Razor host mode selection, render-time validation, serialized browser host configuration, host-value precedence, host-surface normalization, and rendered DOM failure and success contracts.

## Context

The ASP.NET Core package is intentionally a thin host and transport layer over the core ChatKit runtime. Its public behavior still matters because endpoint shape, DI defaults, rendered DOM shape, and serialized browser config are contractual surfaces for consumers even though the assistant logic itself remains in the core package. This package also acts as the repository's .NET projection of a bounded subset of the upstream `chatkit-js` browser configuration surface.

## Host Surface Inventory

- `MapChatKit<TServer, TContext>(...)` is the HTTP adapter for raw ChatKit protocol requests.
- `ChatKitAspNetCoreServiceCollectionExtensions` owns shared host defaults and explicit API versus hosted registration modes.
- `ChatKitAspNetCoreOptions` is the browser-host configuration carrier.
- The Razor host consists of one packaged assets helper, one fail-closed generic helper, and explicit API and hosted render helpers.

## Upstream Source Lineage

- The .NET host surface was reviewed against the upstream `chatkit-js` browser contract, primarily `packages/chatkit/types/index.d.ts`.
- Additional upstream source lineage came from `packages/docs/src/content/docs/quickstart.mdx`, `packages/docs/src/content/docs/customize.mdx`, `packages/docs/src/content/docs/quick-reference/chatkit-component.mdx`, `packages/docs/src/content/docs/quick-reference/use-chatkit.mdx`, and `packages/chatkit-react/src/useChatKit.ts`.
- This repository does not expose the React hook as a .NET API. This spec owns render-time host serialization, DI, assets, and tag-helper behavior, while the packaged browser runtime that mirrors the upstream web-component control and event surface is owned by [`SPEC-CHATKIT-ASPNETCORE-BROWSER`](SPEC-CHATKIT-ASPNETCORE-BROWSER.md).

## REQ-CHATKIT-ASPNETCORE-0001 Keep the ASP.NET Core package surface aligned with the approved hosting facade

The library MUST expose only the approved endpoint-mapping, options, service-registration, and Razor tag-helper surface for the ASP.NET Core ChatKit package, and it MUST treat the public API analyzer files as the compatibility baseline for changes to that host-facing surface.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Shipped.txt`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Shipped.txt)
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Unshipped.txt`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Unshipped.txt)

## REQ-CHATKIT-ASPNETCORE-0002 Keep the HTTP adapter thin and result-driven

`MapChatKit<TServer, TContext>(...)` MUST buffer the incoming POST body, create the per-request context through the supplied factory, delegate protocol processing to `ChatKitServer<TContext>.ProcessAsync(...)`, and emit either `application/json` or `text/event-stream` based on the returned `ChatKitProcessResult` without re-implementing core protocol routing in the ASP.NET Core layer.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs)

## REQ-CHATKIT-ASPNETCORE-0003 Expose explicit service-registration modes for shared browser host defaults

The package MUST support the generic `AddOpenAIChatKit(...)` registration path plus explicit `AddOpenAIChatKitHosted(...)` and `AddOpenAIChatKitApi(...)` modes, and those modes MUST apply the documented API URL, session-endpoint, and domain-key defaults and guards rather than leaving host mode selection ambiguous.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitAspNetCoreServiceCollectionExtensionsTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitAspNetCoreServiceCollectionExtensionsTests.cs)

## REQ-CHATKIT-ASPNETCORE-0004 Emit packaged browser assets at most once per Razor rendering context

The packaged assets tag helper MUST emit the approved ChatKit CSS, upstream script, and local bootstrap module at most once per Razor rendering context, allow those emissions to be toggled independently, and suppress output entirely when nothing remains to render.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitAssetsTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitAssetsTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0005 Require explicit host mode helpers and keep the generic helper fail-closed

The Razor host surface MUST force callers to choose an explicit API or hosted mode for usable configuration, and the generic `<incursa-chatkit>` helper MUST fail closed with a clear mode-selection error instead of guessing whether direct API mode or hosted session mode should be rendered.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0006 Project the supported upstream browser options into a bounded .NET host surface

The package MUST define and serialize a .NET host configuration surface that corresponds to the supported upstream browser options for connection mode, locale, theme, frame title, initial thread, header, history, start screen, composer, disclaimer, entities, and thread item actions, and it MUST surface .NET-specific handler-path and forwarding fields explicitly instead of implying parity with upstream browser features that this package does not expose directly.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs)
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)
  - Upstream: `packages/chatkit/types/index.d.ts`

## REQ-CHATKIT-ASPNETCORE-0007 Resolve browser host values using explicit attributes first, options second, and defaults last

The Razor host helpers MUST resolve browser-facing values using explicit tag-helper attributes first, `ChatKitAspNetCoreOptions` values second, and helper defaults last, and they MUST keep that precedence consistent across connection mode, visual settings, composer settings, callback-path settings, and element sizing.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs)
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs)
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)

## REQ-CHATKIT-ASPNETCORE-0008 Validate and normalize direct API mode explicitly

The `<incursa-chatkit-api>` helper MUST require `api-url`, MUST require the resolved `domain-key`, MUST reject `session-endpoint` and `action-endpoint` inputs for that mode, and MUST serialize a direct-mode config that clears hosted-only fields instead of leaving ambiguous mixed-mode payloads.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0009 Validate and normalize hosted session mode explicitly

The `<incursa-chatkit-hosted>` helper MUST reject `api-url` and `domain-key` for hosted mode, MUST require `session-endpoint`, MUST require `action-endpoint` when widget forwarding is enabled, and MUST serialize a hosted-mode config that clears direct-API-only fields instead of leaving ambiguous mixed-mode payloads.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0010 Reject half-configured header actions and invalid start-prompt payloads

The host configuration pipeline MUST require both icon and handler for each serialized header action, MUST reject start-screen prompt content that is neither a string nor a sequence of `UserMessageContent` values, and MUST materialize accepted structured prompt sequences before serialization so the emitted config is stable during Razor rendering.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0011 Normalize optional composer, theme, upload, and disclaimer substructures before serialization

The host configuration pipeline MUST omit empty optional substructures, treat attachment constraints as an implicit request to enable composer attachments, omit incomplete composer tools, composer models, and font sources, clear upload URLs for non-direct upload strategies, and omit the disclaimer object when no disclaimer text is present so the serialized config does not imply support the runtime will not honor.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0012 Render the ChatKit host element with the documented DOM contract when config succeeds

When host rendering succeeds, the tag-helper base MUST render a `div` element that carries `data-incursa-chatkit-host="true"` and the serialized browser config payload, MUST merge the packaged host class with caller-supplied classes, MUST preserve the optional `id` attribute, and MUST emit explicit `min-height` and `height` styles when a resolved height value is present.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## REQ-CHATKIT-ASPNETCORE-0013 Render a visible initialization failure instead of partial config when host rendering fails

When host configuration or serialization fails, the tag-helper base MUST log the rendering failure, MUST emit `data-incursa-chatkit-error="true"`, MUST render the visible initialization failure text, and MUST avoid emitting a misleading success-path config payload.

Trace:
- Satisfied By: `ARC-CHATKIT-ASPNETCORE-0001`
- Verified By: `VER-CHATKIT-ASPNETCORE-0001`
- Source Refs:
  - [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs)
  - [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)

## Scope Exclusions

This specification does not define assistant behavior, persistence semantics, request discriminators, or widget diff logic. Those behaviors are owned by [`SPEC-CHATKIT-CORE`](SPEC-CHATKIT-CORE.md) even when they are exercised through the ASP.NET Core package. Browser-runtime mount behavior, callback resolution, and the mirrored web-component control and event surface are owned by [`SPEC-CHATKIT-ASPNETCORE-BROWSER`](SPEC-CHATKIT-ASPNETCORE-BROWSER.md).
