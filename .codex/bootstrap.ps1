#!/usr/bin/env pwsh
[CmdletBinding()]
param([string]$WorkerUser = 'samuel', [switch]$RunTests, [switch]$VerifyOnly, [switch]$SkipCodexUpdate)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$DotNetRoot = '/usr/local/share/dotnet'

function Invoke-Checked([string]$File, [string[]]$Arguments = @(), [string]$WorkingDirectory = '') {
    Push-Location
    try {
        if ($WorkingDirectory) { Set-Location -LiteralPath $WorkingDirectory }
        & $File @Arguments
        if ($LASTEXITCODE -ne 0) { throw "Command failed ($LASTEXITCODE): $File $($Arguments -join ' ')" }
    }
    finally { Pop-Location }
}

function Test-Root { (& id -u) -eq '0' }
function Invoke-Root([string]$File, [string[]]$Arguments = @()) {
    if (Test-Root) { Invoke-Checked $File $Arguments } else { Invoke-Checked sudo (@($File) + $Arguments) }
}
function Get-WorkerHome {
    $entry = (& getent passwd $WorkerUser | Out-String).Trim()
    if (-not $entry) { throw "Linux user '$WorkerUser' does not exist." }
    return $entry.Split(':')[5]
}
function Invoke-Worker([string]$File, [string[]]$Arguments = @(), [string]$WorkingDirectory = '') {
    if ((& id -un) -eq $WorkerUser) { Invoke-Checked $File $Arguments $WorkingDirectory; return }
    if (-not (Test-Root)) { throw "Run as root or as '$WorkerUser'." }
    $workerHome = Get-WorkerHome
    $workerPath = "$workerHome/.local/bin:$workerHome/bin:/usr/local/bin:/usr/bin:/bin"
    Invoke-Checked runuser (@('-u', $WorkerUser, '--', 'env', "HOME=$workerHome", "PATH=$workerPath", $File) + $Arguments) $WorkingDirectory
}
function Test-WorkerCommand([string]$Command) {
    $workerHome = Get-WorkerHome
    $workerPath = "$workerHome/.local/bin:$workerHome/bin:/usr/local/bin:/usr/bin:/bin"
    if ((& id -un) -eq $WorkerUser) { & env "HOME=$workerHome" "PATH=$workerPath" bash -lc $Command *> $null }
    elseif (Test-Root) { & runuser -u $WorkerUser -- env "HOME=$workerHome" "PATH=$workerPath" bash -lc $Command *> $null }
    else { return $false }
    return $LASTEXITCODE -eq 0
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$config = Get-Content (Join-Path $PSScriptRoot 'worker.json') -Raw | ConvertFrom-Json
if ([int]$config.schemaVersion -ne 1) { throw "Unsupported worker schema version." }
$os = @{}
Get-Content /etc/os-release | ForEach-Object { if ($_ -match '^([^=]+)=(.*)$') { $os[$Matches[1]] = $Matches[2].Trim('"') } }
if ($os.ID -ne $config.platform.os -or $os.VERSION_ID.Split('.')[0] -ne [string]$config.platform.majorVersion) {
    throw "Expected $($config.platform.os) $($config.platform.majorVersion); found $($os.ID) $($os.VERSION_ID)."
}
Get-WorkerHome | Out-Null

if (-not $VerifyOnly -and @($config.dependencies.aptPackages).Count) {
    Invoke-Root apt-get @('update')
    Invoke-Root apt-get (@('install', '-y') + @($config.dependencies.aptPackages))
}

$dotnet = $config.dependencies.dotnet
$globalJsonProperty = $dotnet.PSObject.Properties['globalJson']
$installedSdks = if (Get-Command dotnet -ErrorAction SilentlyContinue) { @(dotnet --list-sdks 2>$null) } else { @() }
if ($null -ne $globalJsonProperty -and $globalJsonProperty.Value) {
    $sdk = (Get-Content (Join-Path $repoRoot ([string]$globalJsonProperty.Value)) -Raw | ConvertFrom-Json).sdk.version
    $installed = $installedSdks -match "^$([regex]::Escape($sdk))\s"
    $installArgs = @('--version', $sdk)
} else {
    $sdk = [string]$dotnet.channel
    $installed = $installedSdks -match "^$([regex]::Escape($sdk))\."
    $installArgs = @('--channel', $sdk)
}
if (-not $installed) {
    if ($VerifyOnly) { throw ".NET SDK '$sdk' is not installed." }
    $installer = Join-Path ([IO.Path]::GetTempPath()) "dotnet-$([guid]::NewGuid().ToString('N')).sh"
    try {
        Invoke-Checked curl @('-fsSL', 'https://dot.net/v1/dotnet-install.sh', '-o', $installer)
        Invoke-Root mkdir @('-p', $DotNetRoot)
        Invoke-Root bash (@($installer) + $installArgs + @('--install-dir', $DotNetRoot, '--no-path'))
        Invoke-Root ln @('-sfn', "$DotNetRoot/dotnet", '/usr/local/bin/dotnet')
    } finally { Remove-Item $installer -Force -ErrorAction SilentlyContinue }
}

$hasCodex = Test-WorkerCommand 'command -v codex >/dev/null 2>&1'
if (-not $hasCodex -or (-not $SkipCodexUpdate -and -not $VerifyOnly)) {
    if ($VerifyOnly) { throw "Codex CLI is not installed for '$WorkerUser'." }
    $installer = Join-Path ([IO.Path]::GetTempPath()) "codex-$([guid]::NewGuid().ToString('N')).sh"
    try {
        Invoke-Checked curl @('-fsSL', 'https://chatgpt.com/codex/install.sh', '-o', $installer)
        Invoke-Worker sh @($installer)
    } finally { Remove-Item $installer -Force -ErrorAction SilentlyContinue }
}

if (-not $VerifyOnly) {
    foreach ($command in @($config.commands.bootstrap)) { Invoke-Worker ([string]$command.file) @($command.args) $repoRoot }
    if ($RunTests) { foreach ($command in @($config.commands.test)) { Invoke-Worker ([string]$command.file) @($command.args) $repoRoot } }
}

if (Test-WorkerCommand 'codex login status >/dev/null 2>&1') { 'CODEX_WORKER_STATUS=ready' }
else { "Authenticate once as '$WorkerUser' with: codex login --device-auth"; 'CODEX_WORKER_STATUS=ready-needs-codex-auth' }
