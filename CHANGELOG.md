# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

## [0.5.0] — 2026-03-07

### Added

- **Intune Commander CLI** (`ic`) — new headless command-line tool for automation and scripting
  - `ic export` — exports all or selected Intune object types to a local folder (JSON + migration table)
  - `ic import` — imports a previously exported folder into a target tenant (supports dry-run mode)
  - `ic list` — lists objects of a given type from a connected tenant in table or JSON format
  - `ic profile` — manages stored tenant profiles (add, list, remove)
  - Shell completion for zsh, bash, and fish via `ic completion`
  - Self-contained Windows x64 binary published in CI alongside the desktop app
- **Drift Detection** — new `DiffCommand` and `AlertCommand` CLI commands with supporting Core services
  - `ExportNormalizer` strips volatile fields and sorts keys/arrays for stable baseline comparison
  - `DriftDetectionService` performs file-based policy comparison with severity classification (breaking/warning/info)
  - `ic diff` compares two export snapshots and reports added/removed/changed policies
  - `ic alert` monitors a live tenant against a stored baseline and exits non-zero when drift is detected
  - `--normalize` flag on `ic export` writes normalised JSON suitable for diffing
  - 23 unit tests covering models, normalizer, and detection service
- **Wave 12 — Rich Detail Panes** for all category types
  - Generic `ExtractGraphObjectSettings` extractor shows typed derived-class settings (e.g. `AndroidWorkProfileGeneralDeviceConfiguration` properties) in the Device Configuration detail panel
  - Replaced `(complex)` placeholders with recursive object formatting; Graph SDK backing store noise stripped
  - OMA-URI section shown conditionally only for custom configurations
  - All remaining detail panels enriched with scope tags, VM-backed fields, and assignment sections
  - Detail pane content now included in Copy/JSON clipboard output
  - Added Settings Catalog settings section to `SettingsCatalogDetailPanel`
  - Device Health Scripts detail panel redesigned with 3-column layout and PowerShell syntax highlighting
  - Added top/bottom split layout with draggable splitter and toggleable detail pane for Conditional Access
- **Wave 13 — Device Health Script Operations & On-Demand Remediation**
  - New service extensions on `IDeviceHealthScriptService`: `GetRunSummaryAsync`, `GetDeviceRunStatesAsync`, `InitiateOnDemandRemediationAsync`
  - New `IDeviceService` / `DeviceService` with `$search` + `$filter` fallback chain and Windows OS filter
  - `OnDemandDeployWindow` — search devices, select targets, track deployment progress, and monitor live run states (auto-polls every 10s)
  - Run summary strip and device run states DataGrid added to Device Health Scripts detail panel
  - New `DeviceHealthScriptExport` and `OnDemandDeploymentRecord` models
- **Multi-Select in DataGrid** — new `SelectableItem<T>` model wraps items with `IsSelected` for checkbox selection
  - Header checkbox selects/deselects all visible rows
  - `SelectAll` / `DeselectAll` commands on `MainWindowViewModel`
  - Export respects the selected subset when any rows are checked
- **Settings Catalog Embedded Definitions** — schema no longer fetched at runtime from the flaky Cosmos DB-backed endpoint
  - Daily GitHub Actions workflow (`update-settings-catalog.yml`) fetches all definitions/categories from Graph Beta and commits them as embedded gzip resources
  - New `SettingsCatalogDefinitionRegistry` with lazy-loaded, case-insensitive lookup by definition ID, display name, and category
  - `FormatCatalogSettingLabel` uses embedded display names with graceful fallback to string parsing for unknown IDs
  - Settings Catalog registry pre-warmed off the UI thread during connect to eliminate first-click stalls
  - 14 unit tests for registry and model types
- **Conditional Access JSON Export** — `ExportService` gains methods to export CA policies with optional GUID→display-name resolution
  - JSON DOM walker replaces GUIDs with resolved names when enabled
  - Migration-table tracking for CA policy IDs
  - **Resolve GUIDs** toggle in UI; resolved names sourced from Directory Objects, Named Locations, Auth Strengths, Auth Contexts, and Terms of Use
