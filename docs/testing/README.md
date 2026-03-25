---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/testing/README.md
---

# Test Documentation

This folder describes the XML doc schema for the xUnit test suite and hosts generated test inventory artifacts.

Workbench quality integration uses:

- Authored intent contract: [`docs/30-contracts/test-gate.contract.yaml`](../30-contracts/test-gate.contract.yaml)
- Smoke lane: [`scripts/quality/run-smoke-tests.ps1`](../../scripts/quality/run-smoke-tests.ps1)
- Blocking lane: [`scripts/quality/run-blocking-tests.ps1`](../../scripts/quality/run-blocking-tests.ps1)
- Observational lane: [`scripts/quality/run-observational-tests.ps1`](../../scripts/quality/run-observational-tests.ps1)
- Advisory evidence runner: [`scripts/quality/run-advisory-quality-tests.ps1`](../../scripts/quality/run-advisory-quality-tests.ps1)
- Full quality workflow: [`scripts/quality/run-quality-evidence.ps1`](../../scripts/quality/run-quality-evidence.ps1)
- Normalized Workbench outputs: `artifacts/quality/testing/`
- Operating model: [`docs/testing-operating-model.md`](../testing-operating-model.md)
- Known issues: [`docs/testing-known-issues.md`](../testing-known-issues.md)

- Schema: [`docs/testing/test-doc-schema.md`](test-doc-schema.md)
- Generated docs: [`docs/testing/generated/README.md`](generated/README.md)

## How to document tests
1. Add XML doc tags to each `[Fact]` or `[Theory]` method: `summary`, `intent`, `scenario`, and `behavior`.
2. Use `Trait("Category", "...")` tags when the test should participate in smoke or known-issue lanes.
3. Refresh generated docs locally with `dotnet tool run incursa-testdocs -- generate --repoRoot . --outDir docs/testing/generated`.

Optional: `Incursa.TestDocs.Analyzers` warns when required tags are missing or still placeholder text.
