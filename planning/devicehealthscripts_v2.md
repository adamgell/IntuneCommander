# Plan: Wave 13 — Remediation Script Catalog + Execution

## Overview

Build a built-in catalog of Windows remediation (detection + remediation) scripts sourced from
JayRHa/EndpointAnalyticsRemediationScripts. Extends Device Health Scripts with run summaries,
per-device output, on-demand deployment to devices, and result tracking.

Branch: `feature/wave13-remediation-catalog` — starts after Wave 12 merges.

---

## Architecture Decisions

| Decision | Rationale |
|----------|-----------|
| **Metadata-only in the exe** | Embedding PS1 text in a PE binary triggers AV heuristics (Defender, CrowdStrike). The exe contains only name/description/category JSON. |
| **Script content on Azure Blob CDN** | Avoids GitHub raw rate limits (60 req/hr unauthenticated) and keeps the exe clean. One bundle fetch per 7 days, served via Azure CDN. |
| **Source .ps1 files committed to repo** | `CatalogSource/` is the canonical source of truth. No runtime GitHub dependency. CI generates the CDN bundle from these files. |
| **MSBuild generates metadata JSON** | `BeforeTargets="Build"` runs `Build-ScriptCatalog.ps1`; output is committed to git so CI doesn't need to re-run it unless sources change. |
| **`IScriptCatalogService` as DI Singleton** | Metadata is immutable at runtime; service is available before auth (no `GraphServiceClient` needed). |
| **In-memory on-demand tracking** | `OnDemandDeploymentRecord` resets on app exit — no persistence needed for now. |
| **`IDeviceService` instantiated post-auth** | Requires `GraphServiceClient`; follows the same pattern as all other Graph services. |

---

## Two-Artifact Build Pipeline

```
CatalogSource/{slug}/detect.ps1
CatalogSource/{slug}/remediate.ps1
           |
           | Build-ScriptCatalog.ps1
           |
    +------+----------+
    |                 |
script-catalog.json   script-content-bundle.json
(metadata only)       (all PS1 content keyed by slug)
    |                 |
EmbeddedResource      az storage blob upload
in Core.dll           → Azure Blob CDN
    |                 (stable public URL, no rate limit)
ships in .exe         fetched once at runtime,
                      cached 7 days in LiteDB
```

---

## Phase 1: Catalog Ingestion Pipeline

### Step 1 — Commit source scripts

- Create `src/Intune.Commander.Core/CatalogSource/` — one sub-folder per script containing
  `detect.ps1` + `remediate.ps1` (names normalized)
- Write `scripts/Fetch-CatalogScripts.ps1` — **one-time developer tool**:
  - `git clone https://github.com/JayRHa/EndpointAnalyticsRemediationScripts` to temp dir
  - Walk each sub-folder; find detection/remediation pair; normalize filenames
  - Copy to `src/Intune.Commander.Core/CatalogSource/{FolderName}/`; save as ASCII (per CLAUDE.md PowerShell encoding rule)
- Developer runs once; output .ps1 files committed to git

### Step 2 — `scripts/Build-ScriptCatalog.ps1`

Parameters:
- `-SourceDir` — path to `CatalogSource/`
- `-MetadataOutputFile` — path to `Assets/script-catalog.json` (**metadata only**)
- `-ContentBundleOutputFile` — path to `artifacts/script-content-bundle.json` (**PS1 content**)

Per-folder logic:
- Read first ~30 lines of `detect.ps1` to parse comment headers
- Regex: `^#\s*([\w ]+):\s*(.+)$` → extract Name, Description, Category, Version, Publisher
- Fall back to folder name as display name if `# Name:` absent
- **Metadata file**: emit `{ id, name, description, category, version, publisher }` — **no content fields**
- **Content bundle**: emit `{ "version": "...", "generatedAt": "...", "scripts": { "slug": { "detectionContent": "...", "remediationContent": "..." } } }`
- Sort by category then name (deterministic output, no spurious git diffs)
- MSBuild target passes only `-MetadataOutputFile`; release CI passes both

### Step 3 — MSBuild target in `Intune.Commander.Core.csproj`

