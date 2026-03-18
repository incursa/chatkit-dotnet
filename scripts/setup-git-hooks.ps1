[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $RepoRoot
try {
    & git config core.hooksPath .githooks
    if ($LASTEXITCODE -ne 0) {
        throw "git config failed with exit code $LASTEXITCODE."
    }

    Write-Host "Configured git hooks path to '.githooks'."
}
finally {
    Pop-Location
}