- **Directory Object Resolver** — new `IDirectoryObjectResolver` / `DirectoryObjectResolver` in Core
  - Batch-resolves directory object GUIDs to display names via `POST /directoryObjects/getByIds` (up to 1,000 IDs per call)
  - Handles Users, Groups, Directory Roles, Role Templates, Service Principals, and Applications
  - Automatically filters sentinel values (`All`, `None`, `GuestsOrExternalUsers`, etc.) to avoid unnecessary API calls
  - Checks `WellKnownAppRegistry` for Microsoft first-party app IDs before hitting the Graph API
- **Well-Known App Registry** — new `WellKnownAppRegistry` static class in Core/Models
  - Lazy-loads 4,354 Microsoft first-party Entra ID application entries from embedded `MicrosoftApps.json`
  - Case-insensitive GUID lookup with `WellKnownAppRegistry.Resolve(appId)` convenience method
  - Replaces the hardcoded ~30-entry `WellKnownApps` dictionary previously in `MainWindowViewModel.Detail.cs`
  - JSON file is an embedded resource — update by simply swapping the file, no code changes needed
- **CA PowerPoint GUID-to-Name Resolution** — GUIDs in Conditional Access PowerPoint exports are now resolved to human-readable names
  - `AssignedUserWorkload`: resolves user, group, directory role, and service principal GUIDs in include/exclude lists
  - `AssignedCloudAppAction`: resolves application GUIDs in include/exclude lists
  - `ConditionLocations`: resolves named location GUIDs; sentinel values (`All` → "Any location", `AllTrusted` → "All trusted locations") preserved
  - `ConditionalAccessPptExportService`: collects all GUIDs across all policies, performs a single batch resolution, and passes the lookup to all helper constructors
  - 24 new unit tests covering name resolution across all helper classes, `WellKnownAppRegistry`, and `DirectoryObjectResolver` contract
- **Permission Check Service** — new `IPermissionCheckService` / `PermissionCheckService` in Core
  - Acquires the current token via `TokenCredential`, base64url-decodes the JWT payload, and compares granted permissions against the 14 known-required Graph scopes
  - Supports both application tokens (`roles` claim) and delegated tokens (`scp` claim)
  - Returns a `PermissionCheckResult` model with `GrantedPermissions`, `MissingPermissions`, `ExtraPermissions`, `AllPermissionsGranted`, and `ClaimSource`
  - Fire-and-forget check fired immediately after successful tenant connection; result logged to Debug Log and stored on `MainWindowViewModel.LastPermissionCheckResult`
  - 15 unit tests covering JWT decoding, classification, interface contract, and edge cases (empty/malformed JWT, no permission claim)
- **Permissions Window** — new non-modal `PermissionsWindow` displaying:
  - Summary pill: green "All 15/15 Granted" or red "N/15 Granted"
  - Token type badge (`roles` = application / `scp` = delegated)
  - Missing permissions section (red ✗, only visible when there are gaps)
  - Granted permissions section (green ✓)
  - Collapsible Extra permissions expander
  - "Not connected" placeholder when no check result is available
  - Accessible via **Help → Permissions...** menu item (disabled when not connected)
- Added `IntuneGraphClientFactory.CreateClientWithCredentialAsync` — returns a `(GraphServiceClient, TokenCredential, string[])` tuple so the credential used for authentication can be reused by `PermissionCheckService` without a second auth round-trip
- Added three AXAML value converters in `Converters/PermissionConverters.cs`:
  - `PermissionSummaryBrushConverter` — `bool → SolidColorBrush` (green / red)
  - `PermissionSummaryTextConverter` — `PermissionCheckResult → "All N/N Granted"` summary string
  - `CountGreaterThanZeroConverter` — `int → bool` for conditional visibility bindings
- **Conditional Access PowerPoint Export** (Phase 1-5 complete)
  - New service: `IConditionalAccessPptExportService` / `ConditionalAccessPptExportService`
  - Generates comprehensive PowerPoint presentations with:
    - Cover slide with tenant name and timestamp
    - Tenant summary with policy counts
    - Policy inventory table (all policies)
    - Per-policy detail slides (conditions, grant controls, assignments)
  - UI integration: Export button visible in Conditional Access category
  - File save dialog with timestamped default filename
  - Async export with cancellation support and progress feedback
  - 11 comprehensive unit tests (parameter validation, file creation, PPTX structure)
  - Commercial cloud support (v1); GCC/GCC-High/DoD deferred to future release