```xml
<Target Name="GenerateScriptCatalog" BeforeTargets="Build"
        Condition="'$(CI)'=='' Or !Exists('$(MSBuildProjectDirectory)\Assets\script-catalog.json')">
  <Exec Command="pwsh -NonInteractive -File &quot;$(MSBuildProjectDirectory)\..\..\scripts\Build-ScriptCatalog.ps1&quot; -SourceDir &quot;$(MSBuildProjectDirectory)\CatalogSource&quot; -MetadataOutputFile &quot;$(MSBuildProjectDirectory)\Assets\script-catalog.json&quot;" />
</Target>

<ItemGroup>
  <EmbeddedResource Include="Assets\script-catalog.json" />
</ItemGroup>
```

- `pwsh` pre-installed on `ubuntu-latest` and `windows-latest` — no CI yaml changes
- Committed `script-catalog.json` means CI skips regeneration when source is unchanged

### Step 4 — Models

**`src/Intune.Commander.Core/Models/ScriptCatalogEntry.cs`**
```csharp
public sealed class ScriptCatalogEntry
{
    public required string Id { get; init; }        // folder-name slug
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? Version { get; init; }
    public string? Publisher { get; init; }
}
```

**`src/Intune.Commander.Core/Models/ScriptContentBundle.cs`**
```csharp
public sealed class ScriptContentBundle
{
    public required string Version { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public required Dictionary<string, ScriptContent> Scripts { get; init; }
}

public sealed class ScriptContent
{
    public required string DetectionContent { get; init; }
    public required string RemediationContent { get; init; }
}
```

### Step 5 — `IScriptCatalogService` + `ScriptCatalogService`

```csharp
public interface IScriptCatalogService
{
    IReadOnlyList<ScriptCatalogEntry> GetAll();
    ScriptCatalogEntry? GetById(string id);
    Task<ScriptContent?> GetScriptContentAsync(string id, CancellationToken cancellationToken = default);
    Task<ScriptContentBundle> FetchContentBundleAsync(CancellationToken cancellationToken = default);
}
```

Implementation notes:
- Constructor: load metadata from `GetManifestResourceStream("Intune.Commander.Core.Assets.script-catalog.json")`
  (follows `ConditionalAccessPptExportService` pattern exactly — same `typeof(T).Assembly` approach)
- `FetchContentBundleAsync`: check `ICacheService` using `tenantId = "global"`, `dataType = "ScriptContentBundle"`, with 7-day TTL first.
  `ICacheService` only supports `List<T>` values, so the bundle is wrapped as a single-item `List<ScriptContentBundle>` — use `cache.Get<ScriptContentBundle>("global", "ScriptContentBundle")?[0]` to read and `cache.Set("global", "ScriptContentBundle", new List<ScriptContentBundle> { bundle }, TimeSpan.FromDays(7))` to write.
  The `"global"` tenantId is a documented convention for non-tenant-scoped, globally-shared cache artifacts (not a real AAD tenant ID).
  On cache miss, call `HttpClient.GetFromJsonAsync(BlobUrl, ct)`; store in cache; return.
- `BlobUrl`: **private const string** — not user-configurable (prevents SSRF):
  `const string BlobUrl = "https://{account}.blob.core.windows.net/intunecommander/catalog/script-content-bundle.json";`
- `GetScriptContentAsync`: calls `FetchContentBundleAsync` then looks up by id
- `IHttpClientFactory` injected; uses named client `"CatalogContent"` registered in DI
- Registered as **Singleton** in `ServiceCollectionExtensions.AddIntuneCommanderCore()`

### Step 6 — Azure Blob Storage

- Container: `intunecommander`, public read
- Blob: `catalog/script-content-bundle.json`
- GitHub secret: `AZURE_CREDENTIALS` (service principal with `Storage Blob Data Contributor` on container only)
- Setup script: `scripts/Setup-CatalogStorage.ps1` — provisions storage account + container + uploads initial bundle
  (analogous to existing `scripts/Setup-IntegrationTestApp.ps1`)

### Step 7 — CI: Azure upload step in `build-release.yml` + `codesign.yml`

