# VER-CHATKIT-ASPNETCORE-0001 - ChatKit ASP.NET Core Host Verification

## Verification Method

Execution, inspection, and documented test-inventory evidence from the current ASP.NET Core test suite.

## Verifies

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

## Scope

This verification slice covers the current proof for the ASP.NET Core package public API baseline, HTTP endpoint result handling, service-registration modes, packaged asset emission, explicit host-mode selection, direct versus hosted mode validation, serialized browser host configuration, value-precedence rules, host-surface normalization, and success versus failure DOM rendering. Direct browser-runtime proof for the packaged JavaScript host bridge now lives in [`VER-CHATKIT-ASPNETCORE-BROWSER-0001`](VER-CHATKIT-ASPNETCORE-BROWSER-0001.md).

## Evidence

- [`src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Shipped.txt`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Shipped.txt)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Unshipped.txt`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/PublicAPI.Unshipped.txt)
- [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitEndpointTests.cs)
- [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitAspNetCoreServiceCollectionExtensionsTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/ChatKitAspNetCoreServiceCollectionExtensionsTests.cs)
- [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitAssetsTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitAssetsTagHelperTests.cs)
- [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/IncursaChatKitTagHelperTests.cs)
- [`docs/testing/generated/stats.json`](../../../docs/testing/generated/stats.json)
- [`docs/30-contracts/chatkit-tag-helper.md`](../../../docs/30-contracts/chatkit-tag-helper.md)

## Status Summary

Current evidence is stronger for the ASP.NET Core package than for the core runtime. The endpoint adapter, service registration, asset helper, and host tag helpers all have direct test coverage and documented inventory entries. The packaged browser runtime no longer relies only on indirect proof from the rendered config contract; that direct runtime evidence is now tracked in [`VER-CHATKIT-ASPNETCORE-BROWSER-0001`](VER-CHATKIT-ASPNETCORE-BROWSER-0001.md).
