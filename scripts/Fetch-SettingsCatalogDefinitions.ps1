<#
.SYNOPSIS
    Fetches Intune Settings Catalog definitions (settings + categories) from
    Microsoft Graph Beta API and writes them to the Assets directory as embedded
    resources for the .exe build.

.DESCRIPTION
    Authenticates via client credentials (AZURE_TENANT_ID, AZURE_CLIENT_ID,
    AZURE_CLIENT_SECRET) and paginates through:
      - GET /beta/deviceManagement/configurationSettings
      - GET /beta/deviceManagement/configurationCategories

    Outputs two GZip-compressed JSON files:
      - settings-catalog-definitions.json.gz  (setting definitions)
      - settings-catalog-categories.json.gz   (category tree)

    These are committed to the repo and embedded in the .exe at build time,
    eliminating the need for runtime Graph calls to resolve setting display
    names, descriptions, and allowed values. GZip reduces ~65 MB of JSON
    to ~5-8 MB in the binary.

    The definitions are Microsoft's global schema and are identical across
    all cloud environments, so this always fetches from the Commercial endpoint.

.PARAMETER OutputDir
    Directory to write JSON files. Defaults to src/Intune.Commander.Core/Assets.

.EXAMPLE
    $env:AZURE_TENANT_ID = "..."
    $env:AZURE_CLIENT_ID = "..."
    $env:AZURE_CLIENT_SECRET = "..."
    .\Fetch-SettingsCatalogDefinitions.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Validate credentials ──

$tenantId     = $env:AZURE_TENANT_ID
$clientId     = $env:AZURE_CLIENT_ID
$clientSecret = $env:AZURE_CLIENT_SECRET

if (-not $tenantId -or -not $clientId -or -not $clientSecret) {
    Write-Error "AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET environment variables are required."
    exit 1
}

# ── Resolve output directory ──

if (-not $OutputDir) {
    $OutputDir = Join-Path $PSScriptRoot ".." "src" "Intune.Commander.Core" "Assets"
}
$OutputDir = (Resolve-Path $OutputDir -ErrorAction SilentlyContinue) ?? $OutputDir
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$settingsFile   = Join-Path $OutputDir "settings-catalog-definitions.json.gz"
$categoriesFile = Join-Path $OutputDir "settings-catalog-categories.json.gz"

# Settings Catalog definitions are Microsoft's global schema -- identical
# across Commercial, GCC, GCC-High, and DoD. Always fetch from Commercial.
$graphEndpoint = "https://graph.microsoft.com"
$loginEndpoint = "https://login.microsoftonline.com"
$scope = "$graphEndpoint/.default"

# ── Acquire token ──

Write-Host "Authenticating..."
$tokenBody = @{
    grant_type    = "client_credentials"
    client_id     = $clientId
    client_secret = $clientSecret
    scope         = $scope
}
$tokenResponse = Invoke-RestMethod -Method Post `
    -Uri "$loginEndpoint/$tenantId/oauth2/v2.0/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $tokenBody

$accessToken = $tokenResponse.access_token
$headers = @{ Authorization = "Bearer $accessToken" }

# ── Paginated fetch helper ──

function Invoke-GraphPaginated {
    param(
        [string]$Uri,
        [string]$Label
    )

    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    $nextLink = $Uri

    while ($nextLink) {
        Write-Host "  $Label - page $page..."
        try {
            $response = Invoke-RestMethod -Uri $nextLink -Headers $headers -Method Get
        }
        catch {
            $responseMessage = $_.Exception.Response
            if ($null -ne $responseMessage -and $responseMessage.StatusCode.value__ -eq 429) {
                $retryAfter = 30
                $retryHeader = $responseMessage.Headers | Where-Object { $_.Key -eq "Retry-After" }
                if ($retryHeader) { $retryAfter = [int]$retryHeader.Value[0] }
                Write-Warning "  Throttled (429). Waiting ${retryAfter}s..."
                Start-Sleep -Seconds $retryAfter
                continue
            }
            throw
        }

        if ($response.value) {
            $all.AddRange($response.value)
        }

        $nextLink = $response.'@odata.nextLink'
        $page++
    }

    return $all
}

# ── Fetch categories ──

Write-Host ""
Write-Host "Fetching configuration categories..."
$catSelect = "id,name,displayName,description,categoryDescription,helpText,platforms,technologies,settingUsage,parentCategoryId,rootCategoryId,childCategoryIds"
$categoriesUri = "$graphEndpoint/beta/deviceManagement/configurationCategories?`$select=$catSelect"
$categories = Invoke-GraphPaginated -Uri $categoriesUri -Label "Categories"
Write-Host "  Retrieved $($categories.Count) categories."

