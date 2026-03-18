param(
    [string]$Solution = "Incursa.OpenAI.ChatKit.slnx",
    [string]$Configuration = "Release",
    [string]$Runsettings = "runsettings/blocking.runsettings",
    [string]$ResultsDirectory = "artifacts/test-results/advisory",
    [string]$CoverageDirectory = "artifacts/coverage/advisory",
    [switch]$NoRestore,
    [switch]$NoBuild,
    [switch]$SkipCoverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "QualityLane.Common.ps1")

Assert-DotNetAvailable

$repoRoot = Get-QualityRepoRoot
$solutionPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $Solution
$runsettingsPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $Runsettings
$resultsPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $ResultsDirectory
$coveragePath = Resolve-RepoPath -RepoRoot $repoRoot -Path $CoverageDirectory
$summaryPath = Join-Path $resultsPath "summary.md"
$libraryCoveragePath = Join-Path $coveragePath "libraries"
$libraryCoverageTargets = @(
    "ChatKitCore",
    "ChatKitAspNetCore"
)

Write-Host "Running advisory lane..." -ForegroundColor Cyan
Write-Host "Solution: $solutionPath" -ForegroundColor Yellow
Write-Host "Runsettings: $runsettingsPath" -ForegroundColor Yellow
Write-Host "Results: $resultsPath" -ForegroundColor Yellow
Write-Host "Coverage: $coveragePath" -ForegroundColor Yellow

Initialize-ArtifactDirectory -Path $resultsPath -Clean | Out-Null
Initialize-ArtifactDirectory -Path $coveragePath -Clean | Out-Null
Invoke-TestPrerequisites -Solution $solutionPath -Configuration $Configuration -NoRestore:$NoRestore -NoBuild:$NoBuild

$testArgs = @(
    "test"
    $solutionPath
    "--configuration"
    $Configuration
    "--settings"
    $runsettingsPath
    "--results-directory"
    $resultsPath
    "--logger"
    "trx"
    "--no-build"
    "--no-restore"
)

& dotnet @testArgs
if ($LASTEXITCODE -ne 0) {
    throw "Advisory test lane failed with exit code $LASTEXITCODE."
}

if (-not $SkipCoverage) {
    & (Join-Path $repoRoot "scripts/quality/run-library-coverage.ps1") -LineThreshold 0 -Targets $libraryCoverageTargets -CoverageRoot $libraryCoveragePath -SummaryPath (Join-Path $coveragePath "library-coverage-summary.md") -NoRestore:$NoRestore -NoBuild:$NoBuild
    if ($LASTEXITCODE -ne 0) {
        throw "Library coverage collection failed with exit code $LASTEXITCODE."
    }
}

$summary = Write-TrxSummaryMarkdown -Title "Advisory Lane Summary" -ResultsDirectory $resultsPath -SummaryPath $summaryPath -RepoRoot $repoRoot -EmptyMessage "The advisory lane did not produce any TRX files."
Append-GitHubStepSummary -SummaryPath $summary.SummaryPath

if (-not $summary.HasResults) {
    throw "Advisory lane completed without producing TRX results."
}

Write-Host "Advisory lane completed successfully." -ForegroundColor Green
