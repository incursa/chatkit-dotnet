param(
    [string]$Root = (Get-Location).Path,
    [string]$CopyrightName = 'Incursa',
    [int]$Year = (Get-Date).Year
)

$sourceExtensions = @('.cs', '.cshtml', '.razor')
$csHeader = "// Copyright (c) $Year $CopyrightName`n// SPDX-License-Identifier: MIT`n`n"
$razorHeader = "@* Copyright (c) $Year $CopyrightName`n   SPDX-License-Identifier: MIT *@`n`n"

Get-ChildItem -Path $Root -Recurse -File |
    Where-Object {
        $sourceExtensions -contains $_.Extension.ToLowerInvariant() -and
        $_.FullName -notmatch '[\\\/](bin|obj|artifacts|\.git|\.workbench)[\\\/]'
    } |
    ForEach-Object {
        $path = $_.FullName
        $content = [System.IO.File]::ReadAllText($path)
        $content = $content -replace "`r`n", "`n"
        $content = $content -replace "`r", "`n"

        $header = if ($_.Extension -in @('.cshtml', '.razor')) { $razorHeader } else { $csHeader }

        $updated = if ($content.StartsWith($header.TrimEnd("`n"))) {
            $content
        }
        else {
            $header + $content.TrimStart("`n")
        }

        if (-not $updated.EndsWith("`n")) {
            $updated += "`n"
        }

        [System.IO.File]::WriteAllText($path, $updated, [System.Text.UTF8Encoding]::new($false))
    }
