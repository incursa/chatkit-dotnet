param(
    [int]$LineThreshold = 20,
    [int]$BranchThreshold = 0,
    [string]$CoverageRoot = "",
    [string]$SummaryPath = "",
    [string[]]$Targets = @("ChatKitCore", "ChatKitAspNetCore"),
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "QualityLane.Common.ps1")

Assert-DotNetAvailable

$repoRoot = Get-QualityRepoRoot
$coverageRoot = if ([string]::IsNullOrWhiteSpace($CoverageRoot)) { Join-Path $repoRoot "artifacts\coverage\libraries" } else { $CoverageRoot }
$summaryPath = if ([string]::IsNullOrWhiteSpace($SummaryPath)) { Join-Path $repoRoot "artifacts\coverage\library-coverage-summary.md" } else { $SummaryPath }
New-Item -Path $coverageRoot -ItemType Directory -Force | Out-Null

$configuredTargets = @(
    @{ Name = "ChatKitCore"; Project = "tests/Incursa.OpenAI.ChatKit.Tests/Incursa.OpenAI.ChatKit.Tests.csproj"; Filter = 'Category!=Integration&Category!=KnownIssue&RequiresDocker!=true'; Include = "[Incursa.OpenAI.ChatKit]*" },
    @{ Name = "ChatKitAspNetCore"; Project = "tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests/Incursa.OpenAI.ChatKit.AspNetCore.Tests.csproj"; Filter = 'Category!=Integration&Category!=KnownIssue&RequiresDocker!=true'; Include = "[Incursa.OpenAI.ChatKit.AspNetCore]*" }
)

$selectedTargets = @()
foreach ($targetName in $Targets) {
    $matched = $configuredTargets | Where-Object { $_.Name -eq $targetName } | Select-Object -First 1
    if ($null -eq $matched) {
        Write-Error "Unknown coverage target '$targetName'."
        exit 1
    }

    $selectedTargets += $matched
}

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Library Coverage Summary")
$summary.Add("")
$summary.Add("| Target | Filter | Result |")
$summary.Add("| --- | --- | --- |")

$failures = 0
$threshold = "$LineThreshold"
$thresholdType = "line"

if ($BranchThreshold -gt 0) {
    $threshold = "$LineThreshold,$BranchThreshold"
    $thresholdType = "line,branch"
}

foreach ($target in $selectedTargets) {
    $name = $target.Name
    $project = $target.Project
    $filter = $target.Filter
    $include = $target.Include

    $coverageOutputPrefix = Join-Path $coverageRoot $name
    Write-Host "Running library coverage gate for $name..."
    try {
        $projectPath = Resolve-RepoPath -RepoRoot $repoRoot -Path $project
        if (-not $NoRestore) {
            & dotnet restore $projectPath
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet restore failed for $name with exit code $LASTEXITCODE."
            }
        }

        if (-not $NoBuild) {
            & dotnet build $projectPath --configuration Release --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet build failed for $name with exit code $LASTEXITCODE."
            }
        }

        $dotnetArgs = @(
            "test"
            $projectPath
            "--configuration"
            "Release"
            "--no-build"
            "--no-restore"
            "/p:CollectCoverage=true"
            "/p:CoverletOutput=$coverageOutputPrefix"
            "/p:CoverletOutputFormat=cobertura"
            "/p:Threshold=$threshold"
            "/p:ThresholdType=$thresholdType"
            "/p:ThresholdStat=total"
            "/p:Include=$include"
        )

        if (-not [string]::IsNullOrWhiteSpace($filter)) {
            $dotnetArgs += @("--filter", $filter)
        }

        $output = dotnet @dotnetArgs 2>&1

        $noMatchingTests = $false
        foreach ($line in $output) {
            if ($line -match "No test matches the given testcase filter") {
                $noMatchingTests = $true
                break
            }
        }

        if ($noMatchingTests) {
            throw "No matching tests found for $name coverage filter '$filter'."
        }

        $coverageFiles = @(
            Get-ChildItem -Path $coverageRoot -Filter "$name*.cobertura.xml" -ErrorAction SilentlyContinue
        )
        if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
            throw "Coverage output file was not generated for ${name} under $coverageRoot"
        }

        if ($BranchThreshold -gt 0) {
            $summary.Add("| $name | $filter | Passed (line >= $LineThreshold, branch >= $BranchThreshold) |")
        } else {
            $summary.Add("| $name | $filter | Passed (line >= $LineThreshold) |")
        }
    } catch {
        $failures++
        $summary.Add("| $name | $filter | Failed coverage gate |")
        Write-Host "Coverage gate failed for ${name}: $($_.Exception.Message)"
    }
}

$summary | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host "Coverage summary written to $summaryPath"

if ($failures -gt 0) {
    exit 1
}
