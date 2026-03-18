param(
    [switch]$SkipInstall,
    [switch]$SkipToolRestore,
    [switch]$SkipSolutionRestore,
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($SkipRestore) {
    $SkipToolRestore = $true
    $SkipSolutionRestore = $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$globalJsonPath = Join-Path $repoRoot 'global.json'
$toolManifestPath = Join-Path $repoRoot '.config\dotnet-tools.json'
$solutionPath = Join-Path $repoRoot 'Incursa.OpenAI.ChatKit.slnx'
$installRoot = Join-Path $repoRoot 'artifacts\dotnet'

if (-not (Test-Path $globalJsonPath)) {
    throw "global.json was not found at '$globalJsonPath'."
}

$globalJson = Get-Content $globalJsonPath -Raw | ConvertFrom-Json
$sdkVersion = [string]$globalJson.sdk.version

if ([string]::IsNullOrWhiteSpace($sdkVersion)) {
    throw "global.json does not specify an SDK version."
}

$dotnetExe = if ($IsWindows) {
    Join-Path $installRoot 'dotnet.exe'
}
else {
    Join-Path $installRoot 'dotnet'
}

function Test-SdkInstalled {
    param(
        [Parameter(Mandatory)]
        [string]$DotnetPath,
        [Parameter(Mandatory)]
        [string]$Version
    )

    if (-not (Test-Path $DotnetPath)) {
        return $false
    }

    $list = & $DotnetPath --list-sdks 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $false
    }

    return ($list -match "^\s*$( [regex]::Escape($Version) )\s")
}

if (-not $SkipInstall) {
    if (-not (Test-SdkInstalled -DotnetPath $dotnetExe -Version $sdkVersion)) {
        New-Item -ItemType Directory -Path $installRoot -Force | Out-Null

        $installer = Join-Path $env:TEMP 'dotnet-install.ps1'
        Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
        & pwsh -NoProfile -ExecutionPolicy Bypass -File $installer -Version $sdkVersion -InstallDir $installRoot -NoPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet-install.ps1 failed while installing SDK $sdkVersion."
        }
    }
}

if (-not $SkipToolRestore) {
    if (-not (Test-Path $toolManifestPath)) {
        throw "Tool manifest not found at '$toolManifestPath'."
    }

    Push-Location $repoRoot
    try {
        & $dotnetExe tool restore --tool-manifest $toolManifestPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed."
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipSolutionRestore) {
    if (-not (Test-Path $solutionPath)) {
        throw "Solution file not found at '$solutionPath'."
    }

    Push-Location $repoRoot
    try {
        & $dotnetExe restore $solutionPath
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed."
        }
    }
    finally {
        Pop-Location
    }
}
