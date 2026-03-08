<#
.SYNOPSIS
    Fetches Intune Settings Catalog definitions (settings + categories) from
    Microsoft Graph Beta API and writes them to the Assets directory as embedded
    resources for the .exe build.

.DESCRIPTION
    Authenticates via client credentials (AZURE_TENANT_ID, AZURE_CLIENT_ID,
    AZURE_CLIENT_SECRET) and paginates through all documented settings definition
    and category collections:
      - GET /beta/deviceManagement/configurationSettings
      - GET /beta/deviceManagement/complianceSettings
      - GET /beta/deviceManagement/inventorySettings
      - GET /beta/deviceManagement/reusableSettings
      - GET /beta/deviceManagement/configurationCategories
      - GET /beta/deviceManagement/complianceCategories
      - GET /beta/deviceManagement/inventoryCategories
      - GET /beta/deviceManagement/reusableCategories

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
        [string]$Label,
        [ref]$PaginationInfo,
        [switch]$AllowNotFound
    )

    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    $nextLink = $Uri
    $seenLinks = [System.Collections.Generic.HashSet[string]]::new()

    function Get-GraphNextLink {
        param([object]$Response)

        if ($null -eq $Response) { return $null }

        if ($Response -is [System.Collections.IDictionary]) {
            foreach ($key in @("@odata.nextLink", "@odata.nextlink")) {
                if ($Response.Contains($key) -and $Response[$key]) {
                    return [string]$Response[$key]
                }
            }
        }

        $nextLinkProperty = $Response.PSObject.Properties |
            Where-Object { $_.Name -ieq "@odata.nextLink" } |
            Select-Object -First 1
        if ($nextLinkProperty -and $nextLinkProperty.Value) {
            return [string]$nextLinkProperty.Value
        }

        return $null
    }

    while ($nextLink) {
        if (-not $seenLinks.Add($nextLink)) {
            throw "Pagination loop detected while fetching $Label. Graph returned a previously seen nextLink."
        }

        Write-Host "  $Label - page $page..."
        try {
            $response = Invoke-RestMethod -Uri $nextLink -Headers $headers -Method Get
        }
        catch {
            $responseMessage = $_.Exception.Response
            $statusCode = $null
            if ($responseMessage -and $responseMessage.StatusCode) {
                $statusCode = $responseMessage.StatusCode.value__
            }

            if ($AllowNotFound -and $statusCode -eq 404 -and $page -eq 1) {
                Write-Warning "  $Label endpoint is unavailable (404). Treating this collection as optional and continuing."
                break
            }

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

        $nextLink = Get-GraphNextLink -Response $response
        $page++
    }

    if ($PaginationInfo) {
        $PaginationInfo.Value = [pscustomobject]@{
            PagesFetched = $page - 1
            TotalItems   = $all.Count
        }
    }

    return $all
}

function Merge-ById {
    param(
        [object[]]$Items,
        [string]$Label
    )

    $seenIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $merged = [System.Collections.Generic.List[object]]::new()
    $duplicateCount = 0

    foreach ($item in $Items) {
        if ($null -eq $item) { continue }

        if (-not $item.id) {
            $merged.Add($item)
            continue
        }

        if ($seenIds.Add([string]$item.id)) {
            $merged.Add($item)
        }
        else {
            $duplicateCount++
        }
    }

    if ($duplicateCount -gt 0) {
        Write-Host "  Removed $duplicateCount duplicate $Label by id."
    }

    return $merged
}

# ── Fetch categories ──

Write-Host ""
Write-Host "Fetching category collections..."
$catSelect = "id,name,displayName,description,categoryDescription,helpText,platforms,technologies,settingUsage,parentCategoryId,rootCategoryId,childCategoryIds"
$categoryCollections = @(
    @{ Path = "configurationCategories"; Label = "Categories:configuration" },
    @{ Path = "complianceCategories"; Label = "Categories:compliance" },
    @{ Path = "inventoryCategories"; Label = "Categories:inventory" },
    @{ Path = "reusableCategories"; Label = "Categories:reusable" }
)

