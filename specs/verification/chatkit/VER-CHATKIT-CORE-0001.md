# VER-CHATKIT-CORE-0001 - ChatKit Core Runtime Verification

## Verification Method

Execution, inspection, and documented test-inventory evidence from the current core test suite.

## Verifies

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

## Scope

This verification slice covers the current core-package proof for public API baseline mapping, request-envelope serialization, supported request-kind handling, transport classification, hidden-context filtering, selected streaming persistence and cancellation behavior, client-tool continuation, destructive retry, attachment-store gating, explicit extension-point behavior, widget diff behavior, and exported widget-definition loading and rendering.

## Evidence

- [`src/Incursa.OpenAI.ChatKit/PublicAPI.Shipped.txt`](../../../src/Incursa.OpenAI.ChatKit/PublicAPI.Shipped.txt)
- [`src/Incursa.OpenAI.ChatKit/PublicAPI.Unshipped.txt`](../../../src/Incursa.OpenAI.ChatKit/PublicAPI.Unshipped.txt)
- [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreTests.cs)
- [`tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs`](../../../tests/Incursa.OpenAI.ChatKit.Tests/ChatKitCoreBoundaryTests.cs)
- [`docs/testing/generated/stats.json`](../../../docs/testing/generated/stats.json)
- [`specs/libraries/library-conformance-matrix.md`](../../libraries/library-conformance-matrix.md)

## Status Summary

Current execution evidence is sufficient to anchor the expanded canonical requirements for the core package, but proof depth remains uneven. Serialization, request classification, visible-versus-hidden state, widget behavior, and selected lifecycle paths are exercised. Broader destructive retry paths, attachment lifecycle edges, pagination and ordering guarantees, and more negative or error branches remain open follow-up work rather than proven-complete coverage.
