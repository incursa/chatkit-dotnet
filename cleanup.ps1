[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot

Push-Location $repoRoot
try {
    & pwsh -NoProfile -File (Join-Path $repoRoot "scripts/setup-git-hooks.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "setup-git-hooks.ps1 failed with exit code $LASTEXITCODE."
    }

    & python -m pre_commit run --hook-stage manual --all-files
    if ($LASTEXITCODE -ne 0) {
        throw "pre-commit manual hook run failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
