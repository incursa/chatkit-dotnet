param(
    [string]$Configuration = "Release",
    [string]$Contract = "docs/30-contracts/test-gate.contract.yaml",
    [string]$ResultsDirectory = "artifacts/test-results/advisory",
    [string]$CoverageDirectory = "artifacts/coverage/advisory",
    [string]$OutDir = "artifacts/quality/testing",
    [ValidateSet("report", "inventory", "results", "coverage")]
    [string]$ShowKind = "report",
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$SkipTests,
    [switch]$SkipShow
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "QualityLane.Common.ps1")

Assert-DotNetAvailable

$repoRoot = Get-QualityRepoRoot
$contractPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $Contract
$resultsPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $ResultsDirectory
$coveragePath = Resolve-RepoPath -RepoRoot $repoRoot -Path $CoverageDirectory
$outDirPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $OutDir

if (-not $SkipTests) {
    & (Join-Path $repoRoot "scripts/quality/run-advisory-quality-tests.ps1") -Configuration $Configuration -ResultsDirectory $resultsPath -CoverageDirectory $coveragePath -NoRestore:$NoRestore -NoBuild:$NoBuild
    if ($LASTEXITCODE -ne 0) {
        throw "Advisory quality test lane failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Syncing Workbench quality evidence..." -ForegroundColor Cyan
& dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed with exit code $LASTEXITCODE."
}

Initialize-ArtifactDirectory -Path $outDirPath -Clean | Out-Null

$syncArgs = @(
    "tool"
    "run"
    "workbench"
    "quality"
    "sync"
    "--contract"
    $contractPath
    "--results"
    $resultsPath
    "--coverage"
    $coveragePath
    "--out-dir"
    $outDirPath
)

$syncOutput = (& dotnet @syncArgs 2>&1) | Out-String
if (-not [string]::IsNullOrWhiteSpace($syncOutput)) {
    Write-Host $syncOutput.TrimEnd()
}

if ($LASTEXITCODE -ne 0) {
    $isSchemaResolutionIssue =
        $syncOutput -match 'schema not found' -and
        $syncOutput -match 'coverage-summary\.json'

    if ($isSchemaResolutionIssue) {
        Write-Warning "Workbench quality sync hit a schema resolution issue while writing advisory coverage output. The advisory lane remains non-blocking, so continuing without normalized Workbench artifacts."
        $SkipShow = $true
    } else {
        throw "Workbench quality sync failed with exit code $LASTEXITCODE."
    }
}

if (-not $SkipShow) {
    $showPath = switch ($ShowKind) {
        "inventory" { Join-Path $outDirPath "test-inventory.json" }
        "results" { Join-Path $outDirPath "test-run-summary.json" }
        "coverage" { Join-Path $outDirPath "coverage-summary.json" }
        default { Join-Path $outDirPath "quality-report.json" }
    }

    $showArgs = @(
        "tool"
        "run"
        "workbench"
        "quality"
        "show"
        "--kind"
        $ShowKind
        "--path"
        $showPath
    )

    & dotnet @showArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Workbench quality show failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Quality evidence workflow completed." -ForegroundColor Green
