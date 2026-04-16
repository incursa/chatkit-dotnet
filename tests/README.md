# Tests

This folder contains the xUnit test projects for `Incursa.OpenAI.ChatKit` and `Incursa.OpenAI.ChatKit.AspNetCore`.

## Taxonomy

- Use `Trait("Category", "Positive")` for representative success paths.
- Use `Trait("Category", "Negative")` for expected rejection and error paths.
- Use `Trait("Requirement", "REQ-...")` when a test is directly proving one of the canonical SpecTrace requirements under [`../specs/requirements/chatkit/`](../specs/requirements/chatkit/README.md).
- Keep `Trait("Category", "Smoke")` for the curated fast lane.
- Keep `Trait("Category", "Unit")`, `Integration`, `KnownIssue`, and other lane-specific tags only when they affect execution policy.

## Related Surfaces

- Mutation configs live under [`../scripts/quality/stryker`](../scripts/quality/stryker).
- Permanent BenchmarkDotNet suites live under [`../benchmarks`](../benchmarks/README.md).
- SharpFuzz harnesses live under [`../fuzz`](../fuzz/README.md).
