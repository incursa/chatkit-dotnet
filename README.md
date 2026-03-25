# Incursa.OpenAI.ChatKit

[![CI](https://github.com/incursa/chatkit-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/incursa/chatkit-dotnet/actions/workflows/ci.yml)
[![Quality](https://github.com/incursa/chatkit-dotnet/actions/workflows/library-fast-quality.yml/badge.svg)](https://github.com/incursa/chatkit-dotnet/actions/workflows/library-fast-quality.yml)
[![Docs](https://github.com/incursa/chatkit-dotnet/actions/workflows/testdocs.yml/badge.svg)](https://github.com/incursa/chatkit-dotnet/actions/workflows/testdocs.yml)
[![NuGet Core](https://img.shields.io/nuget/v/Incursa.OpenAI.ChatKit.svg)](https://www.nuget.org/packages/Incursa.OpenAI.ChatKit/)
[![NuGet ASP.NET Core](https://img.shields.io/nuget/v/Incursa.OpenAI.ChatKit.AspNetCore.svg)](https://www.nuget.org/packages/Incursa.OpenAI.ChatKit.AspNetCore/)
[![License](https://img.shields.io/github/license/incursa/chatkit-dotnet)](LICENSE)

[`Incursa.OpenAI.ChatKit`](src/Incursa.OpenAI.ChatKit/README.md) is a `.NET 10` translation of the server-side ChatKit library from `openai/chatkit-python`, with an ASP.NET Core extension package for endpoint hosting and Razor-based UI wrapping.

Upstream source of truth: [openai/chatkit-python](https://github.com/openai/chatkit-python).

## Packages

- [`Incursa.OpenAI.ChatKit`](src/Incursa.OpenAI.ChatKit/README.md): core ChatKit models, request routing, store abstractions, widget diff helpers, and agent integration helpers.
- [`Incursa.OpenAI.ChatKit.AspNetCore`](src/Incursa.OpenAI.ChatKit.AspNetCore/README.md): ASP.NET Core endpoint mapping, Razor tag helpers, and packaged browser assets for ChatKit hosts.

## Scope

- Includes exact wire-shape ChatKit request and event handling for the currently translated surface.
- Includes a runnable ASP.NET Core quickstart sample, a Razor UI wrapper package, and repo-managed quality tooling.
- Reuses `Incursa.OpenAI.Agents` where ChatKit needs agent-side interop instead of duplicating that runtime here.
- Excludes unrelated agent orchestration features that belong in the agents repo rather than ChatKit.

## Quickstart

Restore and build the repo:

```bash
dotnet restore
dotnet build Incursa.OpenAI.ChatKit.slnx -c Release
dotnet test Incursa.OpenAI.ChatKit.slnx -c Release
```

Run the sample server:

```bash
dotnet run --project samples/Incursa.OpenAI.ChatKit.QuickstartSample/Incursa.OpenAI.ChatKit.QuickstartSample.csproj
```

The sample maps `/chatkit` and returns a simple assistant message through the ChatKit server pipeline.

If you need to validate ChatKit against a local checkout of `openai-agents-dotnet` before `Incursa.OpenAI.Agents` `1.0.22` is available on your configured NuGet source, set:

```powershell
$Env:USE_LOCAL_INCURSA_AGENTS_PROJECT = "true"
$Env:LOCAL_INCURSA_AGENTS_REPO_ROOT = "C:\src\incursa\openai-agents-dotnet"
```

## Repository layout

- [`src/Incursa.OpenAI.ChatKit`](src/Incursa.OpenAI.ChatKit/README.md)
- [`src/Incursa.OpenAI.ChatKit.AspNetCore`](src/Incursa.OpenAI.ChatKit.AspNetCore/README.md)
- [`tests/Incursa.OpenAI.ChatKit.Tests`](tests/Incursa.OpenAI.ChatKit.Tests)
- [`tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests`](tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests)
- [`samples/Incursa.OpenAI.ChatKit.QuickstartSample`](samples/Incursa.OpenAI.ChatKit.QuickstartSample)
- [`tools/upstream-sync`](tools/upstream-sync/README.md)

## Tooling and quality

The repo carries forward the same hardened development model used in the agents translation:

- `.editorconfig`, `.gitattributes`, `.githooks`, and `.pre-commit-config.yaml`
- local dotnet tools for Workbench and Incursa Test Docs
- smoke, blocking, observational, advisory, coverage, and mutation scripts under [`scripts/quality`](scripts/quality/README.md)
- Workbench quality contracts under [`docs/30-contracts`](docs/30-contracts/README.md)
- traceability specs under [`specs/libraries`](specs/libraries)

Useful commands:

```bash
dotnet tool restore
pwsh -File scripts/quality/run-smoke-tests.ps1
pwsh -File scripts/quality/run-blocking-tests.ps1
pwsh -File scripts/quality/run-quality-evidence.ps1
pwsh -File scripts/quality/validate-library-traceability.ps1
```

## Upstream sync automation

[`tools/upstream-sync/Invoke-UpstreamSync.ps1`](tools/upstream-sync/Invoke-UpstreamSync.ps1) watches a local clone of `chatkit-python`, builds a Codex translation prompt from the upstream commit range, validates the result locally, and prepares a sync branch/PR flow.

[`tools/upstream-sync/Invoke-UpstreamChatKitRuntimeSync.ps1`](tools/upstream-sync/Invoke-UpstreamChatKitRuntimeSync.ps1) watches the packaged ChatKit wrapper dependency surface, checks for a newer `@openai/chatkit` release, and regenerates the wrapper assets under [`src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime`](src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime) and [`src/Incursa.OpenAI.ChatKit.AspNetCore/wwwroot/chatkit`](src/Incursa.OpenAI.ChatKit.AspNetCore/wwwroot/chatkit).

GitHub Actions runs a daily Python upstream check from [`.github/workflows/chatkit-python-upstream-check.yml`](.github/workflows/chatkit-python-upstream-check.yml) and the runtime sync daily from [`.github/workflows/chatkit-runtime-upstream-sync.yml`](.github/workflows/chatkit-runtime-upstream-sync.yml). The Python check creates a GitHub issue when it detects new upstream commits so the translation can be run manually afterward.

Default upstream clone path:

```text
C:\src\openai\chatkit-python
```

## Documentation

- [Quickstart](docs/quickstart.md)
- [ASP.NET Core hosting](docs/extensions.md)
- [Parity manifest](docs/parity/manifest.md)
- [Maintenance checklist](docs/parity/maintenance-checklist.md)
- [Quality contracts](docs/30-contracts/README.md)
- [Testing docs](docs/testing/README.md)
