---
workbench:
  type: specification
  workItems: []
  codeRefs:
    - src/Incursa.OpenAI.ChatKit/ChatKitServer.cs
    - src/Incursa.OpenAI.ChatKit/Store.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitEndpointRouteBuilderExtensions.cs
    - src/Incursa.OpenAI.ChatKit.AspNetCore/ChatKitAspNetCoreServiceCollectionExtensions.cs
  pathHistory: []
  path: /specs/requirements/chatkit/README.md
---

# ChatKit Requirements Suite

This directory is the canonical SpecTrace requirement suite for the ChatKit packages in this repository.

## Specifications

- [`SPEC-CHATKIT-CORE.json`](SPEC-CHATKIT-CORE.json) / [`SPEC-CHATKIT-CORE.md`](SPEC-CHATKIT-CORE.md): core runtime, protocol, persistence, widget, and public API requirements for [`Incursa.OpenAI.ChatKit`](../../../src/Incursa.OpenAI.ChatKit/README.md)
- [`SPEC-CHATKIT-ASPNETCORE.json`](SPEC-CHATKIT-ASPNETCORE.json) / [`SPEC-CHATKIT-ASPNETCORE.md`](SPEC-CHATKIT-ASPNETCORE.md): endpoint, DI, Razor host, and public API requirements for [`Incursa.OpenAI.ChatKit.AspNetCore`](../../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md)

## Related Artifacts

- [`REQUIREMENT-GAPS.md`](REQUIREMENT-GAPS.md)
- [`../../architecture/chatkit/ARC-CHATKIT-CORE-0001.json`](../../architecture/chatkit/ARC-CHATKIT-CORE-0001.json)
- [`../../architecture/chatkit/ARC-CHATKIT-ASPNETCORE-0001.json`](../../architecture/chatkit/ARC-CHATKIT-ASPNETCORE-0001.json)
- [`../../verification/chatkit/VER-CHATKIT-CORE-0001.json`](../../verification/chatkit/VER-CHATKIT-CORE-0001.json)
- [`../../verification/chatkit/VER-CHATKIT-ASPNETCORE-0001.json`](../../verification/chatkit/VER-CHATKIT-ASPNETCORE-0001.json)

## Migration Note

The authored files under [`../../libraries/`](../../libraries/) remain in place because [`scripts/quality/validate-library-traceability.ps1`](../../../scripts/quality/validate-library-traceability.ps1) still consumes the `LIB-*` matrix. Treat those files as compatibility surfaces and this directory as the canonical source of requirements.
