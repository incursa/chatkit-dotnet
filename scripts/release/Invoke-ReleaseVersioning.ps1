[CmdletBinding()]
param(
    [string]$Version,
    [string]$ProjectsRoot = "src",
    [string]$PropsPath = "Directory.Build.props",
    [string]$FirstReleaseVersion = "1.0.12",
    [string]$CommitMessagePrefix = "chore(release):",
    [switch]$NoCommit,
    [switch]$NoTag,
    [switch]$NoPush
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$propsFullPath = Join-Path $repoRoot $PropsPath

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $output = & git -C $repoRoot @Arguments 2>&1
    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        $joined = $Arguments -join " "
        $message = ($output | Out-String).Trim()
        throw "git $joined failed.`n$message"
    }

    return $output
}

function Get-VersionFromProps {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        throw "Version props file was not found: $Path"
    }

    $content = Get-Content -Path $Path -Raw -Encoding UTF8
    $match = [regex]::Match($content, '<Version>(?<version>\d+\.\d+\.\d+)</Version>')
    if (-not $match.Success) {
        throw "Could not locate a <Version>x.y.z</Version> element in $Path"
    }

    return [version]$match.Groups["version"].Value
}

function Set-VersionInProps {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [version]$TargetVersion
    )

    $content = Get-Content -Path $Path -Raw -Encoding UTF8
    $updated = [regex]::Replace(
        $content,
        '<Version>\d+\.\d+\.\d+</Version>',
        "<Version>$TargetVersion</Version>",
        1)

    if ($updated -eq $content) {
        throw "Failed to update <Version> in $Path"
    }

    Set-Content -Path $Path -Value $updated -Encoding UTF8
}

