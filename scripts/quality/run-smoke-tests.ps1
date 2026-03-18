param(
    [string]$Solution = "Incursa.OpenAI.ChatKit.slnx",
    [string]$Configuration = "Release",
    [string]$Runsettings = "runsettings/smoke.runsettings",
    [string]$ResultsDirectory = "artifacts/test-results/smoke",
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

$projects = @(
    "tests/Incursa.OpenAI.ChatKit.Tests/Incursa.OpenAI.ChatKit.Tests.csproj",
    "tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests.csproj"
)

Write-Host "Running smoke lane..." -ForegroundColor Cyan
Write-Host "Solution: $solutionPath" -ForegroundColor Yellow
Write-Host "Runsettings: $runsettingsPath" -ForegroundColor Yellow
Write-Host "Results: $resultsPath" -ForegroundColor Yellow

Initialize-ArtifactDirectory -Path $resultsPath -Clean | Out-Null
Invoke-TestPrerequisites -Solution $solutionPath -Configuration $Configuration -NoRestore:$NoRestore -NoBuild:$NoBuild

foreach ($project in $projects) {
    $projectPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $project
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
    $testArgs = @(
        "test"
        $projectPath
        "--configuration"
        $Configuration
        "--settings"
        $runsettingsPath
        "--results-directory"
        $resultsPath
        "--logger"
        "trx;LogFileName=$projectName.trx"
        "--no-build"
        "--no-restore"
    )

    Write-Host "Testing $projectName..." -ForegroundColor Cyan
    & dotnet @testArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Smoke lane failed for $projectName with exit code $LASTEXITCODE."
    }
}

$summary = Write-TrxSummaryMarkdown -Title "Smoke Lane Summary" -ResultsDirectory $resultsPath -SummaryPath $summaryPath -RepoRoot $repoRoot -EmptyMessage "The smoke lane did not produce any TRX files."
Append-GitHubStepSummary -SummaryPath $summary.SummaryPath

if (-not $summary.HasResults) {
    throw "Smoke lane completed without producing TRX results."
}

Write-Host "Smoke lane completed successfully." -ForegroundColor Green
