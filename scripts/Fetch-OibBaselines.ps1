<#
.SYNOPSIS
    Fetches Open Intune Baseline (OIB) policy JSON files from GitHub and
    writes them to the Assets directory as embedded resources.

.DESCRIPTION
    Downloads JSON files from three subdirectories of
    SkipToTheEndpoint/OpenIntuneBaseline on GitHub and bundles each
    directory's contents into a single GZip-compressed JSON array file.

    Subdirectories mapped to output files:
      WINDOWS/Settings Catalog/    -> oib-sc-baselines.json.gz
      WINDOWS/Endpoint Security/   -> oib-es-baselines.json.gz
      WINDOWS/Compliance Policies/ -> oib-compliance-baselines.json.gz

    Each array element contains:
      - fileName  : the original file name (used for category parsing)
      - rawJson   : the complete policy JSON payload

    No authentication is required (public repository).

.PARAMETER OutputDir
    Directory to write JSON files. Defaults to src/Intune.Commander.Core/Assets.

.EXAMPLE
    .\Fetch-OibBaselines.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# -- Resolve output directory --

if (-not $OutputDir) {
    $OutputDir = Join-Path $PSScriptRoot ".." "src" "Intune.Commander.Core" "Assets"
}
$OutputDir = (Resolve-Path $OutputDir -ErrorAction SilentlyContinue) ?? $OutputDir
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$owner = "SkipToTheEndpoint"
$repo = "OpenIntuneBaseline"
$branch = "main"
$baseApiUrl = "https://api.github.com/repos/$owner/$repo/contents"
$userAgent = "IntuneCommander-OIB-Fetcher"

$directories = @(
    @{ Path = "WINDOWS/Settings Catalog";    Output = "oib-sc-baselines.json.gz" }
    @{ Path = "WINDOWS/Endpoint Security";   Output = "oib-es-baselines.json.gz" }
    @{ Path = "WINDOWS/Compliance Policies"; Output = "oib-compliance-baselines.json.gz" }
)

# -- GZip helper (mirrors Fetch-SettingsCatalogDefinitions.ps1) --

function Write-GzipJson {
    param(
        [string]$Path,
        [string]$Json
    )
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Json)
    $fs = [System.IO.File]::Create($Path)
    $gz = [System.IO.Compression.GZipStream]::new($fs, [System.IO.Compression.CompressionLevel]::Optimal)
    $gz.Write($bytes, 0, $bytes.Length)
    $gz.Dispose()
    $fs.Dispose()
}

# -- Fetch each directory --

foreach ($dir in $directories) {
    Write-Host ""
    Write-Host "Fetching $($dir.Path)..."

    $apiUrl = "$baseApiUrl/$([uri]::EscapeUriString($dir.Path))?ref=$branch"
    $files = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = $userAgent } -Method Get
    $jsonFiles = @($files | Where-Object { $_.name -like "*.json" })

    if ($jsonFiles.Count -eq 0) {
        Write-Warning "  No JSON files found in $($dir.Path)"
        Write-GzipJson -Path (Join-Path $OutputDir $dir.Output) -Json "[]"
        continue
    }

    Write-Host "  Found $($jsonFiles.Count) JSON files"

    $entries = [System.Collections.Generic.List[object]]::new()

    foreach ($file in $jsonFiles) {
        Write-Host "    Downloading $($file.name)..."
        $content = Invoke-RestMethod -Uri $file.download_url -Headers @{ "User-Agent" = $userAgent } -Method Get
        $entries.Add(@{
            fileName = $file.name
            rawJson  = $content
        })
    }

    $outputPath = Join-Path $OutputDir $dir.Output
    $jsonText = ConvertTo-Json -InputObject @($entries) -Depth 50 -Compress
    Write-GzipJson -Path $outputPath -Json $jsonText

    $sizeKb = [math]::Round((Get-Item $outputPath).Length / 1KB, 2)
    Write-Host "  Wrote $($entries.Count) policies to $($dir.Output) ($sizeKb KB)"
}

# -- Summary --

Write-Host ""
Write-Host "Done!"
foreach ($dir in $directories) {
    $path = Join-Path $OutputDir $dir.Output
    if (Test-Path $path) {
        $sizeKb = [math]::Round((Get-Item $path).Length / 1KB, 2)
        Write-Host "  $($dir.Output): $sizeKb KB"
    }
}
