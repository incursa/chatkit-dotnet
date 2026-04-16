# ARC-CHATKIT-ASPNETCORE-0001 - ChatKit ASP.NET Core Host Boundary Architecture

## Purpose

Describe how the ASP.NET Core package stays a thin transport and browser-host layer over the core ChatKit runtime while projecting a bounded upstream browser configuration surface.

## Satisfies

- `REQ-CHATKIT-ASPNETCORE-0001`
- `REQ-CHATKIT-ASPNETCORE-0002`
- `REQ-CHATKIT-ASPNETCORE-0003`
- `REQ-CHATKIT-ASPNETCORE-0004`
- `REQ-CHATKIT-ASPNETCORE-0005`
- `REQ-CHATKIT-ASPNETCORE-0006`
- `REQ-CHATKIT-ASPNETCORE-0007`
- `REQ-CHATKIT-ASPNETCORE-0008`
- `REQ-CHATKIT-ASPNETCORE-0009`
- `REQ-CHATKIT-ASPNETCORE-0010`
- `REQ-CHATKIT-ASPNETCORE-0011`
- `REQ-CHATKIT-ASPNETCORE-0012`
- `REQ-CHATKIT-ASPNETCORE-0013`

## Design Summary

The ASP.NET Core package is intentionally split into two thin surfaces. The HTTP adapter maps one POST route, buffers the incoming body, creates a request context, and forwards the payload to the core server, which remains the owner of request parsing and protocol behavior.

The browser-host layer stores shared defaults in [`ChatKitAspNetCoreOptions`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs) and projects those defaults through explicit tag helpers for assets, direct API mode, and hosted mode. The mode-specific helpers resolve values through one precedence stack, validate required combinations after option resolution, normalize optional substructures before serialization, and emit either one valid browser config payload or one visible initialization failure. The resulting host surface is a bounded .NET projection of the upstream browser configuration model rather than a second implementation of the browser runtime itself.

## Key Components

- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreOptions.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelperBase.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitTagHelper.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitApiTagHelper.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitHostedTagHelper.cs)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/TagHelpers/IncursaChatKitAssetsTagHelper.cs)

## Constraints

- The endpoint must preserve the core server's transport classification and not invent its own heuristics.
- Asset emission must be idempotent within one Razor rendering context.
- API mode and hosted mode have distinct required fields and incompatible combinations.
- Tag-helper attributes override options, and options override helper defaults.
- Incomplete header actions, start prompts, tools, models, font sources, upload settings, and disclaimer values are normalized or rejected before serialization.
- Successful rendering and initialization failure rendering have distinct DOM contracts.

## Risks

- If endpoint behavior grows beyond forwarding and content-type selection, the package will duplicate core runtime rules and drift from the core spec.
- If mode validation or normalization weakens, callers can render browser hosts that fail only at runtime.
- If the host surface implies parity with upstream browser features that are not actually supported, consumers will rely on configuration that the runtime cannot honor.
