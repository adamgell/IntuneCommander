# Build-Installer.ps1
# Local test script for the Master Packager Dev installer build.
# Run from the repo root: .\scripts\Build-Installer.ps1
#
# First time only: install mpdev
#   Invoke-WebRequest https://cdn.masterpackager.com/installer/dev/2.1.1/mpdev_self_contained_x64_2.1.1.msi -OutFile $env:TEMP\mpdev.msi
#   Start-Process msiexec.exe -ArgumentList "/i $env:TEMP\mpdev.msi /quiet /norestart" -Wait
#
# Examples:
#   .\scripts\Build-Installer.ps1                  # x64 unsigned
#   .\scripts\Build-Installer.ps1 -All             # x64 + arm64 unsigned
#   .\scripts\Build-Installer.ps1 -All -Sign       # x64 + arm64 signed
#   .\scripts\Build-Installer.ps1 -Arm64 -Sign     # arm64 signed only
#   .\scripts\Build-Installer.ps1 -SkipPublish     # repack without rebuilding

[CmdletBinding()]
param(
    [string]$Version = "1.0.0-local",
    [switch]$Sign,
    [switch]$SkipPublish,
    [switch]$Arm64,
    [switch]$All   # Build both x64 and arm64
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root        = $PSScriptRoot | Split-Path
$InstallerDir = Join-Path $Root "publish\installer"
$DesktopProj = Join-Path $Root "src\Intune.Commander.DesktopReact\Intune.Commander.DesktopReact.csproj"
$CliProj     = Join-Path $Root "src\Intune.Commander.CLI\Intune.Commander.CLI.csproj"
$ReactDir    = Join-Path $Root "intune-commander-react"
$Package     = Join-Path $Root "src\Intune.Commander.Installer\$(if ($Sign) {'package.signed.json'} else {'package.json'})"
$EnvFile     = Join-Path $Root ".env"

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "    OK: $msg" -ForegroundColor Green }
function Fail($msg) { Write-Error "FAILED: $msg" }

# Numeric version for MSI/MSIX metadata (strip prerelease suffix, ensure Major >= 1)
$NumericVersion = $Version -replace '-.*',''
$parts = $NumericVersion -split '\.'
if ([int]$parts[0] -eq 0) { $parts[0] = '1' }
$NumericVersion = $parts -join '.'

# Load .env if -Sign was requested
if ($Sign) {
    if (-not (Test-Path $EnvFile)) { Fail ".env not found at $EnvFile -- fill in your signing values" }

    $env_values = @{}
    Get-Content $EnvFile | Where-Object { $_ -match '^\s*[^#]\S+=\S' } | ForEach-Object {
        $k, $v = $_ -split '=', 2
        $env_values[$k.Trim()] = $v.Trim()
    }

    $required = @('AZURE_TENANT_ID','AZURE_CLIENT_ID','AZURE_CLIENT_SECRET','SIGNING_ENDPOINT','SIGNING_ACCOUNT_NAME','SIGNING_PROFILE_NAME')
    foreach ($key in $required) {
        if (-not $env_values.ContainsKey($key) -or $env_values[$key] -match '^00000000|your-') {
            Fail "$key is not set in .env"
        }
    }

    $env:AZURE_TENANT_ID       = $env_values['AZURE_TENANT_ID']
    $env:AZURE_CLIENT_ID       = $env_values['AZURE_CLIENT_ID']
    $env:AZURE_CLIENT_SECRET   = $env_values['AZURE_CLIENT_SECRET']
    $env:SIGNING_ENDPOINT      = $env_values['SIGNING_ENDPOINT']
    $env:SIGNING_ACCOUNT_NAME  = $env_values['SIGNING_ACCOUNT_NAME']
    $env:SIGNING_PROFILE_NAME  = $env_values['SIGNING_PROFILE_NAME']

    Step "Signing enabled -- Azure Trusted Signing ($($env_values['SIGNING_ACCOUNT_NAME']) / $($env_values['SIGNING_PROFILE_NAME']))"
}

# Determine which architectures to build
$archs = if ($All) { @('x64', 'arm64') } elseif ($Arm64) { @('arm64') } else { @('x64') }

# ── React build (once, shared across arches) ──────────────────────────────────
if (-not $SkipPublish) {
    Step "React build"
    Push-Location $ReactDir
    npm ci --silent
    npm run build
    Pop-Location
    if (-not (Test-Path "$ReactDir\dist\index.html")) { Fail "React dist missing" }
    Ok "dist/index.html present"
}

New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null

# ── Per-architecture build ────────────────────────────────────────────────────
foreach ($arch in $archs) {
    $runtime    = "win-$arch"
    $desktopDir = Join-Path $Root "publish\desktop$(if ($arch -eq 'arm64') {'-arm64'} else {''})"
    $cliDir     = Join-Path $Root "publish\cli$(if ($arch -eq 'arm64') {'-arm64'} else {''})"

    if (-not $SkipPublish) {
        Step "Publish desktop ($runtime) -> $desktopDir"
        dotnet publish $DesktopProj -c Release -r $runtime --self-contained true --output $desktopDir -p:Version=$Version --nologo -v:q
        if (-not (Test-Path "$desktopDir\IntuneCommander.exe")) { Fail "IntuneCommander.exe missing ($arch)" }
        if (-not (Test-Path "$desktopDir\wwwroot\index.html")) { Fail "wwwroot/index.html missing ($arch)" }
        Ok "IntuneCommander.exe present"

        Step "Publish CLI ($runtime) -> $cliDir"
        dotnet publish $CliProj -c Release -r $runtime --self-contained true --output $cliDir -p:PublishSingleFile=true -p:Version=$Version --nologo -v:q
        if (-not (Test-Path "$cliDir\ic.exe")) { Fail "ic.exe missing ($arch)" }
        Ok "ic.exe present"
    }

    $env:IC_DESKTOP_PUBLISH_DIR = $desktopDir
    $env:IC_CLI_PUBLISH_DIR     = $cliDir

    Step "mpdev build (MSI + MSIX, $arch)"
    $props = @(
        "$.version=$NumericVersion",
        "$.outputFileName=IntuneCommander-$Version-$arch",
        "$.platform=$arch"
    )
    mpdev build $Package --working-dir $Root --properties @props

    Step "Verify $arch output"
    foreach ($ext in 'msi', 'msix') {
        $f = Join-Path $InstallerDir "IntuneCommander-$Version-$arch.$ext"
        if (Test-Path $f) {
            $size = [math]::Round((Get-Item $f).Length / 1MB, 1)
            Ok "$arch $ext -> $f ($size MB)"
        } else {
            Fail "$arch $ext not found at $f"
        }
    }
}

Write-Host "`nDone. Installers are in: $InstallerDir`n" -ForegroundColor Green
Get-ChildItem $InstallerDir -Filter "IntuneCommander-$Version-*" | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}} | Format-Table -AutoSize
