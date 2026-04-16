---
workbench:
  type: specification
  workItems: []
  codeRefs: []
  pathHistory: []
  path: /specs/requirements/README.md
---

# Requirements

This directory holds the canonical SpecTrace requirement suites for the repository.
Canonical requirement artifacts are authored in sibling `.json` files, with Markdown companions for human review.

## Suites

- [`chatkit/`](chatkit/README.md): the ChatKit runtime and ASP.NET Core hosting requirement slice

## Gap Tracking

- [`chatkit/REQUIREMENT-GAPS.md`](chatkit/REQUIREMENT-GAPS.md): the local requirement gap ledger for ChatKit

The older [`specs/libraries/`](../libraries/) documents remain in the repo as compatibility inputs for the current library traceability script, but they are no longer the canonical requirement corpus once the `chatkit/` suite exists.