After `dotnet publish`, add:
```yaml
- name: Generate script content bundle
  run: pwsh -NonInteractive -File scripts/Build-ScriptCatalog.ps1
       -SourceDir src/Intune.Commander.Core/CatalogSource
       -MetadataOutputFile src/Intune.Commander.Core/Assets/script-catalog.json
       -ContentBundleOutputFile artifacts/script-content-bundle.json

- name: Upload content bundle to Azure
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

- run: az storage blob upload
       --file artifacts/script-content-bundle.json
       --container-name intunecommander
       --name catalog/script-content-bundle.json
       --overwrite true
```

### Step 8 — Tests (`ScriptCatalogServiceTests.cs`)

- `GetAll()` returns non-empty list (embedded resource loaded successfully)
- All entries have non-null `Id`, `Name`
- No `ScriptCatalogEntry` has a content field (verify model shape)
- `GetById` round-trips correctly
- Interface contract via reflection (method signatures + CancellationToken params)
- `FetchContentBundleAsync` — unit test with mock `HttpClient` (no real network in unit tests)
- `GetScriptContentAsync` — returns null for unknown id; returns content for known id from mock bundle

---

## Phase 2: Extend DeviceHealthScriptService

### Step 9 — Extend `IDeviceHealthScriptService` + `DeviceHealthScriptService`

New methods:
```csharp
Task<DeviceHealthScriptRunSummary?> GetRunSummaryAsync(string scriptId, CancellationToken cancellationToken = default);
Task<List<DeviceHealthScriptDeviceState>> GetDeviceRunStatesAsync(string scriptId, CancellationToken cancellationToken = default);
Task InitiateOnDemandRemediationAsync(string managedDeviceId, string scriptId, CancellationToken cancellationToken = default);
```

Implementation:
- `GetRunSummaryAsync`: `_graphClient.DeviceManagement.DeviceHealthScripts[id].RunSummary.GetAsync(ct)`
- `GetDeviceRunStatesAsync`: manual `OdataNextLink` loop on `.DeviceRunStates`, `$expand=managedDevice`, `$top=200`
- `InitiateOnDemandRemediationAsync`: `_graphClient.DeviceManagement.ManagedDevices[deviceId].InitiateOnDemandProactiveRemediation.PostAsync(new InitiateOnDemandProactiveRemediationPostRequestBody { ScriptPolicyId = scriptId }, ct)`

Also fix: change `$top=200` to `$top=999` on the initial `ListDeviceHealthScriptsAsync` call (per copilot-instructions.md convention).

### Step 10 — New `IDeviceService` + `DeviceService`

```csharp
public interface IDeviceService
{
    Task<List<ManagedDevice>> SearchDevicesAsync(string query, CancellationToken cancellationToken = default);
    Task<ManagedDevice?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default);
}
```

Implementation:
- `SearchDevicesAsync`: filter `contains(deviceName,'{query}')`, `$top=50`, `$select=id,deviceName,operatingSystem,osVersion,lastSyncDateTime,managementState`
- Input `query` must be validated (non-null, non-empty, no OData injection characters) before inserting into filter string
- Instantiated post-auth in `MainWindowViewModel.ConnectToProfile` alongside other Graph services

### Step 11 — `DeviceHealthScriptExport` model

```csharp
public sealed class DeviceHealthScriptExport
{
    public required DeviceHealthScript Script { get; init; }
    public List<DeviceHealthScriptAssignment> Assignments { get; init; } = [];
}
```

Parity with `DeviceManagementScriptExport`. Update `ExportService` to use this wrapper and bundle assignments.

### Step 12 — `OnDemandDeploymentRecord` model

