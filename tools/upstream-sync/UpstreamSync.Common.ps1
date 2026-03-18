function Write-SyncLog {
    param(
        [Parameter(Mandatory)][ValidateSet('INFO', 'WARN', 'ERROR', 'DEBUG')]
        [string]$Level,
        [Parameter(Mandatory)][string]$Message
    )

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-Host "$timestamp [$Level] $Message"
}

function Ensure-Tool {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$Name' is not available on PATH."
    }
}

function Load-JsonFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        $Default
    )

    if (Test-Path $Path) {
        return Get-Content $Path -Raw | ConvertFrom-Json
    }

    return $Default
}

function Save-JsonFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Value
    )

    $parent = Split-Path $Path -Parent
    if ($parent -and -not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $Value | ConvertTo-Json -Depth 8 | Set-Content -Path $Path -Encoding UTF8
}

function Truncate-Text {
    param(
        [Parameter(Mandatory)][string]$Text,
        [int]$MaxCharacters = 8000
    )

    if ($Text.Length -le $MaxCharacters) {
        return $Text
    }

    return $Text.Substring(0, $MaxCharacters) + "`n... [truncated at $MaxCharacters characters] ..."
}

function Get-DefaultTrackedState {
    param(
        [Parameter(Mandatory)][string]$UpstreamPath,
        [Parameter(Mandatory)][string]$UpstreamBranch,
        [string]$UpstreamRepoUrl = 'https://github.com/openai/chatkit-python.git'
    )

    return [ordered]@{
        upstreamRepoUrl = $UpstreamRepoUrl
        targetRepoUrl = 'https://github.com/incursa/chatkit-dotnet.git'
        upstreamLocalPath = $UpstreamPath
        upstreamBranch = $UpstreamBranch
        lastTranslatedSha = $null
        lastSuccessUtc = $null
    }
}

function Get-DefaultLocalState {
    return [ordered]@{
        lastAttemptedSha = $null
        lastRunUtc = $null
        bootstrapLastTranslatedSha = $null
    }
}

function Initialize-UpstreamSyncState {
    param(
        [Parameter(Mandatory)][string]$TrackedStatePath,
        [Parameter(Mandatory)][string]$LocalStatePath,
        [Parameter(Mandatory)][string]$UpstreamPath,
        [Parameter(Mandatory)][string]$UpstreamBranch,
        [string]$UpstreamRepoUrl = 'https://github.com/openai/chatkit-python.git'
    )

    $defaultTracked = Get-DefaultTrackedState -UpstreamPath $UpstreamPath -UpstreamBranch $UpstreamBranch -UpstreamRepoUrl $UpstreamRepoUrl
    $defaultLocal = Get-DefaultLocalState

    $tracked = Load-JsonFile -Path $TrackedStatePath -Default $defaultTracked
    $local = Load-JsonFile -Path $LocalStatePath -Default $defaultLocal

    foreach ($propertyName in $defaultTracked.Keys) {
        if (-not ($tracked.PSObject.Properties.Name -contains $propertyName) -or $null -eq $tracked.$propertyName) {
            $tracked | Add-Member -NotePropertyName $propertyName -NotePropertyValue $defaultTracked[$propertyName] -Force
        }
    }

    foreach ($propertyName in $defaultLocal.Keys) {
        if (-not ($local.PSObject.Properties.Name -contains $propertyName)) {
            $local | Add-Member -NotePropertyName $propertyName -NotePropertyValue $defaultLocal[$propertyName] -Force
        }
    }

    # Always honor the runtime parameters so sync prompts reflect the actual upstream source in use.
    $tracked.upstreamRepoUrl = $UpstreamRepoUrl
    $tracked.upstreamLocalPath = $UpstreamPath
    $tracked.upstreamBranch = $UpstreamBranch

    if (-not (Test-Path $TrackedStatePath)) {
        Save-JsonFile -Path $TrackedStatePath -Value $tracked
    }

    if (-not (Test-Path $LocalStatePath)) {
        Save-JsonFile -Path $LocalStatePath -Value $local
    }

    return @{
        tracked = $tracked
        local = $local
    }
}