- Added Syncfusion.Presentation.Net.Core v28.1.33 dependency for PowerPoint generation
- Added Syncfusion license initialization via `SYNCFUSION_LICENSE_KEY` environment variable
- Updated SERVICE-IMPLEMENTATION-PLAN.md with Wave 6 (CA PowerPoint Export Integration)
- Documented Syncfusion licensing requirements in README.md

### Changed

- **Performance — Parallel Loads**: core type loads in `RefreshAsync` now run with `Task.WhenAll` (~4× faster)
- **Performance — $select Projection**: `ApplicationService` and `ConfigurationProfileService` list methods now use `$select` to fetch only UI-required fields; full object re-fetched on selection
- **Performance — Debounced Search**: search filter debounced (300 ms) and reuses filtered collections to eliminate redundant UI work
- **Performance — Cache Serialization**: `JsonIgnoreCondition.WhenWritingNull` reduces cache database size
- **Performance — Group Counts**: group member pagination replaced with 3 parallel `$count` calls with fallback
- **Performance — ReplaceAll**: `ObservableCollectionExtensions.ReplaceAll` added to Core for bulk collection swaps with a single change notification
- `ConditionalAccessPptExportService` constructor now accepts an optional `IDirectoryObjectResolver` for batch GUID resolution
- `AssignedUserWorkload`, `AssignedCloudAppAction`, and `ConditionLocations` constructors accept an optional `IReadOnlyDictionary<string, string>` name lookup
- Desktop `MainWindowViewModel.Detail.cs` now delegates app-ID resolution to the shared `WellKnownAppRegistry` instead of a local dictionary
- Moved **Permissions** toolbar button into the **Help** menu as "🔑 Permissions..." to reduce toolbar clutter; item is disabled when not connected
- `ExportService` updated with optional assignments parameter to support selective export

### Fixed

- **Cloud PC Provisioning Policies & User Settings HTTP 400** — removed unsupported `$top` query parameter from both endpoints; pagination via `OdataNextLink` preserved
- **ApplicationService missing fields** — expanded `$select` to include `owner`, `developer`, `notes`, `isFeatured`, `informationUrl`, and subtype-specific fields for Win32LobApp, iOS, macOS, Android, and WebApp
- **DeviceConfiguration detail pane incomplete** — `$select`-limited list queries omitted OMA settings; now fetches the full object on selection via `LoadConfigurationDetailsAsync`
- **Memory leak in `AssignmentReportWindow`** — `PropertyChanged` event handler now unsubscribed in `OnClosed`
- **Memory leak in `GroupLookupWindow`** — added `OnClosed` override with event unsubscription
- **ExportService file overwrite on duplicate names with null IDs** — numeric suffix fallback added when ID is null or empty
- **ProfileService silent data loss on decryption failure** — added `Debug.WriteLine` logging so failures are visible in the debug log
- **CSV formula injection in `AssignmentReportExporter.CsvQ()`** — values starting with `=`, `+`, `-`, or `@` are prefixed with a single-quote so Excel treats them as text
- **CSV formula injection in HTML template `exportCsv()` function** — same sanitization applied to the in-browser export path
- **Settings Catalog HTTP 500** — Cosmos DB skip-token cursor failures caused by over-large page requests; `$top` reduced from 999 → 100; throws on 500 instead of returning partial results
- **Settings Catalog `Lazy<>` poisoned on corrupt data** — `InvalidDataException` and `JsonException` caught inside lazy initializer; falls back to empty list instead of permanently re-throwing
- **Settings Catalog `$top=200` on definitions fetch** — explicit page size prevents oversized requests to the definitions endpoint
- **Quality Update Profiles HTTP 400** — `$top=999` exceeded the endpoint's hard cap of 200; reduced to `$top=200`
- **Driver Update Profiles HTTP 400** — same fix as Quality Update Profiles

### Documentation

