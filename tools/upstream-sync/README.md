# Upstream Sync Watcher

This folder hosts a PowerShell watcher that polls the local `chatkit-python` clone, generates Codex-driven translations of new upstream commits, validates them locally, and pushes or creates PRs for the translated changes.

The same folder also hosts a separate npm sync script for the packaged ChatKit runtime assets. The Python translation flow and the npm dependency flow are intentionally split so each upstream source can be updated independently.

## Requirements

- PowerShell 7 or Windows PowerShell
- `git`, `gh`, `codex`, and `dotnet` must be on `PATH`
- A clean `Incursa.OpenAI.ChatKit` working tree (unless overridden with `-AllowDirty`)
- The upstream Python repository must be cloned at `C:\src\openai\chatkit-python` by default (configurable)

## State files

- `state.json` (tracked): stores the upstream metadata and the last translated SHA that the automation has committed.
- `state.local.json` (ignored): tracks runtime metadata such as the last attempted SHA, the last run timestamp, and the bootstrap SHA used when this repo was empty the first time the watcher ran. This file is persisted automatically and must remain ignored.

The first invocation bootstraps the state by recording the current upstream `main` SHA in `state.local.json` and exiting without translating. Rerun the script afterward to begin translating future commits.

## Running

- One-shot mode:
  ```
  pwsh tools/upstream-sync/Invoke-UpstreamSync.ps1 -Once
  ```

- Loop mode (runs every 5 minutes by default):
  ```
  pwsh tools/upstream-sync/Invoke-UpstreamSync.ps1 -Loop -IntervalMinutes 5
  ```

- Force translation from a specific SHA:
  ```
  pwsh tools/upstream-sync/Invoke-UpstreamSync.ps1 -Once -ForceFromSha <sha>
  ```

- Check whether the Python upstream has moved without translating it:
  ```
  pwsh tools/upstream-sync/Invoke-UpstreamSync.ps1 -CheckOnly
  ```

  In GitHub Actions, the daily Python check uses this mode to open an issue when new upstream commits are detected.

- Update the packaged ChatKit runtime from the latest `@openai/chatkit` release:
  ```
  pwsh tools/upstream-sync/Invoke-UpstreamChatKitRuntimeSync.ps1
  ```

- Skip steps when desired:
  - `-SkipBuild`, `-SkipTests` disable the respective `dotnet` commands.
  - `-SkipPush` avoids pushing the sync branch (also prevents PR creation).
  - `-SkipPr` stops PR creation after a push.
  - `-AllowDirty` lets the watcher run even if the repo already has uncommitted changes (use with care).

## Codex invocation

The watcher builds a prompt that includes:

- the upstream commit range and summaries
- the unified diff (truncated if very large)
- the list of changed files
- the guidance from `AGENTS.md` and the helper notes in `CODEX_TRANSLATION_NOTES.md`

Codex is run via `codex exec --dangerously-bypass-approvals-and-sandbox` from the repository root with the upstream clone added via `--add-dir`.

## Post-sync state

Successful syncs commit the translated files plus the updated `state.json`, push a `sync/chatkit-upstream-<shortsha>` branch, and call `gh pr create` with a title/body that references the upstream range and commits. Failed runs leave the branch and working tree intact for inspection; `state.json` is only updated when both translation and validation succeed.

The npm runtime sync updates `src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime/package.json`, `package-lock.json`, and the generated `wwwroot/chatkit` bundle when a newer `@openai/chatkit` release is available.
