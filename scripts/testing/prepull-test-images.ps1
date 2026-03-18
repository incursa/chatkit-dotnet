Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$requiredImages = @(
    "mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04",
    "postgres:16-alpine"
)

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker CLI is required but was not found on PATH."
    exit 1
}

try {
    docker info | Out-Null
} catch {
    Write-Error "Docker daemon is not available. Ensure Docker is running before executing integration tests."
    exit 1
}

Write-Host "Pre-pulling integration test images..."
foreach ($image in $requiredImages) {
    Write-Host "Pulling $image"
    docker pull $image | Out-Host
}

Write-Host "All required integration test images are available."
