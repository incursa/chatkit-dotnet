Set-StrictMode -Version Latest

function Assert-DotNetAvailable {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "dotnet is required but was not found on PATH."
    }
}

function Get-QualityRepoRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path $RepoRoot $Path)
}

function Initialize-ArtifactDirectory {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$Clean
    )

    if ($Clean -and (Test-Path $Path)) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -Path $Path -ItemType Directory -Force | Out-Null
    return (Resolve-Path $Path).Path
}

function Invoke-TestPrerequisites {
    param(
        [Parameter(Mandatory)]
        [string]$Solution,

        [Parameter(Mandatory)]
        [string]$Configuration,

        [switch]$NoRestore,

        [switch]$NoBuild
    )

    if (-not $NoRestore) {
        Write-Host "Restoring $Solution..." -ForegroundColor Cyan
        & dotnet restore $Solution
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }
    }

    if (-not $NoBuild) {
        $buildArgs = @(
            "build"
            $Solution
            "--configuration"
            $Configuration
            "--no-restore"
        )

        Write-Host "Building $Solution..." -ForegroundColor Cyan
        & dotnet @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE."
        }
    }
}

function Get-TrxFiles {
    param(
        [Parameter(Mandatory)]
        [string]$ResultsDirectory
    )

    if (-not (Test-Path $ResultsDirectory)) {
        return @()
    }

    return @(
        Get-ChildItem -Path $ResultsDirectory -Filter *.trx -File -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName
    )
}

function Get-RelativeArtifactPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [Parameter(Mandatory)]
        [string]$Path
    )

    if ($Path.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($RepoRoot.Length).TrimStart('\', '/')
    }

    return $Path
}

function Get-LaneOutcomeDisplay {
    param(
        [string]$Outcome
    )

    if ([string]::IsNullOrWhiteSpace($Outcome)) {
        return "[?] unknown"
    }

    $display = switch ($Outcome.Trim().ToLowerInvariant()) {
        "passed" { "[ok] passed" }
        "completed" { "[ok] completed" }
        "failed" { "[fail] failed" }
        "error" { "[fail] error" }
        "timeout" { "[time] timeout" }
        "aborted" { "[stop] aborted" }
        "partial" { "[warn] partial" }
        "warning" { "[warn] warning" }
        "notexecuted" { "[hold] not executed" }
        default { "[info] $Outcome" }
    }

    return $display
}

function Format-LaneCountSummary {
    param(
        [int]$Total,
        [int]$Passed,
        [int]$Failed,
        [int]$Skipped,
        [int]$NotExecuted
    )

    $segments = New-Object System.Collections.Generic.List[string]
    $segments.Add(("[total] {0}" -f $Total))
    $segments.Add(("[ok] {0}" -f $Passed))
    $segments.Add(("[fail] {0}" -f $Failed))

    if ($Skipped -gt 0) {
        $segments.Add(("[skip] {0}" -f $Skipped))
    }

    if ($NotExecuted -gt 0) {
        $segments.Add(("[hold] {0}" -f $NotExecuted))
    }

    return [string]::Join("  ", $segments)
}

function Write-TrxSummaryMarkdown {
    param(
        [Parameter(Mandatory)]
        [string]$Title,

        [Parameter(Mandatory)]
        [string]$ResultsDirectory,

        [Parameter(Mandatory)]
        [string]$SummaryPath,

        [Parameter(Mandatory)]
        [string]$RepoRoot,

        [string]$EmptyMessage = "No .trx files were produced."
    )

    $relativeResultsDirectory = Get-RelativeArtifactPath -RepoRoot $RepoRoot -Path $ResultsDirectory

    $summaryLines = New-Object System.Collections.Generic.List[string]
    $summaryLines.Add("# $Title")
    $summaryLines.Add("")
    $summaryLines.Add(('- Results directory: `{0}`' -f $relativeResultsDirectory))

    $trxFiles = @(Get-TrxFiles -ResultsDirectory $ResultsDirectory)

    if ($trxFiles.Count -eq 0) {
        $summaryLines.Add("- Outcome: [info] $EmptyMessage")
        $summaryLines | Set-Content -Path $SummaryPath -Encoding UTF8

        return [pscustomobject]@{
            HasResults  = $false
            Total       = 0
            Passed      = 0
            Failed      = 0
            Skipped     = 0
            NotExecuted = 0
            SummaryPath = $SummaryPath
        }
    }

    $total = 0
    $passed = 0
    $failed = 0
    $skipped = 0
    $notExecuted = 0

    $summaryLines.Add("- TRX files: $($trxFiles.Count)")
    $summaryLines.Add("")
    $summaryLines.Add("| File | Outcome | Counts |")
    $summaryLines.Add("| --- | --- | --- |")

    foreach ($trxFile in $trxFiles) {
        $relativePath = Get-RelativeArtifactPath -RepoRoot $RepoRoot -Path $trxFile.FullName

        try {
            [xml]$trx = Get-Content -LiteralPath $trxFile.FullName
            $counters = $trx.TestRun.ResultSummary.Counters
            $outcome = [string]$trx.TestRun.ResultSummary.outcome

            $fileTotal = [int]$counters.total
            $filePassed = [int]$counters.passed
            $fileFailed = [int]$counters.failed
            $skippedAttribute = $counters.PSObject.Properties["skipped"]
            $fileSkipped = if ($null -ne $skippedAttribute -and -not [string]::IsNullOrWhiteSpace([string]$skippedAttribute.Value)) { [int]$skippedAttribute.Value } else { 0 }
            $fileNotExecuted = [int]$counters.notExecuted

            $total += $fileTotal
            $passed += $filePassed
            $failed += $fileFailed
            $skipped += $fileSkipped
            $notExecuted += $fileNotExecuted

            $summaryLines.Add(('| `{0}` | {1} | {2} |' -f $relativePath, (Get-LaneOutcomeDisplay -Outcome $outcome), (Format-LaneCountSummary -Total $fileTotal -Passed $filePassed -Failed $fileFailed -Skipped $fileSkipped -NotExecuted $fileNotExecuted)))
        } catch {
            $summaryLines.Add(('| `{0}` | unreadable | unable to parse .trx |' -f $relativePath))
        }
    }

    $summaryLines.Insert(3, ("- Totals: {0}" -f (Format-LaneCountSummary -Total $total -Passed $passed -Failed $failed -Skipped $skipped -NotExecuted $notExecuted)))
    $summaryLines | Set-Content -Path $SummaryPath -Encoding UTF8

    return [pscustomobject]@{
        HasResults  = $true
        Total       = $total
        Passed      = $passed
        Failed      = $failed
        Skipped     = $skipped
        NotExecuted = $notExecuted
        SummaryPath = $SummaryPath
    }
}

function Append-GitHubStepSummary {
    param(
        [Parameter(Mandatory)]
        [string]$SummaryPath
    )

    if ([string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY) -or -not (Test-Path $SummaryPath)) {
        return
    }

    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value (Get-Content -Path $SummaryPath -Raw)
    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value [Environment]::NewLine
}
