# Intune Commander - Launch Script
# Starts both the React (Vite) dev server and the .NET WPF desktop app.
# Usage: .\scripts\launch.ps1 [-NoBuild]
#   -NoBuild  Skip the dotnet build step (use last build)

param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ReactDir = Join-Path $RepoRoot 'intune-commander-react'
$DesktopProject = Join-Path $RepoRoot 'src\Intune.Commander.DesktopReact'

Write-Host ''
Write-Host '============================================' -ForegroundColor Cyan
Write-Host '  Intune Commander - Launch' -ForegroundColor Cyan
Write-Host '============================================' -ForegroundColor Cyan
Write-Host ''

# -- Step 1: Install npm dependencies if needed --
$NodeModules = Join-Path $ReactDir 'node_modules'
if (-not (Test-Path $NodeModules)) {
    Write-Host '[1/3] Installing npm dependencies...' -ForegroundColor Yellow
    Push-Location $ReactDir
    npm install
    if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'npm install failed.' }
    Pop-Location
} else {
    Write-Host '[1/3] npm dependencies up to date.' -ForegroundColor Green
}

# -- Step 2: Start Vite dev server --
Write-Host '[2/3] Starting Vite dev server...' -ForegroundColor Yellow
$viteJob = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c npm run dev' -WorkingDirectory $ReactDir -PassThru -NoNewWindow

# Give Vite a moment to start
Start-Sleep -Seconds 2

if ($viteJob.HasExited) {
    throw 'Vite dev server failed to start.'
}
Write-Host '  Vite dev server running (PID: $($viteJob.Id))' -ForegroundColor Green

# -- Step 3: Build and run the .NET desktop app --
$env:DOTNET_ENVIRONMENT = 'Development'

if (-not $NoBuild) {
    Write-Host '[3/3] Building and launching desktop app...' -ForegroundColor Yellow
} else {
    Write-Host '[3/3] Launching desktop app (skipping build)...' -ForegroundColor Yellow
}

try {
    if ($NoBuild) {
        dotnet run --project $DesktopProject --no-build
    } else {
        dotnet run --project $DesktopProject
    }
} finally {
    # Clean up Vite when the desktop app closes
    if ($viteJob -and -not $viteJob.HasExited) {
        Write-Host 'Stopping Vite dev server...' -ForegroundColor DarkGray
        Stop-Process -Id $viteJob.Id -Force -ErrorAction SilentlyContinue
    }
}
