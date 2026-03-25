---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/testing-operating-model.md
---

# Testing Operating Model

The repo uses four quality lanes:

- `smoke`: fast curated checks tagged with `Category=Smoke`
- `blocking`: required non-Docker validation over the maintained solution
- `observational`: visible `Category=KnownIssue` regressions that do not block the lane
- `advisory`: blocking tests plus package coverage, then Workbench normalization
- `integration`: Docker-backed checks tagged with `Category=Integration` and `RequiresDocker=true`

Local commands:

```powershell
pwsh -File scripts/quality/run-smoke-tests.ps1
pwsh -File scripts/quality/run-blocking-tests.ps1
pwsh -File scripts/quality/run-observational-tests.ps1
pwsh -File scripts/quality/run-quality-evidence.ps1
dotnet test --settings runsettings/integration.runsettings
pwsh -File scripts/testing/run-integration-tests.ps1
```

Traceability is governed by:

- `specs/libraries/*.md`
- [`specs/libraries/library-conformance-matrix.md`](../specs/libraries/library-conformance-matrix.md)
- [`scripts/quality/validate-library-traceability.ps1`](../scripts/quality/validate-library-traceability.ps1)
- `src/*/PublicAPI.Shipped.txt`

The Microsoft public API analyzer baselines and the authored `LIB-*` scenarios are both treated as quality evidence: a public API row is only considered covered when it maps to baseline files and automated tests.
