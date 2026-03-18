---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/testing/test-doc-schema.md
---

# Test Documentation Schema

The test documentation generator reads XML documentation from test methods and normalizes a small set of metadata fields.

Required tags:

- `summary`: short statement of the scenario under test
- `intent`: why the test exists
- `scenario`: stable scenario identifier or short scenario label
- `behavior`: expected externally visible behavior

Recommended conventions:

- keep `summary` and `behavior` focused on public behavior, not implementation details
- align `scenario` with the closest `LIB-*` spec row when the test maps directly to the conformance matrix
- use `Trait("Category", "Smoke")` only for intentionally curated fast checks
- use `Trait("Category", "Integration")` for Docker-backed tests that exercise real external services
- use `Trait("Category", "KnownIssue")` only for visible non-blocking regressions
- add `Trait("RequiresDocker", "true")` when a test needs Docker to run

Example:

```csharp
/// <summary>Runner persists the final output into the session conversation.</summary>
/// <intent>Protect the persisted conversation contract for resumable runs.</intent>
/// <scenario>LIB-EXEC-SESSION-001</scenario>
/// <behavior>Completed runs append a final-output item that is available on the next load.</behavior>
[Fact]
public async Task RunAsync_CompletesWithFinalOutputAndPersistsConversation()
{
    // test body
}
```
