[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$OutputDirectory = "docs/testing/generated",
    [double]$MinCompliance = 0.90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $normalized, $encoding)
}

Push-Location $RepoRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed with exit code $LASTEXITCODE."
    }

    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("chatkit-testdocs-" + [Guid]::NewGuid().ToString("N"))
    try {
        New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
        Copy-Item -Path (Join-Path $RepoRoot "tests") -Destination (Join-Path $stagingRoot "tests") -Recurse -Force

        & dotnet tool run incursa-testdocs -- generate --repoRoot $stagingRoot --outDir $OutputDirectory --strict --minCompliance $MinCompliance
        if ($LASTEXITCODE -ne 0) {
            throw "incursa-testdocs generate failed with exit code $LASTEXITCODE."
        }

        $generatedSource = Join-Path $stagingRoot $OutputDirectory
        $generatedDestination = Join-Path $RepoRoot $OutputDirectory
        if (Test-Path $generatedDestination) {
            Remove-Item -Path $generatedDestination -Recurse -Force
        }

        Copy-Item -Path $generatedSource -Destination $generatedDestination -Recurse -Force
    }
    finally {
        if (Test-Path $stagingRoot) {
            Remove-Item -Path $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    $statsJsonPath = Join-Path $RepoRoot (Join-Path $OutputDirectory "stats.json")
    if (Test-Path $statsJsonPath) {
        $statsJson = Get-Content -Path $statsJsonPath -Raw -Encoding UTF8
        $statsJson = [regex]::Replace(
            $statsJson,
            '"generatedAtUtc"\s*:\s*"[^"]+"',
            '"generatedAtUtc": "1970-01-01T00:00:00Z"')
        Write-Utf8File -Path $statsJsonPath -Content $statsJson
    }

    $statsMarkdownPath = Join-Path $RepoRoot (Join-Path $OutputDirectory "stats.md")
    if (Test-Path $statsMarkdownPath) {
        $statsMarkdown = Get-Content -Path $statsMarkdownPath -Raw -Encoding UTF8
        $statsMarkdown = [regex]::Replace(
            $statsMarkdown,
            'Generated at \(UTC\): .+',
            'Generated at (UTC): 1970-01-01T00:00:00Z')
        Write-Utf8File -Path $statsMarkdownPath -Content $statsMarkdown
    }

    Get-ChildItem -Path (Join-Path $RepoRoot $OutputDirectory) -Recurse -File |
        Where-Object { $_.Extension -in @(".md", ".json") } |
        ForEach-Object {
            Write-Utf8File -Path $_.FullName -Content (Get-Content -Path $_.FullName -Raw -Encoding UTF8)
        }
}
finally {
    Pop-Location
}
