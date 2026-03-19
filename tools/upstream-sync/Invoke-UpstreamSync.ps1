param(
    [switch]$Loop,
    [int]$IntervalMinutes = 5,
    [switch]$Once,
    [switch]$CheckOnly,
    [string]$UpstreamPath = 'C:\src\openai\chatkit-python',
    [string]$UpstreamBranch = 'main',
    [switch]$SkipPush,
    [switch]$SkipPr,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [string]$ForceFromSha,
    [switch]$AllowDirty
)

Set-StrictMode -Version Latest
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
. (Join-Path $ScriptRoot 'UpstreamSync.Common.ps1')

if ($Loop -and $Once) {
    throw 'Cannot specify both -Loop and -Once.'
}

if ($CheckOnly -and ($Loop -or $Once)) {
    throw 'Cannot specify -CheckOnly together with -Loop or -Once.'
}

if ($IntervalMinutes -lt 1) {
    $IntervalMinutes = 1
    Write-SyncLog 'WARN' 'IntervalMinutes must be >= 1; using 1 minute.'
}

$RepoRoot = Split-Path -Parent $ScriptRoot
Set-Location $RepoRoot

$TrackedStatePath = Join-Path $ScriptRoot 'state.json'
$LocalStatePath = Join-Path $ScriptRoot 'state.local.json'
$CodexNotesPath = Join-Path $ScriptRoot 'CODEX_TRANSLATION_NOTES.md'
$GuidancePath = Join-Path $RepoRoot 'AGENTS.md'

Ensure-Tool 'git'
if (-not $CheckOnly) {
    Ensure-Tool 'gh'
    Ensure-Tool 'codex'
    Ensure-Tool 'dotnet'
}

function Persist-TrackedState {
    param($State)
    Save-JsonFile -Path $TrackedStatePath -Value $State
}

function Persist-LocalState {
    param($State)
    Save-JsonFile -Path $LocalStatePath -Value $State
}

function Ensure-UpstreamRepo {
    if (-not (Test-Path $UpstreamPath)) {
        throw "Upstream path '$UpstreamPath' does not exist."
    }

    Push-Location $UpstreamPath
    try {
        & git rev-parse --is-inside-work-tree > $null
        if ($LASTEXITCODE -ne 0) {
            throw "Upstream path '$UpstreamPath' is not a git repository."
        }
    } finally {
        Pop-Location
    }
}

function Ensure-WorktreeClean {
    Push-Location $RepoRoot
    try {
        $status = & git status --porcelain
    } finally {
        Pop-Location
    }

    $dirty = $status | Where-Object { $_ -and $_ -notmatch 'tools/upstream-sync/state\.local\.json' }
    if ($dirty) {
        if (-not $AllowDirty) {
            throw 'Working tree has uncommitted changes; stash or commit them before running.'
        }

        Write-SyncLog 'WARN' 'Working tree is dirty but -AllowDirty was provided.'
    }
}

function Reset-WorkingMain {
    Push-Location $RepoRoot
    try {
        Write-SyncLog 'INFO' 'Refreshing local main branch against origin.'
        & git fetch origin main
        & git checkout -B main origin/main
    } finally {
        Pop-Location
    }
}

function Get-UpstreamLatestSha {
    Push-Location $UpstreamPath
    try {
        Write-SyncLog 'INFO' 'Fetching upstream changes.'
        & git fetch origin $UpstreamBranch
        return (& git rev-parse "origin/$UpstreamBranch").Trim()
    } finally {
        Pop-Location
    }
}

function Get-CommitLines {
    param(
        [string]$FromSha,
        [Parameter(Mandatory)][string]$ToSha
    )

    Push-Location $UpstreamPath
    try {
        if (-not $FromSha) {
            return & git log --oneline $ToSha
        }

        return & git log --oneline "$FromSha..$ToSha"
    } finally {
        Pop-Location
    }
}

function Get-UpstreamDiff {
    param(
        [string]$FromSha,
        [Parameter(Mandatory)][string]$ToSha
    )

    Push-Location $UpstreamPath
    try {
        if (-not $FromSha) {
            return & git diff $ToSha
        }

        return & git diff "$FromSha..$ToSha"
    } finally {
        Pop-Location
    }
}

