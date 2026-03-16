# Build-Installer.ps1
# Local test script for the Master Packager Dev installer build.
# Run from the repo root: .\scripts\Build-Installer.ps1
#
# First time only: install mpdev
#   Invoke-WebRequest https://cdn.masterpackager.com/installer/dev/2.1.1/mpdev_self_contained_x64_2.1.1.msi -OutFile $env:TEMP\mpdev.msi
#   Start-Process msiexec.exe -ArgumentList "/i $env:TEMP\mpdev.msi /quiet /norestart" -Wait
#
# Signed local build (reads .env from repo root):
#   .\scripts\Build-Installer.ps1 -Sign
#
# ARM64 build:
#   .\scripts\Build-Installer.ps1 -Arm64
#   .\scripts\Build-Installer.ps1 -Arm64 -Sign

[CmdletBinding()]
param(
    [string]$Version = "1.0.0-local",
    [switch]$Sign,
    [switch]$SkipPublish,
    [switch]$Arm64
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root          = $PSScriptRoot | Split-Path
$Arch          = if ($Arm64) { "arm64" } else { "x64" }
$Runtime       = "win-$Arch"
$DesktopDir    = Join-Path $Root "publish\desktop$(if ($Arm64) {'-arm64'} else {''})"
$CliDir        = Join-Path $Root "publish\cli$(if ($Arm64) {'-arm64'} else {''})"
$InstallerDir  = Join-Path $Root "publish\installer"
$DesktopProj   = Join-Path $Root "src\Intune.Commander.DesktopReact\Intune.Commander.DesktopReact.csproj"
$CliProj       = Join-Path $Root "src\Intune.Commander.CLI\Intune.Commander.CLI.csproj"
$ReactDir      = Join-Path $Root "intune-commander-react"
$Package       = Join-Path $Root "src\Intune.Commander.Installer\$(if ($Sign) {'package.signed.json'} else {'package.json'})"
$EnvFile       = Join-Path $Root ".env"

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
    if (-not (Test-Path $EnvFile)) { Fail ".env not found at $EnvFile — copy .env and fill in your signing values" }

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

    # Surface credentials where mpdev/Azure SDK can find them
    $env:AZURE_TENANT_ID       = $env_values['AZURE_TENANT_ID']
    $env:AZURE_CLIENT_ID       = $env_values['AZURE_CLIENT_ID']
    $env:AZURE_CLIENT_SECRET   = $env_values['AZURE_CLIENT_SECRET']
    $env:SIGNING_ENDPOINT      = $env_values['SIGNING_ENDPOINT']
    $env:SIGNING_ACCOUNT_NAME  = $env_values['SIGNING_ACCOUNT_NAME']
    $env:SIGNING_PROFILE_NAME  = $env_values['SIGNING_PROFILE_NAME']

    Step "Signing enabled -- Azure Trusted Signing ($($env_values['SIGNING_ACCOUNT_NAME']) / $($env_values['SIGNING_PROFILE_NAME']))"
}

if (-not $SkipPublish) {
    # Only build React once (shared across arches)
    if (-not $Arm64) {
        Step "React build"
        Push-Location $ReactDir
        npm ci --silent
        npm run build
        Pop-Location
        if (-not (Test-Path "$ReactDir\dist\index.html")) { Fail "React dist missing" }
        Ok "dist/index.html present"
    }

    Step "Publish desktop ($Runtime) -> $DesktopDir"
    dotnet publish $DesktopProj -c Release -r $Runtime --self-contained true --output $DesktopDir -p:Version=$Version --nologo -v:q
    if (-not (Test-Path "$DesktopDir\IntuneCommander.exe")) { Fail "IntuneCommander.exe missing" }
    if (-not $Arm64 -and -not (Test-Path "$DesktopDir\wwwroot\index.html")) { Fail "wwwroot/index.html missing" }
    Ok "IntuneCommander.exe present"

    Step "Publish CLI ($Runtime) -> $CliDir"
    dotnet publish $CliProj -c Release -r $Runtime --self-contained true --output $CliDir -p:PublishSingleFile=true -p:Version=$Version --nologo -v:q
    if (-not (Test-Path "$CliDir\ic.exe")) { Fail "ic.exe missing" }
    Ok "ic.exe present"
}

# Set env vars that mpdev's %IC_*% expansion will pick up
$env:IC_DESKTOP_PUBLISH_DIR = $DesktopDir
$env:IC_CLI_PUBLISH_DIR     = $CliDir

Step "mpdev build (MSI + MSIX, $Arch)"
New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null
$props = @(
    "$.version=$NumericVersion",
    "$.outputFileName=IntuneCommander-$Version-$Arch",
    "$.platform=$Arch"
)

mpdev build $Package --working-dir $Root --properties @props

Step "Verify output"
foreach ($ext in 'msi','msix') {
    $f = Join-Path $InstallerDir "IntuneCommander-$Version-$Arch.$ext"
    if (Test-Path $f) {
        $size = [math]::Round((Get-Item $f).Length / 1MB, 1)
        Ok "$ext -> $f ($size MB)"
    } else {
        Fail "$ext not found at $f"
    }
}

Write-Host "`nDone. Installers are in: $InstallerDir`n" -ForegroundColor Green