function New-TranslationPrompt {
    param(
        [Parameter(Mandatory)][string]$BaseSha,
        [Parameter(Mandatory)][string]$LatestSha,
        [string[]]$CommitLines = @(),
        [string[]]$DiffLines = @(),
        [string[]]$ChangedFiles = @(),
        [Parameter(Mandatory)][string]$UpstreamRepoUrl,
        [Parameter(Mandatory)][string]$UpstreamBranch,
        [string]$GuidancePath,
        [string]$CodexNotesPath
    )

    $guidance = ''
    if ($GuidancePath -and (Test-Path $GuidancePath)) {
        $guidance = Get-Content $GuidancePath -Raw
    }

    $notes = ''
    if ($CodexNotesPath -and (Test-Path $CodexNotesPath)) {
        $notes = Get-Content $CodexNotesPath -Raw
    }

    $commitSection = (($CommitLines | Where-Object { $_ }) | ForEach-Object { "- $_" }) -join "`n"
    if (-not $commitSection) {
        $commitSection = '- (no commits were returned by git log)'
    }

    $filesSection = (($ChangedFiles | Where-Object { $_ }) | ForEach-Object { "- $_" }) -join "`n"
    if (-not $filesSection) {
        $filesSection = '- (no files changed in diff)'
    }

    $briefDiff = Truncate-Text -Text (($DiffLines | Where-Object { $null -ne $_ }) -join "`n") -MaxCharacters 6000

    return @"
Translate the upstream delta described below into the semantic .NET port in this repo.

Target repository: Incursa.OpenAI.ChatKit (.NET 10)
Upstream repository: $UpstreamRepoUrl
Branch: $UpstreamBranch
Range: $BaseSha..$LatestSha

Commits included:
$commitSection

Changed files:
$filesSection

Relevant diff excerpt (truncated if needed):
$briefDiff

Translation constraints:
1. Preserve exact ChatKit wire compatibility in serialized requests, responses, and stream events.
2. Translate only the behavior represented in the upstream diff; avoid unrelated refactors.
3. Keep agent-runtime concerns in Incursa.OpenAI.Agents unless ChatKit must surface them directly.
4. Prefer tests that mirror the upstream change and keep the sample/docs aligned when public behavior changes.
5. Respect the repo scope documented in docs/parity/manifest.md and docs/quality/repo-scope-boundary.md.
6. Document assumptions inline when the translation requires a .NET-specific choice.

Existing repo guidance:
$guidance

Additional notes:
$notes

End of instructions.
"@
}

function Get-SyncBranchName {
    param([Parameter(Mandatory)][string]$LatestSha)

    $shortSha = $LatestSha.Substring(0, [Math]::Min(7, $LatestSha.Length))
    return "sync/chatkit-upstream-$shortSha"
}

function Get-SyncCommitMessage {
    param([Parameter(Mandatory)][string]$LatestSha)

    $shortSha = $LatestSha.Substring(0, [Math]::Min(7, $LatestSha.Length))
    return "Sync upstream chatkit-python through $shortSha"
}

function Get-SyncPullRequestTitle {
    param([Parameter(Mandatory)][string]$LatestSha)

    $shortSha = $LatestSha.Substring(0, [Math]::Min(7, $LatestSha.Length))
    return "Sync upstream chatkit-python through $shortSha"
}

function Get-SyncPullRequestBody {
    param(
        [Parameter(Mandatory)][string]$UpstreamRepoUrl,
        [Parameter(Mandatory)][string]$BaseSha,
        [Parameter(Mandatory)][string]$LatestSha,
        [string[]]$CommitLines = @()
    )

    $commitSection = (($CommitLines | Where-Object { $_ }) | ForEach-Object { "- $_" }) -join "`n"
    if (-not $commitSection) {
        $commitSection = '- (no commits were returned by git log)'
    }

    return @"
Upstream repository: $UpstreamRepoUrl
Translated range: $BaseSha..$LatestSha

Commits translated:
$commitSection

This PR was generated by the local ChatKit upstream sync automation.
"@
}
