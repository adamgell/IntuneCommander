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

    Outputs two JSON files:
      - settings-catalog-definitions.json  (setting definitions)
      - settings-catalog-categories.json   (category tree)

    These are committed to the repo and embedded in the .exe at build time,
    eliminating the need for runtime Graph calls to resolve setting display
    names, descriptions, and allowed values.

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

$settingsFile   = Join-Path $OutputDir "settings-catalog-definitions.json"
$categoriesFile = Join-Path $OutputDir "settings-catalog-categories.json"

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
        [string]$Label,
        [int]$MaxRetries = 3
    )

    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    $nextLink = $Uri

    while ($nextLink) {
        Write-Host "  $Label - page $page..."
        $response = $null
        for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
            try {
                $response = Invoke-RestMethod -Uri $nextLink -Headers $headers -Method Get
                break
            }
            catch {
                $status = $_.Exception.Response.StatusCode.value__
                if ($status -eq 429) {
                    $retryAfter = 30
                    $retryHeader = $_.Exception.Response.Headers | Where-Object { $_.Key -eq "Retry-After" }
                    if ($retryHeader) { $retryAfter = [int]$retryHeader.Value[0] }
                    Write-Warning "  Throttled (429). Waiting ${retryAfter}s..."
                    Start-Sleep -Seconds $retryAfter
                    continue
                }
                elseif ($status -eq 500 -and $attempt -lt $MaxRetries) {
                    $wait = [math]::Pow(2, $attempt)
                    Write-Warning "  HTTP 500 on page $page, attempt $attempt. Retrying in ${wait}s..."
                    Start-Sleep -Seconds $wait
                    continue
                }
                elseif ($status -eq 500) {
                    Write-Warning "  HTTP 500 exhausted retries on page $page. Returning $($all.Count) items collected so far."
                    return $all
                }
                else {
                    throw
                }
            }
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
$settingsUri = "$graphEndpoint/beta/deviceManagement/configurationSettings"
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
            $status = $_.Exception.Response.StatusCode.value__
            Write-Warning "  Could not fetch category $catId (status $status) -- skipping"
        }
    }
    Write-Host "  Fetched $fetched/$(@($orphanIds).Count) orphan categories."
}

# ── Write output ──

Write-Host ""
$settingsJson = $settings | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText($settingsFile, $settingsJson, [System.Text.Encoding]::UTF8)
Write-Host "Wrote $($settings.Count) settings to $settingsFile"

$categoriesJson = $categories | ConvertTo-Json -Depth 20 -Compress
[System.IO.File]::WriteAllText($categoriesFile, $categoriesJson, [System.Text.Encoding]::UTF8)
Write-Host "Wrote $($categories.Count) categories to $categoriesFile"

# ── Summary ──

$settingsSize   = [math]::Round((Get-Item $settingsFile).Length / 1MB, 2)
$categoriesSize = [math]::Round((Get-Item $categoriesFile).Length / 1MB, 2)

Write-Host ""
Write-Host "Done!"
Write-Host "  Settings:   $settingsSize MB ($($settings.Count) definitions)"
Write-Host "  Categories: $categoriesSize MB ($($categories.Count) categories)"
