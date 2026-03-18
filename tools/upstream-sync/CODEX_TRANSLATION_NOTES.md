# Codex Translation Notes

- This automation is translating only the behavior introduced in an upstream diff from `chatkit-python`.
- Preserve exact ChatKit wire compatibility for request discriminators, payload property names, and stream event shapes.
- Preserve the existing .NET structure and avoid unrelated refactors unless the diff explicitly requires new helpers.
- Prefer tests that mirror the upstream Python change and keep the quickstart/docs aligned when public behavior changes.
- Keep modifications minimal and frame them so reviewers can trace them directly to the upstream Python changes.

Include these notes in the Codex prompt to reinforce the existing instructions in AGENTS.md.