# ── Fetch setting definitions ──

Write-Host ""
Write-Host "Fetching configuration settings (definitions)..."
# No $select -- setting definitions are polymorphic (choice, simple, group subtypes)
# and $select on the base type rejects sub-type-only fields like 'options'.
# $top=200 balances throughput vs stability. The configurationSettings endpoint is
# backed by Cosmos DB and becomes unreliable with large page sizes (skip-token
# failures / HTTP 500s). 200 keeps pages manageable while limiting round-trips.
$settingsUri = "$graphEndpoint/beta/deviceManagement/configurationSettings?`$top=200"
$settings = Invoke-GraphPaginated -Uri $settingsUri -Label "Settings"
Write-Host "  Retrieved $($settings.Count) settings."

# ── Fetch orphan categories ──

$knownCatIds = [System.Collections.Generic.HashSet[string]]::new()
foreach ($cat in $categories) {
    if ($cat.id) { $knownCatIds.Add($cat.id) | Out-Null }
}

$settingCatIds = [System.Collections.Generic.HashSet[string]]::new()
foreach ($s in $settings) {
    if ($s.categoryId) { $settingCatIds.Add($s.categoryId) | Out-Null }
}

$orphanIds = $settingCatIds | Where-Object { -not $knownCatIds.Contains($_) }
if ($orphanIds) {
    Write-Host ""
    Write-Host "Found $(@($orphanIds).Count) orphan category IDs. Fetching individually..."
    $fetched = 0
    foreach ($catId in $orphanIds) {
        try {
            $cat = Invoke-RestMethod -Uri "$graphEndpoint/beta/deviceManagement/configurationCategories/$($catId)?`$select=$catSelect" -Headers $headers -Method Get
            $categories += $cat
            $fetched++
        }
        catch {
            $response = $_.Exception.Response
            if ($response -and $response.StatusCode) {
                $status = $response.StatusCode.value__
                Write-Warning "  Could not fetch category $catId (status $status) -- skipping"
            }
            else {
                Write-Warning "  Could not fetch category $catId (no HTTP response) -- $($_.Exception.Message)"
            }
        }
    }
    Write-Host "  Fetched $fetched/$(@($orphanIds).Count) orphan categories."
}

# ── Write compressed output ──

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

Write-Host ""
$settingsJson = $settings | ConvertTo-Json -Depth 20 -Compress
Write-GzipJson -Path $settingsFile -Json $settingsJson
Write-Host "Wrote $($settings.Count) settings to $settingsFile"

$categoriesJson = $categories | ConvertTo-Json -Depth 20 -Compress
Write-GzipJson -Path $categoriesFile -Json $categoriesJson
Write-Host "Wrote $($categories.Count) categories to $categoriesFile"

# ── Summary ──

$settingsSize   = [math]::Round((Get-Item $settingsFile).Length / 1MB, 2)
$categoriesSize = [math]::Round((Get-Item $categoriesFile).Length / 1MB, 2)

Write-Host ""
Write-Host "Done!"
Write-Host "  Settings:   $settingsSize MB ($($settings.Count) definitions, gzipped)"
Write-Host "  Categories: $categoriesSize MB ($($categories.Count) categories, gzipped)"
