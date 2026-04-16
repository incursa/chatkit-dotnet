# Specification Index

This directory contains the repository's authored specification and traceability material.

Canonical SpecTrace areas:

- [`specs/requirements/`](requirements/README.md)
- [`specs/architecture/`](architecture/README.md)
- [`specs/verification/`](verification/README.md)
- [`specs/work-items/`](work-items/README.md)

Compatibility traceability surfaces:

- [`specs/libraries/chatkit-core.md`](libraries/chatkit-core.md)
- [`specs/libraries/chatkit-aspnetcore.md`](libraries/chatkit-aspnetcore.md)
- [`specs/libraries/library-conformance-matrix.md`](libraries/library-conformance-matrix.md)

The `libraries/` documents remain in place for [`scripts/quality/validate-library-traceability.ps1`](../scripts/quality/validate-library-traceability.ps1) and the current `LIB-*` coverage mapping. The canonical ChatKit requirement corpus now lives under [`specs/requirements/chatkit/`](requirements/chatkit/README.md), with supporting design and verification artifacts under the sibling `architecture/` and `verification/` areas.
