param(
    [string]$SpecsRoot = "specs/libraries",
    [string]$MatrixPath = "specs/libraries/library-conformance-matrix.md",
    [string]$SummaryPath = "artifacts/library-traceability-summary.md"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$specsRootPath = Join-Path $repoRoot $SpecsRoot
$matrixFullPath = Join-Path $repoRoot $MatrixPath
$summaryFullPath = Join-Path $repoRoot $SummaryPath

if (-not (Test-Path $specsRootPath)) {
    Write-Error "Specs root was not found: $specsRootPath"
    exit 1
}

if (-not (Test-Path $matrixFullPath)) {
    Write-Error "Matrix file was not found: $matrixFullPath"
    exit 1
}

New-Item -ItemType Directory -Force -Path (Split-Path $summaryFullPath -Parent) | Out-Null

$specFiles = @(
    Get-ChildItem -Path $specsRootPath -Filter "*.md" -File |
        Where-Object { $_.Name -ne (Split-Path $MatrixPath -Leaf) } |
        Sort-Object Name
)

if (-not $specFiles -or $specFiles.Count -eq 0) {
    Write-Error "No library spec files were found under $specsRootPath"
    exit 1
}

$allSpecIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)

foreach ($specFile in $specFiles) {
    $content = Get-Content $specFile.FullName -Raw
    $matches = [regex]::Matches($content, 'LIB-[A-Z0-9]+-[A-Z0-9]+-\d{3}')

    foreach ($match in $matches) {
        [void]$allSpecIds.Add($match.Value)
    }
}

$matrixIds = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$duplicateIds = New-Object System.Collections.Generic.List[string]
$invalidPaths = New-Object System.Collections.Generic.List[string]
$coveredApiLibraries = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$publicApiBaselineLibraries = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)

$baselineFiles = @(
    Get-ChildItem -Path (Join-Path $repoRoot "src") -Filter "PublicAPI.Shipped.txt" -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object FullName
)

foreach ($baselineFile in $baselineFiles) {
    $libraryName = [System.IO.Path]::GetFileName([System.IO.Path]::GetDirectoryName($baselineFile.FullName))
    [void]$publicApiBaselineLibraries.Add($libraryName)
}

$lineNumber = 0
foreach ($line in Get-Content $matrixFullPath) {
    $lineNumber++
    if ($line -match '^\|\s*(LIB-[A-Z0-9]+-[A-Z0-9]+-\d{3})\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*(Covered|Missing|Deferred)\s*\|\s*(.*?)\s*\|$') {
        $scenarioId = $matches[1]
        $library = $matches[2].Trim()
        $area = $matches[3].Trim()
        $status = $matches[4]
        $mappedCell = $matches[5]

        if ($matrixIds.Contains($scenarioId)) {
            $duplicateIds.Add($scenarioId) | Out-Null
        } else {
            [void]$matrixIds.Add($scenarioId)
        }

        $paths = @([regex]::Matches($mappedCell, '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value })
        if ($status -eq "Covered") {
            if (-not $paths -or $paths.Count -eq 0) {
                $invalidPaths.Add("$scenarioId (line $lineNumber): Covered row has no mapped path.") | Out-Null
            } else {
                $hasPublicApiBaseline = $false
                $hasTestArtifact = $false
                foreach ($path in $paths) {
                    $candidate = Join-Path $repoRoot $path
                    if (-not (Test-Path $candidate)) {
                        $invalidPaths.Add("$scenarioId (line $lineNumber): Path not found '$path'.") | Out-Null
                    }

                    if ($path -match 'PublicAPI\.(Shipped|Unshipped)\.txt$') {
                        $hasPublicApiBaseline = $true
                        $libraryName = [System.IO.Path]::GetFileName((Split-Path (Join-Path $repoRoot $path) -Parent))
                        [void]$coveredApiLibraries.Add($libraryName)
                    }

                    if ($path -match '^tests/' -or $path -match '^tests\\') {
                        $hasTestArtifact = $true
                    }
                }

                if ($area -eq "PublicApi") {
                    if (-not $hasPublicApiBaseline) {
                        $invalidPaths.Add("$scenarioId (line $lineNumber): PublicApi row must reference a PublicAPI baseline file.") | Out-Null
                    }

                    if (-not $hasTestArtifact) {
                        $invalidPaths.Add("$scenarioId (line $lineNumber): PublicApi row must reference at least one test artifact.") | Out-Null
                    }
                }
            }
        }
    }
}

$missingFromMatrix = @()
foreach ($id in $allSpecIds) {
    if (-not $matrixIds.Contains($id)) {
        $missingFromMatrix += $id
    }
}

$unknownInMatrix = @()
foreach ($id in $matrixIds) {
    if (-not $allSpecIds.Contains($id)) {
        $unknownInMatrix += $id
    }
}

$missingPublicApiCoverage = @()
foreach ($library in $publicApiBaselineLibraries) {
    if (-not $coveredApiLibraries.Contains($library)) {
        $missingPublicApiCoverage += $library
    }
}

$summary = New-Object System.Collections.Generic.List[string]
$summary.Add("# Library Traceability Summary")
$summary.Add("")
$summary.Add("| Check | Result |")
$summary.Add("| --- | --- |")
$summary.Add("| Spec files discovered | $($specFiles.Count) |")
$summary.Add("| Spec IDs discovered | $($allSpecIds.Count) |")
$summary.Add("| Matrix IDs discovered | $($matrixIds.Count) |")
$summary.Add("| Missing IDs in matrix | $($missingFromMatrix.Count) |")
$summary.Add("| Unknown IDs in matrix | $($unknownInMatrix.Count) |")
$summary.Add("| Duplicate matrix IDs | $($duplicateIds.Count) |")
$summary.Add("| Invalid mapped paths | $($invalidPaths.Count) |")
$summary.Add("| Public API baselines without matrix coverage | $($missingPublicApiCoverage.Count) |")
$summary.Add("")

if ($missingFromMatrix.Count -gt 0) {
    $summary.Add("## Missing IDs")
    foreach ($id in ($missingFromMatrix | Sort-Object)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($unknownInMatrix.Count -gt 0) {
    $summary.Add("## Unknown Matrix IDs")
    foreach ($id in ($unknownInMatrix | Sort-Object)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($duplicateIds.Count -gt 0) {
    $summary.Add("## Duplicate Matrix IDs")
    foreach ($id in ($duplicateIds | Sort-Object -Unique)) {
        $summary.Add("- $id")
    }
    $summary.Add("")
}

if ($invalidPaths.Count -gt 0) {
    $summary.Add("## Invalid Coverage Rows")
    foreach ($entry in $invalidPaths) {
        $summary.Add("- $entry")
    }
    $summary.Add("")
}

if ($missingPublicApiCoverage.Count -gt 0) {
    $summary.Add("## Public API Coverage Gaps")
    foreach ($entry in ($missingPublicApiCoverage | Sort-Object)) {
        $summary.Add("- $entry")
    }
    $summary.Add("")
}

$summary | Set-Content -Path $summaryFullPath -Encoding UTF8
Write-Host "Traceability summary written to $summaryFullPath"

if ($missingFromMatrix.Count -gt 0 -or $unknownInMatrix.Count -gt 0 -or $duplicateIds.Count -gt 0 -or $invalidPaths.Count -gt 0 -or $missingPublicApiCoverage.Count -gt 0) {
    exit 1
}