function Get-ChangedFiles {
    param(
        [string]$FromSha,
        [Parameter(Mandatory)][string]$ToSha
    )

    Push-Location $UpstreamPath
    try {
        if (-not $FromSha) {
            return & git diff --name-only $ToSha
        }

        return & git diff --name-only "$FromSha..$ToSha"
    } finally {
        Pop-Location
    }
}

function Invoke-CodexTranslation {
    param([Parameter(Mandatory)][string]$PromptContent)

    $tempPrompt = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $tempPrompt -Value $PromptContent -Encoding UTF8
        Write-SyncLog 'INFO' 'Invoking Codex to translate the upstream diff.'
        $stdin = Get-Content $tempPrompt -Raw
        $result = $stdin | codex exec --dangerously-bypass-approvals-and-sandbox -C $RepoRoot --add-dir $UpstreamPath -
        Write-SyncLog 'INFO' 'Codex finished translating.'
        return $result
    } finally {
        Remove-Item $tempPrompt -ErrorAction SilentlyContinue
    }
}

function Run-DotnetCommand {
    param([Parameter(Mandatory)][string[]]$Arguments)

    Write-SyncLog 'INFO' "Running dotnet $($Arguments -join ' ')."
    Push-Location $RepoRoot
    try {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
}

function Create-SyncBranch {
    param([Parameter(Mandatory)][string]$LatestSha)

    $branchName = Get-SyncBranchName -LatestSha $LatestSha

    Push-Location $RepoRoot
    try {
        & git checkout -B main origin/main
        & git rev-parse --verify --quiet "refs/heads/$branchName" > $null
        if ($LASTEXITCODE -ne 0) {
            & git checkout -b $branchName
        } else {
            & git checkout $branchName
        }
    } finally {
        Pop-Location
    }

    return $branchName
}

function Commit-Changes {
    param(
        [Parameter(Mandatory)][string]$LatestSha
    )

    $message = Get-SyncCommitMessage -LatestSha $LatestSha
    Push-Location $RepoRoot
    try {
        & git add -A
        & git commit -m $message
    } finally {
        Pop-Location
    }
}

function Push-Branch {
    param([Parameter(Mandatory)][string]$BranchName)

    Write-SyncLog 'INFO' "Pushing branch $BranchName to origin."
    Push-Location $RepoRoot
    try {
        & git push -u origin $BranchName
    } finally {
        Pop-Location
    }
}

function Create-PullRequest {
    param(
        [Parameter(Mandatory)][string]$BranchName,
        [Parameter(Mandatory)][string]$BaseSha,
        [Parameter(Mandatory)][string]$LatestSha,
        [string[]]$CommitLines = @()
    )

    $title = Get-SyncPullRequestTitle -LatestSha $LatestSha
    $body = Get-SyncPullRequestBody -UpstreamRepoUrl $TrackedState.tracked.upstreamRepoUrl -BaseSha $BaseSha -LatestSha $LatestSha -CommitLines $CommitLines
    $tempBody = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $tempBody -Value $body -Encoding UTF8
        Push-Location $RepoRoot
        try {
            & gh pr create --title $title --body-file $tempBody --base main --head $BranchName
        } finally {
            Pop-Location
        }
    } finally {
        Remove-Item $tempBody -ErrorAction SilentlyContinue
    }
}

function Update-LocalMetadata {
    param([Parameter(Mandatory)][string]$Sha)

    $localState = $TrackedState.local
    $localState.lastAttemptedSha = $Sha
    $localState.lastRunUtc = (Get-Date).ToUniversalTime().ToString('o')
    Persist-LocalState -State $localState
}

