[CmdletBinding()]
param(
    [string]$SolutionPath = "",
    [string]$NoticePath = "NOTICE.md",
    [switch]$NoRestore,
    [switch]$UseDefaultNuGetConfig
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-MarkdownTableValue {
    param(
        [AllowEmptyString()]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return ($Value -replace '\|', '\|').Trim()
}

function Get-OptionalCollection {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Object,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName
    )

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value) {
        return @()
    }

    return @($property.Value)
}

function Get-PrimarySolutionPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,
        [AllowEmptyString()]
        [string]$RequestedPath
    )

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        return (Resolve-Path (Join-Path $RepositoryRoot $RequestedPath)).Path
    }

    $candidates = @(@(
        Get-ChildItem -Path $RepositoryRoot -File -Filter *.slnx -ErrorAction SilentlyContinue
        Get-ChildItem -Path $RepositoryRoot -File -Filter *.sln -ErrorAction SilentlyContinue
    ) | Sort-Object FullName -Unique)

    if ($candidates.Count -eq 0) {
        throw "No solution file was found under $RepositoryRoot."
    }

    $preferred = $candidates | Where-Object { $_.Name -notmatch '\.CI\.' } | Select-Object -First 1
    if ($null -ne $preferred) {
        return $preferred.FullName
    }

    return ($candidates | Select-Object -First 1).FullName
}

function Get-NormalizedNoticeContent {
    param(
        [AllowEmptyString()]
        [string]$Content
    )

    if ([string]::IsNullOrWhiteSpace($Content)) {
        return ""
    }

    $normalized = $Content -replace "`r`n", "`n"
    $lines = $normalized -split "`n" | Where-Object { $_ -notmatch '^- Generated: ' }
    return ($lines -join "`n").Trim()
}

function Get-PackageMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $packageIdLower = $PackageId.ToLowerInvariant()
    $versionLower = $Version.ToLowerInvariant()
    $nuspecPath = Join-Path $script:GlobalPackagesPath "$packageIdLower\$versionLower\$packageIdLower.nuspec"
    $nuspecContent = $null

    if (Test-Path $nuspecPath) {
        $nuspecContent = Get-Content $nuspecPath -Raw
    }
    else {
        $nuspecUrl = "https://api.nuget.org/v3-flatcontainer/$packageIdLower/$versionLower/$packageIdLower.nuspec"

        try {
            $response = Invoke-WebRequest -Uri $nuspecUrl -UseBasicParsing
            $nuspecContent = $response.Content
        }
        catch {
            return [pscustomobject]@{
                ProjectUrl  = ""
                LicenseType = "unknown"
                License     = ""
            }
        }
    }

    [xml]$nuspec = $nuspecContent
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($nuspec.NameTable)
    $namespaceManager.AddNamespace("n", $nuspec.DocumentElement.NamespaceURI)
    $metadataNode = $nuspec.SelectSingleNode("/n:package/n:metadata", $namespaceManager)

    if ($null -eq $metadataNode) {
        throw "NuGet metadata for $PackageId $Version did not contain a <metadata> element."
    }

    $projectUrlNode = $metadataNode.SelectSingleNode("n:projectUrl", $namespaceManager)
    $licenseNode = $metadataNode.SelectSingleNode("n:license", $namespaceManager)
    $licenseUrlNode = $metadataNode.SelectSingleNode("n:licenseUrl", $namespaceManager)

    $licenseType = "unknown"
    $licenseValue = ""

    if ($null -ne $licenseNode -and -not [string]::IsNullOrWhiteSpace($licenseNode.InnerText)) {
        $licenseValue = $licenseNode.InnerText.Trim()
        $typeAttribute = $licenseNode.Attributes["type"]
        if ($null -ne $typeAttribute -and -not [string]::IsNullOrWhiteSpace($typeAttribute.Value)) {
            $licenseType = $typeAttribute.Value.Trim()
        }
    }
    elseif ($null -ne $licenseUrlNode -and -not [string]::IsNullOrWhiteSpace($licenseUrlNode.InnerText)) {
        $licenseType = "url"
        $licenseValue = $licenseUrlNode.InnerText.Trim()
    }

    [pscustomobject]@{
        ProjectUrl  = if ($null -ne $projectUrlNode) { $projectUrlNode.InnerText.Trim() } else { "" }
        LicenseType = $licenseType
        License     = $licenseValue
    }
}

function Get-LicenseDisplay {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Metadata
    )

    switch ($Metadata.LicenseType) {
        "expression" { return Get-MarkdownTableValue -Value $Metadata.License }
        "url" {
            if ([string]::IsNullOrWhiteSpace($Metadata.License)) {
                return "Unknown"
            }

            return "[link]($($Metadata.License))"
        }
        "file" {
            if ([string]::IsNullOrWhiteSpace($Metadata.License)) {
                return "Embedded package license file"
            }

            return "Embedded file: ``$($Metadata.License)``"
        }
        default {
            return "Unknown"
        }
    }
}

function Get-AdditionalNoticeFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,
        [Parameter(Mandatory = $true)]
        [string]$NoticeFullPath
    )

    $allNotices = @(Get-ChildItem -Path $RepositoryRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -ieq 'NOTICE.md' -and
            $_.FullName -ne $NoticeFullPath -and
            $_.FullName -notmatch '[\\/](artifacts|bin|obj|\.git)[\\/]'
        } |
        ForEach-Object {
            [System.IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName).Replace('\', '/')
        } |
        Sort-Object -Unique)

    return @($allNotices)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$script:GlobalPackagesPath = [System.Environment]::GetFolderPath("UserProfile")
