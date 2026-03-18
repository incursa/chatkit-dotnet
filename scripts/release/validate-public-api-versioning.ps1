param(
    [string]$Tag,
    [string]$ProjectsRoot = "src",
    [string]$FirstReleaseVersion = "1.0.12",
    [string]$SummaryPath = "artifacts/release/api-versioning-summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Tag)) {
    $Tag = $env:GITHUB_REF_NAME
}

if ([string]::IsNullOrWhiteSpace($Tag)) {
    Write-Error "Release tag was not provided. Use -Tag or set GITHUB_REF_NAME."
    exit 1
    }

$Tag = $Tag.Trim()
if ($Tag.StartsWith("refs/tags/")) {
    $Tag = $Tag.Substring("refs/tags/".Length)
}

if (-not ($Tag -match '^v(?<version>\d+\.\d+\.\d+)$')) {
    Write-Error "Tag '$Tag' must follow v<major>.<minor>.<patch>."
    exit 1
}

$releaseVersion = [version]$Matches.version
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$artifactsDir = Join-Path $repoRoot "artifacts/release"
New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

function Get-PublicApiDeltaKind {
    param(
        [string]$CurrentShippedPath,
        [string]$PriorTag
    )

    if (-not (Test-Path $CurrentShippedPath)) {
        return "MissingCurrent"
    }

    $currentLines = (Get-Content -Path $CurrentShippedPath -Raw -ErrorAction SilentlyContinue -Encoding UTF8) -split "`r?`n"
    $currentSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $hasCurrentLine = $false

    foreach ($line in $currentLines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        $hasCurrentLine = $true
        [void]$currentSet.Add($line.Trim())
    }

    if (-not $hasCurrentLine) {
        return "EmptyCurrent"
    }

    if (-not $PriorTag) {
        return "InitialRelease"
    }

    $relativePath = (Resolve-Path $CurrentShippedPath).Path
    if ($relativePath.StartsWith($repoRoot)) {
        $relativePath = $relativePath.Substring($repoRoot.Length).TrimStart('\','/')
    }
    $relativePath = $relativePath.Replace("\", "/")

    $priorContent = & git -C $repoRoot show "$PriorTag`:$relativePath" 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $priorContent) {
        return "InitialRelease"
    }

    $priorLines = $priorContent -split "`r?`n"
    $priorSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $priorFilteredCount = 0
    foreach ($line in $priorLines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        [void]$priorSet.Add($line.Trim())
        $priorFilteredCount++
    }

    if ($priorFilteredCount -eq 0) {
        return "InitialRelease"
    }

    $added = 0
    $removed = 0

    foreach ($line in $currentSet) {
        if (-not $priorSet.Contains($line)) {
            $added++
        }
    }

    foreach ($line in $priorSet) {
        if (-not $currentSet.Contains($line)) {
            $removed++
        }
    }

    if ($removed -gt 0) {
        return "Breaking"
    }

    if ($added -gt 0) {
        return "Additive"
    }

    return "NoChange"
}

$priorTag = $null
$previousTags = & git -C $repoRoot tag --list
if ($LASTEXITCODE -eq 0 -and $previousTags) {
    $releaseTags = @()
    foreach ($existingTag in $previousTags) {
        if ($existingTag -match '^v(?<version>\d+\.\d+\.\d+)$') {
            $releaseTags += [pscustomobject]@{
                Name = $existingTag
                Version = [version]$Matches.version
            }
        }
    }

    $releaseTags = $releaseTags | Sort-Object Version
    $priorTag = ($releaseTags | Where-Object { $_.Version -lt $releaseVersion } | Select-Object -Last 1)?.Name
}

$projects = Get-ChildItem -Path (Join-Path $repoRoot $ProjectsRoot) -Recurse -Filter "*.csproj" -File
$projects = $projects | Where-Object {
    $_.FullName -notmatch "[\\/]samples[\\/]" -and
    $_.FullName -notmatch "[\\/]tests[\\/]"
}
$projects = $projects | Where-Object { Test-Path (Join-Path $_.DirectoryName "PublicAPI.Shipped.txt") }

if (-not $projects) {
    Write-Error "No project files found under '$ProjectsRoot'."
    exit 1
}

$requiredBump = "None"
$projectSummaries = New-Object System.Collections.Generic.List[string]
$unshippedWarnings = New-Object System.Collections.Generic.List[string]

foreach ($project in $projects) {
    $shippedPath = Join-Path $project.DirectoryName "PublicAPI.Shipped.txt"
    $unshippedPath = Join-Path $project.DirectoryName "PublicAPI.Unshipped.txt"
    $delta = Get-PublicApiDeltaKind -CurrentShippedPath $shippedPath -PriorTag $priorTag
    $projectName = $project.Name.Replace(".csproj","")

    $projectSummaries.Add("$projectName => $delta")

    switch ($delta) {
        "Breaking" { $requiredBump = "Major" }
        "Additive" {
            if ($requiredBump -ne "Major") {
                $requiredBump = "Minor"
            }
        }
    }

    $priorUnshippedText = $null
    if ($priorTag -and (Test-Path $unshippedPath)) {
        $relativeUnshipped = (Resolve-Path $unshippedPath).Path
        if ($relativeUnshipped.StartsWith($repoRoot)) {
            $relativeUnshipped = $relativeUnshipped.Substring($repoRoot.Length).TrimStart('\','/')
        }
        $relativeUnshipped = $relativeUnshipped.Replace("\", "/")
        $priorUnshippedText = & git -C $repoRoot show "$priorTag`:$relativeUnshipped" 2>$null
    }

    $currentUnshipped = Get-Content $unshippedPath -Raw -ErrorAction SilentlyContinue
    $unshippedBase = if ($priorTag) { $priorTag } else { "[first tracked release]" }
    if ($currentUnshipped -ne $null -and ($priorUnshippedText -ne $currentUnshipped)) {
        $unshippedWarnings.Add("$projectName => unshipped API baselines changed since $unshippedBase")
    }
}

if ($priorTag -eq $null) {
    Write-Output "No prior release tags found. Skipping incremental semver enforcement."
    if ($releaseVersion -lt [version]$FirstReleaseVersion) {
        Write-Error "First release version must be at least $FirstReleaseVersion for this policy."
        exit 1
    }
}

if (-not ($releaseVersion -ge [version]$FirstReleaseVersion)) {
    Write-Error "First release baseline version policy requires tag '$releaseVersion' to be at least $FirstReleaseVersion."
    exit 1
}

$latestPriorVersion = if ($priorTag) {
    ($releaseTags | Where-Object { $_.Name -eq $priorTag } | Select-Object -First 1).Version
}

if ($priorTag) {
    if (-not ($releaseVersion -gt $latestPriorVersion)) {
        Write-Error "Release tag '$releaseVersion' must be greater than prior release '$latestPriorVersion' ($priorTag)."
        exit 1
    }
}

if ($requiredBump -eq "Major" -and $priorTag) {
    if ($releaseVersion.Major -le $latestPriorVersion.Major) {
        Write-Error "Public API shipped changes are breaking. Version must include a major increment."
        exit 1
    }
}

if ($requiredBump -eq "Minor" -and $priorTag) {
    if ($releaseVersion.Major -ne $latestPriorVersion.Major -or $releaseVersion.Minor -le $latestPriorVersion.Minor) {
        Write-Error "Public API shipped additions are detected. Version must include a minor increment."
        exit 1
    }
}

Write-Host "Release version check summary (tag: $Tag):"
foreach ($item in $projectSummaries) {
    Write-Host " - $item"
}
if ($unshippedWarnings.Count -gt 0) {
    Write-Host "Unshipped API baselines changed since prior release:"
    foreach ($warning in $unshippedWarnings) {
        Write-Host " - $warning"
    }
}
else {
    Write-Host "Unshipped API baselines: no changes since prior release."
}

$summary = @()
$summary += "# Public API Versioning Check"
$summary += ""
$summary += "Tag: $Tag"
$summary += "Prior tag: $(if ($priorTag) { $priorTag } else { 'none' })"
$summary += "Required bump: $requiredBump"
$summary += ""
$summary += "## Per-project shipped baseline delta"
foreach ($item in $projectSummaries) {
    $summary += "- $item"
}
$summary | Set-Content -Path $SummaryPath -Encoding UTF8

if ($requiredBump -ne "None") {
    Write-Host "Release policy check passed for shipped API changes: $requiredBump increment required."
}

$global:LASTEXITCODE = 0
exit 0