$allCategories = [System.Collections.Generic.List[object]]::new()
foreach ($collection in $categoryCollections) {
    $uri = "$graphEndpoint/beta/deviceManagement/$($collection.Path)?`$select=$catSelect"
    $collectionItems = Invoke-GraphPaginated -Uri $uri -Label $collection.Label -AllowNotFound
    Write-Host "  Retrieved $($collectionItems.Count) from $($collection.Path)."
    $allCategories.AddRange($collectionItems)
}

$categories = Merge-ById -Items $allCategories -Label "categories"
Write-Host "  Total categories after merge: $($categories.Count)"

# ── Fetch setting definitions ──

Write-Host ""
Write-Host "Fetching settings definition collections..."
# No $select -- setting definitions are polymorphic (choice, simple, group subtypes)
# and $select on the base type rejects sub-type-only fields like 'options'.
# $top=200 balances throughput vs stability. The configurationSettings endpoint is
# backed by Cosmos DB and becomes unreliable with large page sizes (skip-token
# failures / HTTP 500s). 200 keeps pages manageable while limiting round-trips.
$settingsPageSize = 200
$settingsCollections = @(
    @{ Path = "configurationSettings"; Label = "Settings:configuration" },
    @{ Path = "complianceSettings"; Label = "Settings:compliance" },
    @{ Path = "inventorySettings"; Label = "Settings:inventory" },
    @{ Path = "reusableSettings"; Label = "Settings:reusable" }
)

$allSettings = [System.Collections.Generic.List[object]]::new()

foreach ($collection in $settingsCollections) {
    $settingsUri = "$graphEndpoint/beta/deviceManagement/$($collection.Path)?`$top=$settingsPageSize"
    $paginationInfo = $null
    $collectionItems = Invoke-GraphPaginated -Uri $settingsUri -Label $collection.Label -PaginationInfo ([ref]$paginationInfo) -AllowNotFound
    Write-Host "  Retrieved $($collectionItems.Count) from $($collection.Path)."

    $singlePageAtLimit = $paginationInfo.PagesFetched -eq 1 -and $collectionItems.Count -eq $settingsPageSize
    if ($singlePageAtLimit) {
        Write-Warning "Retrieved exactly one page at the configured limit ($settingsPageSize) from $($collection.Path). This can be normal for a single collection; merged-count validation across all collections remains the primary completeness guardrail."
    }

    $allSettings.AddRange($collectionItems)
}

$settings = Merge-ById -Items $allSettings -Label "settings definitions"
Write-Host "  Total settings after merge: $($settings.Count)"

$minimumExpectedSettings = 500
$unexpectedlyLowSettingsCount = $settings.Count -lt $minimumExpectedSettings
if ($unexpectedlyLowSettingsCount) {
    throw "Retrieved only $($settings.Count) merged settings across all definition collections. This is unexpectedly low for Settings Catalog definitions and likely indicates incomplete paging or an upstream Graph issue. Aborting to prevent writing a truncated snapshot."
}

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
        if ($knownCatIds.Contains($catId)) { continue }

        $fetchedCategory = $null
        try {
            foreach ($collection in $categoryCollections) {
                try {
                    $candidate = Invoke-RestMethod -Uri "$graphEndpoint/beta/deviceManagement/$($collection.Path)/$($catId)?`$select=$catSelect" -Headers $headers -Method Get
                    if ($candidate -and $candidate.id) {
                        $fetchedCategory = $candidate
                        break
                    }
                }
                catch {
                    $response = $_.Exception.Response
                    if ($response -and $response.StatusCode -and $response.StatusCode.value__ -eq 404) {
                        continue
                    }
                    throw
                }
            }

            if ($fetchedCategory -and $knownCatIds.Add([string]$fetchedCategory.id)) {
                $categories += $fetchedCategory
                $fetched++
            }
            elseif (-not $fetchedCategory) {
                Write-Warning "  Could not fetch category $catId from any category collection -- skipping"
            }
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