function Run-SyncCycle {
    Ensure-UpstreamRepo
    Ensure-WorktreeClean
    Reset-WorkingMain

    $latestSha = Get-UpstreamLatestSha
    if (-not $latestSha) {
        throw 'Unable to resolve latest upstream SHA.'
    }

    $baseSha = $ForceFromSha ?? $TrackedState.tracked.lastTranslatedSha ?? $TrackedState.local.bootstrapLastTranslatedSha
    if (-not $baseSha) {
        if ($CheckOnly) {
            [ordered]@{
                status = 'bootstrap'
                baseSha = $null
                latestSha = $latestSha
                commitLines = @()
                changedFiles = @()
            } | ConvertTo-Json -Depth 6 -Compress
            return
        }

        $TrackedState.local.bootstrapLastTranslatedSha = $latestSha
        Persist-LocalState -State $TrackedState.local
        Write-SyncLog 'INFO' "Bootstrapped last translated SHA to $latestSha. Run again to translate future commits."
        Update-LocalMetadata -Sha $latestSha
        return
    }

    if ($baseSha -eq $latestSha) {
        Write-SyncLog 'INFO' 'No new upstream commits detected.'
        if ($CheckOnly) {
            [ordered]@{
                status = 'current'
                baseSha = $baseSha
                latestSha = $latestSha
                commitLines = @()
                changedFiles = @()
            } | ConvertTo-Json -Depth 6 -Compress
            return
        }

        Update-LocalMetadata -Sha $latestSha
        return
    }

    $commitLines = Get-CommitLines -FromSha $baseSha -ToSha $latestSha
    $changedFiles = Get-ChangedFiles -FromSha $baseSha -ToSha $latestSha

    if ($CheckOnly) {
        [ordered]@{
            status = 'updates-found'
            baseSha = $baseSha
            latestSha = $latestSha
            commitLines = @($commitLines)
            changedFiles = @($changedFiles)
        } | ConvertTo-Json -Depth 6 -Compress
        return
    }

    $diffLines = Get-UpstreamDiff -FromSha $baseSha -ToSha $latestSha

    try {
        $prompt = New-TranslationPrompt `
            -BaseSha $baseSha `
            -LatestSha $latestSha `
            -CommitLines $commitLines `
            -DiffLines $diffLines `
            -ChangedFiles $changedFiles `
            -UpstreamRepoUrl $TrackedState.tracked.upstreamRepoUrl `
            -UpstreamBranch $UpstreamBranch `
            -GuidancePath $GuidancePath `
            -CodexNotesPath $CodexNotesPath

        Invoke-CodexTranslation -PromptContent $prompt | Out-Null

        if (-not $SkipBuild) {
            Run-DotnetCommand -Arguments @('build', 'Incursa.OpenAI.ChatKit.slnx', '--configuration', 'Release')
        } else {
            Write-SyncLog 'INFO' 'Skipping dotnet build.'
        }

        if (-not $SkipTests) {
            Run-DotnetCommand -Arguments @('test', 'Incursa.OpenAI.ChatKit.slnx', '--configuration', 'Release')
        } else {
            Write-SyncLog 'INFO' 'Skipping dotnet test.'
        }

        $branchName = Create-SyncBranch -LatestSha $latestSha
        $TrackedState.tracked.lastTranslatedSha = $latestSha
        $TrackedState.tracked.lastSuccessUtc = (Get-Date).ToUniversalTime().ToString('o')
        Persist-TrackedState -State $TrackedState.tracked

        Commit-Changes -LatestSha $latestSha

        if (-not $SkipPush) {
            Push-Branch -BranchName $branchName
            if (-not $SkipPr) {
                Create-PullRequest -BranchName $branchName -BaseSha $baseSha -LatestSha $latestSha -CommitLines $commitLines
            } else {
                Write-SyncLog 'INFO' 'Skipping PR creation per -SkipPr.'
            }
        } else {
            Write-SyncLog 'INFO' 'Skipping push (and PR) per -SkipPush.'
        }
    } catch {
        Update-LocalMetadata -Sha $latestSha
        throw
    }

    Update-LocalMetadata -Sha $latestSha

    Push-Location $RepoRoot
    try {
        & git checkout main
    } finally {
        Pop-Location
    }
}

$TrackedState = Initialize-UpstreamSyncState `
    -TrackedStatePath $TrackedStatePath `
    -LocalStatePath $LocalStatePath `
    -UpstreamPath $UpstreamPath `
    -UpstreamBranch $UpstreamBranch

if ($CheckOnly) {
    try {
        Run-SyncCycle
    } catch {
        Write-SyncLog 'ERROR' $_.Exception.Message
        exit 1
    }

    return
}

if ($Loop) {
    do {
        try {
            Run-SyncCycle
        } catch {
            Write-SyncLog 'ERROR' $_.Exception.Message
            exit 1
        }

        Write-SyncLog 'INFO' "Sleeping for $IntervalMinutes minute(s)."
        Start-Sleep -Seconds ($IntervalMinutes * 60)
    } while ($Loop)
} else {
    try {
        Run-SyncCycle
    } catch {
        Write-SyncLog 'ERROR' $_.Exception.Message
        exit 1
    }
}
