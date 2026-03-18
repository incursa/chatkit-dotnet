---
workbench:
  type: doc
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /docs/testing-known-issues.md
---

# Testing Known Issues

The observational lane is available, but the repo currently has no intentionally tracked `Category=KnownIssue` tests.

When a regression needs to remain visible without blocking the main lane:

1. add `Trait("Category", "KnownIssue")`
2. document the reason in the test XML docs
3. remove the trait once the scenario is fixed
