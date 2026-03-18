---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/quality/library-hardening-baseline.md
---

# Library Hardening Baseline

This repo treats the following artifacts as the minimum hardening baseline for a public library:

- authored `LIB-*` specifications for each major domain
- a conformance matrix mapping each scenario to tests or automation artifacts
- committed public API analyzer baselines for each public package
- required blocking and smoke test lanes
- package-level coverage and advisory mutation automation
- generated test documentation inventory

A new public API is not considered fully integrated until it is:

1. intentionally added to `PublicAPI.Unshipped.txt`
2. covered by an authored `LIB-*` scenario
3. mapped to automated tests in the conformance matrix
