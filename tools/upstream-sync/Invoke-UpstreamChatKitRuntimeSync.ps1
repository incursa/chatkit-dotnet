[CmdletBinding()]
param(
    [string]$RuntimePath,
    [switch]$SkipTests,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
. (Join-Path $ScriptRoot 'UpstreamSync.Common.ps1')

$RepoRoot = Split-Path -Parent $ScriptRoot
Set-Location $RepoRoot

if (-not $RuntimePath) {
    $RuntimePath = Join-Path $RepoRoot 'src/Incursa.OpenAI.ChatKit.AspNetCore/ClientApp/chatkit-runtime'
}

$PackageJsonPath = Join-Path $RuntimePath 'package.json'
$PackageLockPath = Join-Path $RuntimePath 'package-lock.json'

Ensure-Tool 'git'
Ensure-Tool 'npm'

if (-not (Test-Path $RuntimePath)) {
    throw "ChatKit runtime path '$RuntimePath' does not exist."
}

if (-not (Test-Path $PackageJsonPath)) {
    throw "ChatKit runtime package.json was not found at '$PackageJsonPath'."
}

if (-not (Test-Path $PackageLockPath)) {
    throw "ChatKit runtime package-lock.json was not found at '$PackageLockPath'."
}

function Get-LatestChatKitReactVersion {
    Write-SyncLog 'INFO' 'Checking npm for the latest @openai/chatkit-react version.'
    $latestVersion = (& npm view @openai/chatkit-react version | Select-Object -Last 1).Trim()
    if (-not $latestVersion) {
        throw 'Unable to determine the latest @openai/chatkit-react version from npm.'
    }

    return $latestVersion
}

function Get-CurrentLockedVersion {
    $packageLock = Get-Content $PackageLockPath -Raw | ConvertFrom-Json -AsHashtable
    $entry = $packageLock['packages']['node_modules/@openai/chatkit-react']
    if (-not $entry) {
        throw "Package lock entry for '@openai/chatkit-react' was not found."
    }

    if (-not $entry['version']) {
        throw "Package lock entry for '@openai/chatkit-react' did not include a version."
    }

    return $entry['version']
}

function Invoke-NpmInstall {
    param([Parameter(Mandatory)][string]$Version)

    Push-Location $RuntimePath
    try {
        Write-SyncLog 'INFO' "Updating @openai/chatkit-react to version $Version."
        & npm install "@openai/chatkit-react@$Version"
        if ($LASTEXITCODE -ne 0) {
            throw "npm install @openai/chatkit-react@$Version failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

function Invoke-NpmTests {
    Push-Location $RuntimePath
    try {
        Write-SyncLog 'INFO' 'Running ChatKit runtime npm tests.'
        & npm test
        if ($LASTEXITCODE -ne 0) {
            throw "npm test failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

function Invoke-NpmBuild {
    Push-Location $RuntimePath
    try {
        Write-SyncLog 'INFO' 'Building the packaged ChatKit runtime assets.'
        & npm run build
        if ($LASTEXITCODE -ne 0) {
            throw "npm run build failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

$currentVersion = Get-CurrentLockedVersion
$latestVersion = Get-LatestChatKitReactVersion

if ($currentVersion -eq $latestVersion) {
    Write-SyncLog 'INFO' "@openai/chatkit-react is already at the latest version ($currentVersion)."
    return
}

Write-SyncLog 'INFO' "@openai/chatkit-react will be updated from $currentVersion to $latestVersion."
Invoke-NpmInstall -Version $latestVersion

if (-not $SkipTests) {
    Invoke-NpmTests
}

if (-not $SkipBuild) {
    Invoke-NpmBuild
}

Write-SyncLog 'INFO' 'ChatKit runtime npm sync completed.'
