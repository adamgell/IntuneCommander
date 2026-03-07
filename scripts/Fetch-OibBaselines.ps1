<#
.SYNOPSIS
    Fetches Open Intune Baseline (OIB) policy JSON files from GitHub and
    writes them to the Assets directory as embedded resources.

.DESCRIPTION
    Downloads JSON files from the SkipToTheEndpoint/OpenIntuneBaseline
    repository on GitHub and bundles them into GZip-compressed JSON array files.

    Source directories and output mapping:
      WINDOWS/IntuneManagement/SettingsCatalog/  (SC-prefixed files) -> oib-sc-baselines.json.gz
      WINDOWS/IntuneManagement/SettingsCatalog/  (ES-prefixed files) -> oib-es-baselines.json.gz
      WINDOWS/IntuneManagement/CompliancePolicies/                   -> oib-compliance-baselines.json.gz

    SC and ES policies live in the same SettingsCatalog directory and are
    distinguished by filename prefix ("Win - OIB - SC -" vs "Win - OIB - ES -").

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

# -- Retry helper (mirrors Fetch-SettingsCatalogDefinitions.ps1) --

function Invoke-RestMethodWithRetry {
    param(
        [string]$Uri,
        [hashtable]$Headers,
        [string]$Method = "Get",
        [int]$MaxRetries = 3
    )
    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        try {
            return Invoke-RestMethod -Uri $Uri -Headers $Headers -Method $Method
        }
        catch {
            $response = $_.Exception.Response
            if ($null -ne $response -and $response.StatusCode.value__ -eq 429) {
                $retryAfter = 30
                $retryHeader = $response.Headers | Where-Object { $_.Key -eq "Retry-After" }
                if ($retryHeader) { $retryAfter = [int]$retryHeader.Value[0] }
                Write-Warning "  Throttled (429). Waiting ${retryAfter}s... (attempt $attempt/$MaxRetries)"
                Start-Sleep -Seconds $retryAfter
                continue
            }
            if ($attempt -eq $MaxRetries) { throw }
            Write-Warning "  Request failed (attempt $attempt/$MaxRetries): $($_.Exception.Message)"
            Start-Sleep -Seconds (2 * $attempt)
        }
    }
}

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

function Write-AssetFile {
    param(
        [string]$OutputDir,
        [string]$FileName,
        [System.Collections.Generic.List[object]]$Entries
    )
    $outputPath = Join-Path $OutputDir $FileName
    $jsonText = ConvertTo-Json -InputObject @($Entries) -Depth 50 -Compress
    Write-GzipJson -Path $outputPath -Json $jsonText
    $sizeKb = [math]::Round((Get-Item $outputPath).Length / 1KB, 2)
    Write-Host "  Wrote $($Entries.Count) policies to $FileName ($sizeKb KB)"
}

# -- Fetch Settings Catalog + Endpoint Security (same source directory, split by prefix) --

Write-Host ""
Write-Host "Fetching WINDOWS/IntuneManagement/SettingsCatalog..."

$scApiUrl = "$baseApiUrl/$([uri]::EscapeUriString('WINDOWS/IntuneManagement/SettingsCatalog'))?ref=$branch"
$files = Invoke-RestMethodWithRetry -Uri $scApiUrl -Headers @{ "User-Agent" = $userAgent }
$jsonFiles = @($files | Where-Object { $_.name -like "*.json" })

$scEntries = [System.Collections.Generic.List[object]]::new()
$esEntries = [System.Collections.Generic.List[object]]::new()

if ($jsonFiles.Count -eq 0) {
    Write-Warning "  No JSON files found in SettingsCatalog"
}
else {
    Write-Host "  Found $($jsonFiles.Count) JSON files"

    foreach ($file in $jsonFiles) {
        Write-Host "    Downloading $($file.name)..."
        $content = Invoke-RestMethodWithRetry -Uri $file.download_url -Headers @{ "User-Agent" = $userAgent }
        $entry = @{
            fileName = $file.name
            rawJson  = $content
        }

        if ($file.name -like "* - ES - *") {
            $esEntries.Add($entry)
        }
        else {
            $scEntries.Add($entry)
        }
    }
}

Write-AssetFile -OutputDir $OutputDir -FileName "oib-sc-baselines.json.gz" -Entries $scEntries
Write-AssetFile -OutputDir $OutputDir -FileName "oib-es-baselines.json.gz" -Entries $esEntries

# -- Fetch Compliance Policies --

Write-Host ""
Write-Host "Fetching WINDOWS/IntuneManagement/CompliancePolicies..."

$compApiUrl = "$baseApiUrl/$([uri]::EscapeUriString('WINDOWS/IntuneManagement/CompliancePolicies'))?ref=$branch"
$compFiles = Invoke-RestMethodWithRetry -Uri $compApiUrl -Headers @{ "User-Agent" = $userAgent }
$compJsonFiles = @($compFiles | Where-Object { $_.name -like "*.json" })

$compEntries = [System.Collections.Generic.List[object]]::new()

if ($compJsonFiles.Count -eq 0) {
    Write-Warning "  No JSON files found in CompliancePolicies"
}
else {
    Write-Host "  Found $($compJsonFiles.Count) JSON files"

    foreach ($file in $compJsonFiles) {
        Write-Host "    Downloading $($file.name)..."
        $content = Invoke-RestMethodWithRetry -Uri $file.download_url -Headers @{ "User-Agent" = $userAgent }
        $compEntries.Add(@{
            fileName = $file.name
            rawJson  = $content
        })
    }
}

Write-AssetFile -OutputDir $OutputDir -FileName "oib-compliance-baselines.json.gz" -Entries $compEntries

# -- Summary --

Write-Host ""
Write-Host "Done!"
foreach ($name in @("oib-sc-baselines.json.gz", "oib-es-baselines.json.gz", "oib-compliance-baselines.json.gz")) {
    $path = Join-Path $OutputDir $name
    if (Test-Path $path) {
        $sizeKb = [math]::Round((Get-Item $path).Length / 1KB, 2)
        Write-Host "  ${name}: $sizeKb KB"
    }
}
