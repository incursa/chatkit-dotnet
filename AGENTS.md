# chatkit-dotnet translation guidance

- This repository hosts the semantic .NET translation of https://github.com/openai/chatkit-python.
- Treat the upstream Python repo as the source of truth for behavior; port only those behaviors that the upstream diff actually introduces.
- Preserve the established .NET architecture (namespaces, layering, and conventions) already present in this project, and avoid unrelated refactors.
- Update .NET tests only when they are needed to secure new or changed behavior from upstream, and keep those changes scoped to the translated files.
- Keep every change minimal and reviewable, with clear linkage to the upstream commit summary and diff.
