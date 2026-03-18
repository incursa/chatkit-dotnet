param(
    [string]$Solution = "Incursa.OpenAI.ChatKit.slnx",
    [string]$Configuration = "Release",
    [string]$ResultsDirectory = "artifacts/test-results/advisory",
    [string]$CoverageDirectory = "artifacts/coverage/advisory",
    [string]$TestFilter = "",
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$SkipCoverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    Write-Host "Ignoring -TestFilter. The Workbench evidence lane now uses the curated advisory runsettings and coverage targets." -ForegroundColor Yellow
}

& (Join-Path $repoRoot "scripts/quality/run-advisory-quality-tests.ps1") `
    -Solution $Solution `
    -Configuration $Configuration `
    -ResultsDirectory $ResultsDirectory `
    -CoverageDirectory $CoverageDirectory `
    -NoRestore:$NoRestore `
    -NoBuild:$NoBuild `
    -SkipCoverage:$SkipCoverage

exit $LASTEXITCODE