$script:GlobalPackagesPath = Join-Path $script:GlobalPackagesPath ".nuget\packages"
$solutionFullPath = Get-PrimarySolutionPath -RepositoryRoot $repoRoot -RequestedPath $SolutionPath
$solutionName = Split-Path -Leaf $solutionFullPath
$noticeFullPath = Join-Path $repoRoot $NoticePath
$artifactDir = Join-Path $repoRoot "artifacts\notice"

New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

$tempNuGetConfigPath = Join-Path $artifactDir "notice.nuget.config"
if (-not $UseDefaultNuGetConfig) {
    @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@ | Set-Content -Path $tempNuGetConfigPath -Encoding utf8
}

Push-Location $repoRoot
try {
    if (-not $NoRestore) {
        $restoreArgs = @("restore", $solutionFullPath)
        if (-not $UseDefaultNuGetConfig) {
            $restoreArgs += @("--configfile", $tempNuGetConfigPath)
        }

        Invoke-DotNet -Arguments $restoreArgs
    }

    $listArgs = @(
        "package",
        "list",
        "--project",
        $solutionFullPath,
        "--include-transitive",
        "--format",
        "json",
        "--no-restore"
    )

    $packageListJson = & dotnet @listArgs | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($listArgs -join ' ') failed with exit code $LASTEXITCODE."
    }

    $packageListPath = Join-Path $artifactDir "package-list.json"
    $packageListJson | Set-Content -Path $packageListPath -Encoding utf8

    $packageGraph = $packageListJson | ConvertFrom-Json -Depth 100
    $packages = @{}

    foreach ($project in $packageGraph.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in (Get-OptionalCollection -Object $framework -PropertyName "topLevelPackages")) {
                $key = "$($package.id)|$($package.resolvedVersion)".ToLowerInvariant()
                if (-not $packages.ContainsKey($key)) {
                    $packages[$key] = [ordered]@{
                        Id      = $package.id
                        Version = $package.resolvedVersion
                        Scopes  = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
                    }
                }

                [void]$packages[$key].Scopes.Add("Direct")
            }

            foreach ($package in (Get-OptionalCollection -Object $framework -PropertyName "transitivePackages")) {
                $key = "$($package.id)|$($package.resolvedVersion)".ToLowerInvariant()
                if (-not $packages.ContainsKey($key)) {
                    $packages[$key] = [ordered]@{
                        Id      = $package.id
                        Version = $package.resolvedVersion
                        Scopes  = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
                    }
                }

                [void]$packages[$key].Scopes.Add("Transitive")
            }
        }
    }

    $packageRows = foreach ($entry in $packages.Values) {
        $metadata = Get-PackageMetadata -PackageId $entry.Id -Version $entry.Version
        $nugetUrl = "https://www.nuget.org/packages/$($entry.Id)/$($entry.Version)"
        $scope = ($entry.Scopes | Sort-Object) -join ", "
        $projectUrl = if ([string]::IsNullOrWhiteSpace($metadata.ProjectUrl)) { "" } else { "[link]($($metadata.ProjectUrl))" }

        [pscustomobject]@{
            Id         = $entry.Id
            Version    = $entry.Version
            Scope      = $scope
            License    = Get-LicenseDisplay -Metadata $metadata
            ProjectUrl = $projectUrl
            NuGetUrl   = "[package]($nugetUrl)"
        }
    }

    $additionalNoticeFiles = @(Get-AdditionalNoticeFiles -RepositoryRoot $repoRoot -NoticeFullPath $noticeFullPath)
    $generatedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# NOTICE")
    $lines.Add("")
    $lines.Add('This file is generated by `scripts/compliance/update-notice.ps1`.')
    $lines.Add("")
    $lines.Add("It inventories the direct and transitive NuGet packages resolved for ``$solutionName``.")
    $lines.Add("")
    $lines.Add("- Generated: $generatedAt")
    $lines.Add('- Package source for restore and metadata lookup: `nuget.org`')
    $lines.Add("")

    if ($additionalNoticeFiles.Count -gt 0) {
        $lines.Add("## Additional Notice Files")
        $lines.Add("")
        foreach ($relativePath in $additionalNoticeFiles) {
            $lines.Add("- [``$relativePath``]($relativePath)")
        }
        $lines.Add("")
    }

    $lines.Add("| Package | Version | Scope | License | Project | NuGet |")
    $lines.Add("| --- | --- | --- | --- | --- | --- |")

    foreach ($row in ($packageRows | Sort-Object Id, Version)) {
        $packageCell = Get-MarkdownTableValue -Value $row.Id
        $versionCell = Get-MarkdownTableValue -Value $row.Version
        $scopeCell = Get-MarkdownTableValue -Value $row.Scope
        $licenseCell = Get-MarkdownTableValue -Value $row.License
        $projectCell = Get-MarkdownTableValue -Value $row.ProjectUrl
        $nugetCell = Get-MarkdownTableValue -Value $row.NuGetUrl

        $lines.Add("| $packageCell | $versionCell | $scopeCell | $licenseCell | $projectCell | $nugetCell |")
    }

    $lines.Add("")
    $lines.Add("Packages that expose only a license URL or an embedded package license file are recorded as such.")
    $lines.Add("If a package reports `Unknown`, inspect its NuGet gallery page and package payload before redistribution.")

    $newContent = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
    $existingContent = if (Test-Path $noticeFullPath) { Get-Content $noticeFullPath -Raw } else { "" }

    if ((Get-NormalizedNoticeContent -Content $existingContent) -eq (Get-NormalizedNoticeContent -Content $newContent)) {
        Write-Host "NOTICE content is unchanged aside from the generated timestamp."
        return
    }

    $newContent | Set-Content -Path $noticeFullPath -Encoding utf8
}
finally {
    Pop-Location
}
