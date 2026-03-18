param(
    [string]$Solution = "Incursa.OpenAI.ChatKit.slnx",
    [string]$Configuration = "Release",
    [string]$Runsettings = "runsettings/observational.runsettings",
    [string]$ResultsDirectory = "artifacts/test-results/observational",
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "QualityLane.Common.ps1")

Assert-DotNetAvailable

$repoRoot = Get-QualityRepoRoot
$solutionPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $Solution
$runsettingsPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $Runsettings
$resultsPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $ResultsDirectory
$summaryPath = Join-Path $resultsPath "summary.md"

Write-Host "Running observational lane..." -ForegroundColor Cyan
Write-Host "Solution: $solutionPath" -ForegroundColor Yellow
Write-Host "Runsettings: $runsettingsPath" -ForegroundColor Yellow
Write-Host "Results: $resultsPath" -ForegroundColor Yellow

Initialize-ArtifactDirectory -Path $resultsPath -Clean | Out-Null
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
$testExitCode = $LASTEXITCODE

if ($testExitCode -ne 0) {
    Write-Host "Observational lane reported failures (exit code $testExitCode). This lane is non-blocking by design." -ForegroundColor Yellow
}

$summary = Write-TrxSummaryMarkdown -Title "Observational Lane Summary" -ResultsDirectory $resultsPath -SummaryPath $summaryPath -RepoRoot $repoRoot -EmptyMessage "No KnownIssue tests matched the observational lane in this run."
Append-GitHubStepSummary -SummaryPath $summary.SummaryPath

if (-not $summary.HasResults -or $summary.Total -eq 0) {
    Write-Host "No KnownIssue tests are currently mapped into the observational lane." -ForegroundColor Yellow
}

exit 0
