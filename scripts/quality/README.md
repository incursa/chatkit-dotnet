# Quality Scripts

## Scripts
- [`scripts/quality/run-smoke-tests.ps1`](run-smoke-tests.ps1)
  - Runs the curated fast smoke suite from the explicit `Category=Smoke` test set in the maintained ChatKit test projects.
  - Default output: `artifacts/test-results/smoke/`
- [`scripts/quality/run-blocking-tests.ps1`](run-blocking-tests.ps1)
  - Runs the required CI-safe non-Docker lane against `Incursa.OpenAI.ChatKit.slnx`.
  - Default output: `artifacts/test-results/blocking/`
- [`scripts/quality/run-observational-tests.ps1`](run-observational-tests.ps1)
  - Runs `Category=KnownIssue` tests without blocking the overall process.
  - Default output: `artifacts/test-results/observational/`
- [`scripts/quality/run-advisory-quality-tests.ps1`](run-advisory-quality-tests.ps1)
  - Produces advisory TRX and curated Cobertura evidence without syncing Workbench.
  - Default outputs: `artifacts/test-results/advisory/` and `artifacts/coverage/advisory/`
- [`scripts/quality/run-quality-evidence.ps1`](run-quality-evidence.ps1)
  - Runs the advisory lane, syncs Workbench quality evidence, and shows the latest advisory report.
  - Default outputs: `artifacts/test-results/advisory/`, `artifacts/coverage/advisory/`, and `artifacts/quality/testing/`
- [`scripts/quality/run-workbench-evidence.ps1`](run-workbench-evidence.ps1)
  - Compatibility entrypoint that forwards to the advisory evidence runner.
  - Default outputs: `artifacts/test-results/advisory/` and `artifacts/coverage/advisory/`
- [`scripts/quality/run-library-coverage.ps1`](run-library-coverage.ps1)
  - Runs package-level unit coverage gates for the [`Incursa.OpenAI.ChatKit`](../../src/Incursa.OpenAI.ChatKit/README.md) and [`Incursa.OpenAI.ChatKit.AspNetCore`](../../src/Incursa.OpenAI.ChatKit.AspNetCore/README.md) packages.
  - Supports line threshold, optional branch threshold, and custom output roots.
- [`scripts/quality/run-library-mutation.ps1`](run-library-mutation.ps1)
  - Runs required mutation configs for the public ChatKit packages in this repo.
- [`scripts/quality/validate-library-traceability.ps1`](validate-library-traceability.ps1)
  - Verifies that `LIB-*` scenario IDs in specs are fully represented in the library conformance matrix.
  - Validates matrix mapped file paths for `Covered` rows, including PublicAPI baseline references for `PublicApi` coverage rows.

## Related Workflows
- [`.github/workflows/library-fast-quality.yml`](../../.github/workflows/library-fast-quality.yml)
- PR/manual fast library validation for traceability, public API baselines, coverage, and non-Docker tests
- [`.github/workflows/workbench-quality.yml`](../../.github/workflows/workbench-quality.yml)
- [`.github/workflows/testdocs.yml`](../../.github/workflows/testdocs.yml)

## Workbench Quality Workflow
- Canonical contract: [`docs/30-contracts/test-gate.contract.yaml`](../../docs/30-contracts/test-gate.contract.yaml)
- Smoke: `pwsh -File scripts/quality/run-smoke-tests.ps1`
- Blocking: `pwsh -File scripts/quality/run-blocking-tests.ps1`
- Observational: `pwsh -File scripts/quality/run-observational-tests.ps1`
- Generate advisory evidence: `pwsh -File scripts/quality/run-advisory-quality-tests.ps1`
- One-command advisory sync/show: `pwsh -File scripts/quality/run-quality-evidence.ps1`
- Normalize evidence manually: `dotnet tool run workbench quality sync --contract docs/30-contracts/test-gate.contract.yaml --results artifacts/test-results/advisory --coverage artifacts/coverage/advisory --out-dir artifacts/quality/testing`
- Inspect the current report: `dotnet tool run workbench quality show`
- Derived outputs: `artifacts/quality/testing/`
