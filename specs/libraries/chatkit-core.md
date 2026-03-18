# ChatKit Core Library Spec

This specification defines the baseline conformance expectations for `Incursa.OpenAI.ChatKit`.

## Scenarios

- `LIB-CHATKIT-CORE-001`: The public API baseline for the core package is maintained and mapped to executable coverage.
- `LIB-CHATKIT-CORE-002`: ChatKit request envelopes and payloads serialize with the exact wire discriminators expected by upstream ChatKit clients.
- `LIB-CHATKIT-CORE-003`: The in-memory store and request router support the thread, item, attachment, and widget flows covered by the maintained core tests.