```csharp
public sealed class OnDemandDeploymentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public required string ScriptId { get; init; }
    public required string ScriptName { get; init; }
    public DateTimeOffset DispatchedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Pending";   // "Pending" | "Completed" | "Error"
    public DeviceHealthScriptDeviceState? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Step 13 — Tests for Phase 2

- `DeviceHealthScriptServiceExtendedTests.cs`: contract tests for 3 new methods via reflection
- `DeviceServiceTests.cs`: interface contract test; verify filter injection guard (empty/null/special-char query)
- `DeviceHealthScriptExportTests.cs`: verify model shape and default values

---

## Phase 3: UI

### Step 14 — `MainWindowViewModel.Remediation.cs` (new partial file)

Properties and commands:
```csharp
[ObservableProperty] DeviceHealthScriptRunSummary? _selectedScriptRunSummary;
[ObservableProperty] bool _isLoadingRunStates;
ObservableCollection<DeviceHealthScriptDeviceState> SelectedScriptDeviceRunStates { get; }
ObservableCollection<OnDemandDeploymentRecord> OnDemandDeployments { get; }

[RelayCommand] Task LoadRunStatesAsync(CancellationToken ct)
[RelayCommand] Task OpenOnDemandDialogAsync()
[RelayCommand] Task RefreshDeploymentStatusAsync(OnDemandDeploymentRecord record, CancellationToken ct)
```

`LoadRunStatesAsync` populates both `SelectedScriptRunSummary` and `SelectedScriptDeviceRunStates`
for the currently `SelectedDeviceHealthScript`. Triggered by selection change and by a manual refresh button.

`RefreshDeploymentStatusAsync` calls `GetDeviceRunStatesAsync` for the script, finds the matching
device run state, updates the record's `Status` and `Result` in-place.

### Step 15 — Catalog VM additions to `MainWindowViewModel.cs`

```csharp
[ObservableProperty] bool _isCatalogPanelOpen;
[ObservableProperty] string _catalogSearchText = string.Empty;
[ObservableProperty] ScriptCatalogEntry? _selectedCatalogEntry;
ObservableCollection<ScriptCatalogEntry> CatalogEntries { get; }
ObservableCollection<ScriptCatalogEntry> FilteredCatalogEntries { get; }

[RelayCommand] void ToggleCatalogPanel()
[RelayCommand] Task DeployFromCatalogAsync(CancellationToken ct)
[RelayCommand] Task PreviewCatalogEntryAsync(ScriptCatalogEntry entry, CancellationToken ct)
```

- `_scriptCatalogService` injected from DI in constructor (Singleton, available before auth)
- `CatalogEntries` populated immediately from `_scriptCatalogService.GetAll()` in constructor
- `FilteredCatalogEntries` filtered on `CatalogSearchText` change (same pattern as other `Filtered*` collections)
- `DeployFromCatalogAsync`:
  1. Call `_scriptCatalogService.GetScriptContentAsync(entry.Id, ct)` (fetches bundle if not cached)
  2. Show IsBusy while fetching
  3. Call `_deviceHealthScriptService!.CreateDeviceHealthScriptAsync(script, ct)`
  4. Refresh `DeviceHealthScripts` collection
  5. Show success toast / StatusText
- `PreviewCatalogEntryAsync`: loads content into a `CatalogPreviewContent` observable property for display
- Add `_deviceService` field; instantiated post-auth

### Step 16 — `OnDemandDeployWindow.axaml` + `OnDemandDeployViewModel.cs`

ViewModel:
```csharp
// Receives: scriptId, scriptName from parent
[ObservableProperty] string _deviceSearchText = string.Empty;
[ObservableProperty] bool _isSearching;
ObservableCollection<ManagedDevice> SearchResults { get; }
ObservableCollection<DeviceDeployTarget> TargetDevices { get; }   // selected device list