- Added `docs-src/user-guide/cli.md` — CLI reference covering all commands, flags, authentication modes, and examples
- Added `docs-src/user-guide/drift-detection.md` — guide to drift detection workflows with `ic diff` and `ic alert`
- Updated `docs/GRAPH-PERMISSIONS.md`:
  - Added "Windows 365 — Cloud PC" section (`CloudPC.ReadWrite.All` permission + notes on Windows 365 licence requirement)
  - Added 9 previously missing service rows to the endpoint permission table (QualityUpdate, DriverUpdate, DeviceShellScript, ComplianceScript, AdmxFile, AppleDep, DeviceCategory, CloudPcProvisioning, CloudPcUserSettings)
  - Expanded existing permission rows to document all services that rely on each scope
- Updated `scripts/Setup-IntegrationTestApp.ps1`: added `CloudPC.ReadWrite.All` to `$requiredPermissions`

## [2026-02-18 Release]

### Added

- Added new Graph-backed services and interfaces for:
  - Conditional Access Policies (`IConditionalAccessPolicyService`, `ConditionalAccessPolicyService`)
  - Assignment Filters (`IAssignmentFilterService`, `AssignmentFilterService`)
  - Policy Sets (`IPolicySetService`, `PolicySetService`)
- Added manual `@odata.nextLink` pagination patterns to newly introduced list operations.
- Added new navigation categories and table wiring for:
  - Conditional Access
  - Assignment Filters
  - Policy Sets
- Added dedicated detail-pane sections for the new categories in the desktop UI.
- Added cache-first support for the new categories, including:
  - cache keys
  - lazy load flags
  - cache restore on connect
  - cache persistence on successful loads
- Added asynchronous lazy loading for the new categories when navigating to their tabs.
- Added `HumanDateTimeConverter` and applied it to detail-pane timestamp displays.
- Added Wave 4–5 Graph-backed services and interfaces for:
  - Windows Autopilot deployment profiles
  - Device Health Scripts
  - macOS Custom Attributes
  - Feature Update Profiles
  - Named Locations
  - Authentication Strength policies
  - Authentication Context references
  - Terms of Use agreements
- Added Wave 4–5 export/import parity coverage in core tests, including new service contract tests.
- Added Graph endpoint audit artifacts:
  - `docs/graph-uri-inventory.csv`
  - `docs/GRAPH_URI_AUDIT.md`
- Added `Micke-K/IntuneManagement` as a Git submodule for endpoint parity and reference.

### Changed

- Rebranded product/user-facing naming to **Intune Commander** across app surfaces and documentation.
- Updated assembly metadata (`AssemblyTitle`, `Product`) to Intune Commander.
- Updated repository URL references to the renamed remote.
- Improved dark/light theme behavior by moving hardcoded UI text/error colors to dynamic theme brushes.
- Tuned left navigation text color for dark mode readability with a dedicated nav text brush.
- Expanded `MainWindowViewModel` category refresh/filter/selection flows to include new service categories.
- Extended `IExportService` / `ExportService` and `IImportService` / `ImportService` with Wave 4–5 object types and migration-map support.
- Updated connection flow to use cache-first behavior for expanded cached dataset coverage.
- Updated refresh behavior to keep new categories lazy (load on selected tab) instead of always eagerly fetching.

### Fixed

- Fixed right-pane “no selection” placeholder logic to account for newly added selection types.
- Fixed XAML issues introduced during detail card expansion:
  - invalid `WrapPanel` size setting (`ItemWidth="Auto"`)
  - invalid `PolicySet` property binding
- Resolved startup/runtime XAML load instability by correcting compile-time XAML errors and validating clean rebuilds.
- Fixed build regressions caused by temporary method-placement/bracing conflicts in `MainWindowViewModel`.
- Fixed remaining Avalonia compiled binding errors in detail panes by replacing invalid `Description` bindings for `NamedLocation` and `Agreement` with valid model properties.

### UX Improvements

- Improved readability of secondary text and error text in dark theme.
- Enhanced new detail cards with richer metadata:
  - Conditional Access: state, timestamps, control indicators
  - Policy Sets: item count, timestamps
- Standardized detail-pane timestamp formatting to human-readable local time.

### Documentation

- Updated branding and links in key project docs (`README.md`, `CLAUDE.md`, planning docs, and Copilot instructions).
- Documented Graph URI inventory and service expansion mapping.

### Build & Validation

- Verified successful desktop project builds after each major implementation wave.
- Validated no remaining compile diagnostics for impacted files at release cut.
- Confirmed full core test pass (`175` passed) via `dotnet test` after Wave 4–5 integration.