function Get-PublicApiDeltaKind {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CurrentShippedPath,
        [string]$PriorTag
    )

    if (-not (Test-Path $CurrentShippedPath)) {
        return "MissingCurrent"
    }

    $currentLines = (Get-Content -Path $CurrentShippedPath -Raw -Encoding UTF8) -split "`r?`n"
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
    if ($relativePath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
        $relativePath = $relativePath.Substring($repoRoot.Length).TrimStart('\', '/')
    }
    $relativePath = $relativePath.Replace("\", "/")

    $priorContent = Invoke-Git -Arguments @("show", "$PriorTag`:$relativePath") -AllowFailure
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

function Get-RequiredVersionBump {
    param(
        [string]$PriorTag,
        [string]$ProjectsRootPath
    )

    $projects = Get-ChildItem -Path $ProjectsRootPath -Recurse -Filter "*.csproj" -File |
        Where-Object {
            $_.FullName -notmatch "[\\/]samples[\\/]" -and
            $_.FullName -notmatch "[\\/]tests[\\/]" -and
            (Test-Path (Join-Path $_.DirectoryName "PublicAPI.Shipped.txt"))
        }

    $requiredBump = "Patch"
    foreach ($project in $projects) {
        $delta = Get-PublicApiDeltaKind -CurrentShippedPath (Join-Path $project.DirectoryName "PublicAPI.Shipped.txt") -PriorTag $PriorTag
        switch ($delta) {
            "Breaking" { return "Major" }
            "Additive" { $requiredBump = "Minor" }
        }
    }

    return $requiredBump
}

function Get-LatestReleaseTag {
    $tags = Invoke-Git -Arguments @("tag", "--list", "v*")
    if (-not $tags) {
        return $null
    }

    $releaseTags = foreach ($tag in $tags) {
        if ($tag -match '^v(?<version>\d+\.\d+\.\d+)$') {
            [pscustomobject]@{
                Name = $tag
                Version = [version]$Matches.version
            }
        }
    }

    return $releaseTags | Sort-Object Version | Select-Object -Last 1
}

function Get-NextVersion {
    param(
        [version]$BaseVersion,
        [string]$RequiredBump,
        [version]$MinimumVersion
    )

    $candidate = switch ($RequiredBump) {
        "Major" { [version]::new($BaseVersion.Major + 1, 0, 0) }
        "Minor" { [version]::new($BaseVersion.Major, $BaseVersion.Minor + 1, 0) }
        default { [version]::new($BaseVersion.Major, $BaseVersion.Minor, $BaseVersion.Build + 1) }
    }

    if ($candidate -lt $MinimumVersion) {
        return $MinimumVersion
    }

    return $candidate
}

function Get-HeadReleaseTags {
    $tags = Invoke-Git -Arguments @("tag", "--points-at", "HEAD")
    if (-not $tags) {
        return @()
    }

    return @($tags | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' })
}

$currentVersion = Get-VersionFromProps -Path $propsFullPath
$minimumVersion = [version]$FirstReleaseVersion
$latestReleaseTag = Get-LatestReleaseTag
$priorTagName = $null
$priorTagVersion = $null
if ($null -ne $latestReleaseTag) {
    $priorTagName = $latestReleaseTag.Name
    $priorTagVersion = $latestReleaseTag.Version
}

if ($Version) {
    $targetVersion = [version]$Version
    $requiredBump = "Explicit"
}
else {
    if ($priorTagVersion) {
        $baseVersion = if ($currentVersion -gt $priorTagVersion) { $currentVersion } else { $priorTagVersion }
    }
    else {
        $baseVersion = $currentVersion
    }

    $requiredBump = Get-RequiredVersionBump -PriorTag $priorTagName -ProjectsRootPath (Join-Path $repoRoot $ProjectsRoot)
    $targetVersion = Get-NextVersion -BaseVersion $baseVersion -RequiredBump $requiredBump -MinimumVersion $minimumVersion
}

if ($targetVersion -lt $minimumVersion) {
    throw "Target version $targetVersion must be at least $minimumVersion."
}

$targetTag = "v$targetVersion"
$existingTag = Invoke-Git -Arguments @("tag", "--list", $targetTag)
if ($existingTag) {
    throw "Tag $targetTag already exists."
}

$initialStatus = Invoke-Git -Arguments @("status", "--porcelain")
$hadWorkingTreeChanges = -not [string]::IsNullOrWhiteSpace(($initialStatus | Out-String).Trim())
$headReleaseTags = @(Get-HeadReleaseTags)

if ($headReleaseTags.Count -gt 0) {
    if ($hadWorkingTreeChanges) {
        Write-Host "Current HEAD already has release tag(s): $($headReleaseTags -join ', ')."
        Write-Host "Dirty working tree detected; release versioning will continue from a new release commit."
    }
    else {
        throw "Current HEAD already has release tag(s): $($headReleaseTags -join ', ')"
    }
}

Write-Host "Current props version: $currentVersion"
Write-Host "Prior release tag: $(if ($priorTagName) { $priorTagName } else { 'none' })"
Write-Host "Required bump: $requiredBump"
Write-Host "Target release version: $targetVersion"

if ($hadWorkingTreeChanges -and $currentVersion -ne $targetVersion) {
    Set-VersionInProps -Path $propsFullPath -TargetVersion $targetVersion
    Write-Host "Updated $PropsPath to $targetVersion"
}
elseif (-not $hadWorkingTreeChanges -and $currentVersion -ne $targetVersion) {
    Write-Host "Working tree is clean; leaving $PropsPath unchanged and tagging the current commit."
}
else {
    Write-Host "$PropsPath already matches $targetVersion"
}

& pwsh -NoProfile -File (Join-Path $PSScriptRoot "validate-public-api-versioning.ps1") -Tag $targetTag
if ($LASTEXITCODE -ne 0) {
    throw "validate-public-api-versioning.ps1 failed for $targetTag"
}

$status = Invoke-Git -Arguments @("status", "--porcelain")
$hasChanges = -not [string]::IsNullOrWhiteSpace(($status | Out-String).Trim())

if ($hadWorkingTreeChanges -and $NoCommit -and -not $NoTag) {
    throw "Cannot create a release tag from a dirty working tree when -NoCommit is specified. Commit the release changes or also pass -NoTag."
}

if (-not $NoCommit -and $hadWorkingTreeChanges) {
    Invoke-Git -Arguments @("add", "-A") | Out-Null
    Invoke-Git -Arguments @("commit", "-m", "$CommitMessagePrefix $targetTag") | Out-Null
    Write-Host "Created release commit: $CommitMessagePrefix $targetTag"
}
elseif ($NoCommit) {
    Write-Host "Skipping commit because -NoCommit was specified."
}
else {
    Write-Host "No pre-existing working tree changes to commit."
}

if (-not $NoTag) {
    Invoke-Git -Arguments @("tag", $targetTag) | Out-Null
    Write-Host "Created git tag: $targetTag"
}
else {
    Write-Host "Skipping tag because -NoTag was specified."
}

if (-not $NoPush) {
    $branchName = (Invoke-Git -Arguments @("rev-parse", "--abbrev-ref", "HEAD")) | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($branchName) -or $branchName -eq "HEAD") {
        throw "Cannot push from a detached HEAD state."
    }

    Invoke-Git -Arguments @("push", "origin", $branchName) | Out-Null
    if (-not $NoTag) {
        Invoke-Git -Arguments @("push", "origin", $targetTag) | Out-Null
    }

    Write-Host "Pushed branch '$branchName' and $(if ($NoTag) { 'no tag' } else { "tag '$targetTag'" })."
}
else {
    Write-Host "Skipping push because -NoPush was specified."
}