[RelayCommand] Task SearchDevicesAsync(CancellationToken ct)      // debounced
[RelayCommand] void AddDevice(ManagedDevice device)
[RelayCommand] void RemoveDevice(DeviceDeployTarget target)
[RelayCommand] Task DeployAsync(CancellationToken ct)
```

`DeployAsync`:
1. For each device in `TargetDevices`:
   - Call `InitiateOnDemandRemediationAsync(device.Id, scriptId, ct)`
   - On success: create `OnDemandDeploymentRecord` with `Status = "Pending"`; add to parent VM's `OnDemandDeployments`
   - On error: update target's inline status to show error message
2. Close window after all dispatched

Pattern: `GroupLookupWindow` / `AssignmentReportWindow` modal (opens with `ShowDialog`).

### Step 17 — Inline detail pane in `MainWindow.axaml`

In the Device Health Scripts section's detail pane area, add:

**Run Summary strip** (hidden when no script selected):
```
[ No Issues: 42 ]  [ Detected: 5 ]  [ Remediated: 3 ]  [ Error: 1 ]  [ Pending: 2 ]
```
Each count bound to `SelectedScriptRunSummary.*DeviceCount`.

**Device Run States section**:
- "Load Run States" button → `LoadRunStatesAsync` (with spinner bound to `IsLoadingRunStates`)
- Inline `DataGrid` bound to `SelectedScriptDeviceRunStates`:
  - Columns: Device Name (from `ManagedDevice.DeviceName`), Detection State, Remediation State, Last Sync
  - "View Output" button per row → opens `RawJsonWindow` showing detection + remediation output text

**On-Demand section**:
- "Deploy On-Demand" button → `OpenOnDemandDialogAsync`
- Collapsible `DataGrid` of `OnDemandDeployments`:
  - Columns: Device Name, Script Name, Dispatched At, Status
  - "Refresh" button per row → `RefreshDeploymentStatusAsync`

### Step 18 — Catalog flyout in `MainWindow.axaml`

In Device Health Scripts toolbar area:
- "Browse Catalog" toggle button → `ToggleCatalogPanel`

Flyout panel (right-side `Grid` column, toggled via `IsVisible` bound to `IsCatalogPanelOpen`):
```
[ Search catalog... ]  (bound to CatalogSearchText)
┌─────────────────────────────────────────┐
│ Name              │ Category  │ Version │
│ Fix Bitlocker     │ Security  │ 1.2     │
│ ...               │ ...       │ ...     │
└─────────────────────────────────────────┘
[ Deploy to Tenant ]  [ Preview ]  [ Close ]

"Preview" expander:
  Detection Script:
  ┌──────────────────────────────────────┐
  │ (read-only TextBox, loaded on demand)│
  └──────────────────────────────────────┘
  Remediation Script:
  ┌──────────────────────────────────────┐
  │ (fetches bundle if not cached)       │
  └──────────────────────────────────────┘
```

---

## Phase 4: CI Updates

### Step 19 — `build-release.yml` additions

After `dotnet publish`:
```yaml
- name: Generate script content bundle
  shell: pwsh
  run: |
    scripts/Build-ScriptCatalog.ps1 `
      -SourceDir src/Intune.Commander.Core/CatalogSource `
      -MetadataOutputFile src/Intune.Commander.Core/Assets/script-catalog.json `
      -ContentBundleOutputFile ${{ runner.temp }}/script-content-bundle.json

- uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

- name: Upload content bundle
  run: |
    az storage blob upload `
      --file "${{ runner.temp }}/script-content-bundle.json" `
      --container-name intunecommander `
      --name catalog/script-content-bundle.json `
      --overwrite true
```

Same step added to `codesign.yml` for signed releases.

**Secrets needed (repository-level):**
- `AZURE_CREDENTIALS` — service principal JSON with `Storage Blob Data Contributor` on container only

---

## Full File Manifest

