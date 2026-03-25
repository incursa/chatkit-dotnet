# ChatKit ASP.NET Core Library Spec

This specification defines the baseline conformance expectations for [`Incursa.OpenAI.ChatKit.AspNetCore`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md).

## Scenarios

- `LIB-CHATKIT-ASPNETCORE-001`: The public API baseline for the ASP.NET Core package is maintained and mapped to executable coverage.
- `LIB-CHATKIT-ASPNETCORE-002`: The ChatKit endpoint adapter accepts JSON requests and returns the expected HTTP payload shape for the maintained sample and tests.
- `LIB-CHATKIT-ASPNETCORE-003`: The Razor tag helpers emit the packaged ChatKit browser assets and serialize the expected host configuration for the UI wrapper surface.