### New files
| File | Purpose |
|------|---------|
| `scripts/Fetch-CatalogScripts.ps1` | One-time JayRHa import tool |
| `scripts/Build-ScriptCatalog.ps1` | Generates metadata + content bundle |
| `scripts/Setup-CatalogStorage.ps1` | Provisions Azure Blob Storage |
| `src/Intune.Commander.Core/CatalogSource/**` | Committed .ps1 source files |
| `src/Intune.Commander.Core/Assets/script-catalog.json` | Generated + committed embedded resource |
| `src/Intune.Commander.Core/Models/ScriptCatalogEntry.cs` | Catalog metadata model |
| `src/Intune.Commander.Core/Models/ScriptContentBundle.cs` | CDN bundle deserialize target |
| `src/Intune.Commander.Core/Models/ScriptContent.cs` | Detection + remediation content pair |
| `src/Intune.Commander.Core/Models/DeviceHealthScriptExport.cs` | Script + assignments export wrapper |
| `src/Intune.Commander.Core/Models/OnDemandDeploymentRecord.cs` | In-memory run tracking |
| `src/Intune.Commander.Core/Services/IScriptCatalogService.cs` | Catalog service interface |
| `src/Intune.Commander.Core/Services/ScriptCatalogService.cs` | Catalog + CDN fetch implementation |
| `src/Intune.Commander.Core/Services/IDeviceService.cs` | Device search interface |
| `src/Intune.Commander.Core/Services/DeviceService.cs` | Graph device search implementation |
| `src/Intune.Commander.Desktop/ViewModels/MainWindowViewModel.Remediation.cs` | Run states + on-demand VM logic |
| `src/Intune.Commander.Desktop/ViewModels/OnDemandDeployViewModel.cs` | On-demand deploy window VM |
| `src/Intune.Commander.Desktop/Views/OnDemandDeployWindow.axaml` | Deploy window XAML |
| `src/Intune.Commander.Desktop/Views/OnDemandDeployWindow.axaml.cs` | Deploy window code-behind |
| `tests/.../Services/ScriptCatalogServiceTests.cs` | Catalog service tests |
| `tests/.../Services/DeviceHealthScriptServiceExtendedTests.cs` | Extended service contract tests |
| `tests/.../Services/DeviceServiceTests.cs` | Device service tests |

### Modified files
| File | Change |
|------|--------|
| `Intune.Commander.Core.csproj` | MSBuild target + `<EmbeddedResource>` |
| `IDeviceHealthScriptService.cs` | 3 new method signatures |
| `DeviceHealthScriptService.cs` | Implement new methods; fix $top=200→999 |
| `ServiceCollectionExtensions.cs` | Register `IScriptCatalogService` singleton + `IHttpClientFactory` named client |
| `ExportService.cs` | Use `DeviceHealthScriptExport` wrapper |
| `MainWindowViewModel.cs` | Catalog fields, `_deviceService`, `_scriptCatalogService` |
| `MainWindowViewModel.Search.cs` | `FilteredCatalogEntries` filter |
| `MainWindow.axaml` | Catalog flyout + detail pane extensions |
| `.github/workflows/build-release.yml` | Azure upload step |
| `.github/workflows/codesign.yml` | Azure upload step |

---

## Verification Checklist

- [ ] `dotnet build` succeeds; `script-catalog.json` in `Assets/` contains only metadata (no PS1 text)
- [ ] `strings Intune.Commander.Desktop.exe | grep "Write-Output\|Param\|\$env:"` returns zero matches
- [ ] `dotnet test --filter "Category!=Integration"` passes with ≥40% coverage
- [ ] `ScriptCatalogService.GetAll()` returns entries matching committed `CatalogSource/` sub-folder count
- [ ] App launches; "Browse Catalog" flyout opens and shows catalog entries
- [ ] Expanding "Preview" triggers one HTTP fetch to the Azure blob; second expand uses LiteDB cache
- [ ] "Deploy to Tenant" creates a real `DeviceHealthScript` in a dev tenant
- [ ] On-demand deploy window: device search returns results; deploy adds `Pending` record to VM
- [ ] After device checks in: "Refresh" on a deployment record updates `Status` to `Completed`
- [ ] Run summary strip shows correct counts after `LoadRunStatesAsync`
- [ ] `build-release.yml` artifact `.exe` size is AV-clean (submit to VirusTotal as integration check)
- [ ] Azure blob `GET` returns valid JSON; `Content-Type: application/json`

---

## Open Questions

1. **Azure subscription**: existing subscription or new one? Needs Storage Account name for the `BlobUrl` constant.
2. **Catalog size filter**: JayRHa repo has ~200 scripts. Filter by platform (Windows-only)? Maximum entries cap?
3. **Bundle versioning**: should the app reject a bundle whose `version` doesn't match a minimum supported version?
4. **Offline mode**: if Azure blob is unreachable and LiteDB cache is expired, show "Catalog content unavailable — check network" vs. silently show metadata-only with disabled Deploy button?
5. **Script signing**: should scripts deployed to Intune via the catalog be signed? (Intune can enforce PS signing policies)
