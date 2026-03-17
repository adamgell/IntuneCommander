# Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

## [0.4.7.0] — 2026-03-17

Biggest UI expansion to date — **16 new React workspaces**, a new top-level Admin tab, and full Application management coverage. Over 13,000 lines of new code across 100+ files.

### Added — Workspaces

- **Groups workspace** (`Admin > Groups`) — full list + detail view of Entra ID groups used by your Intune tenant.
  - DataGrid with display name, group type, membership rule, mail, and creation date columns
  - Type filter chips with counts: All, Dynamic Device, Dynamic User, Assigned
  - Detail panel with group properties, dynamic membership rule viewer, members table (user/device/group type icons), and Intune assignments table showing all policies and apps targeting the group
  - Backend fetches member counts, members list, and assignments in parallel via `Task.WhenAll`; detail cached with 1-hour TTL using the new `GetSingle`/`SetSingle` cache API
- **Security Posture workspace** (`Security > Security Posture`) — tenant-wide security overview dashboard.
  - Animated circular score ring (0–100) with color gradient
  - Score breakdown by category with progress bars
  - Stat cards for CA Policies, Compliance, Endpoint Security, App Protection, Auth Strengths, and Named Locations
  - Security gaps section with severity-colored cards (high/medium/low)
  - Conditional Access and Compliance policy summary tables
- **Assignment Explorer workspace** (`Operations > Assignment Explorer`) — cross-tenant assignment analysis.
  - Six report modes: All Policies, Group Lookup, All Users, All Devices, Unassigned Policies, Empty Groups
  - Group search with autocomplete dropdown for Group Lookup mode
  - Policy type filter chips (dynamically generated from results)
  - DataGrid with mode-dependent columns (assignment summary, reason, group name)
  - Paginated results (25/50/100 rows per page)
- **Policy Comparison workspace** (`Operations > Policy Diff`) — side-by-side JSON diff of any two policies within the same category (Settings Catalog, Compliance, Device Configuration, Conditional Access, App Protection, Endpoint Security) using the Monaco diff editor with syntax highlighting, line numbers, and word wrap.
- **Applications workspace** (`Applications > Applications`) — app gallery with master-detail layout showing all registered Intune mobile apps, enhanced detail panel with categories, superseded app count, information/privacy URLs, install context, and filter/filterMode on assignment entries.
- **Conditional Access workspace** (`Security > Conditional Access`) — policy list with detail panel showing conditions, grant controls, session controls, and assignments.
- **Compliance Policy workspace** (`Configuration > Compliance Policies`) — list + detail view for device compliance policies.
- **Device Configuration workspace** (`Configuration > Device Configurations`) — list + detail view for device configuration profiles.
- **Endpoint Security workspace** (`Security > Endpoint Security`) — list + detail view for endpoint security policies.
- **Enrollment workspace** (`Devices > Enrollment & Autopilot`) — list + detail view for enrollment configuration profiles.
- **Scripts Hub workspace** (`Devices > Scripts Hub`) — list + detail view for device management scripts (PowerShell, shell, platform scripts).

### Added — Application sub-workspaces

- **App Assignments workspace** (`Applications > Application Assignments`) — flat, denormalized view of every app-to-group assignment across the tenant. Each row shows app name, publisher, type, platform, target group, install intent, exclusion status, filter settings, and more. Pre-resolves group names in parallel (5 concurrent requests). Uses the 180-second long-running command timeout for large tenants.
- **Bulk App Assignments workspace** (`Applications > Bulk Assignment`) — apply assignment changes to multiple apps at once. Bootstraps by loading all apps and assignment filters, validates targets and intents, merges new assignments with existing ones (by assignment identity), calls `AssignApplicationAsync()` per app, and returns per-app success/failure counts with cache invalidation on success.
- **App Protection Policies workspace** (`Applications > App Protection Policies`) — list + detail view for iOS/Android MAM policies showing policy type, platform, version, role scope tags, minimum required app/OS versions, and assignments.
- **Managed Device App Configurations workspace** (`Applications > Managed Device App Configurations`) — list + detail view for managed device app configuration policies showing targeted mobile apps, settings, and assignments.
- **Targeted Managed App Configurations workspace** (`Applications > Targeted Managed App Configurations`) — list + detail view for MAM app configuration policies showing app group type, deployed app count, and assignments.
- **VPP Tokens workspace** (`Applications > VPP Tokens`) — list + detail view for Apple Volume Purchase Program tokens showing organization, Apple ID, sync status, expiration, account type, country/region, and auto-update settings.

### Added — Navigation

- **Admin primary tab** — new top-level navigation tab with Tenant Admin and Groups sidebar items.
- **Operations tab additions** — Drift Detection and Export/Import sidebar items with placeholder workspaces.
- **Two-tier secondary tab navigation** — sidebar items that match secondary tabs now trigger tab switches (via `primaryNavTabs` lookup) rather than just sidebar item selection.

### Added — Infrastructure

- **ApplicationDataMapper** (`Services/ApplicationDataMapper.cs`) — extracted static helper class for app type formatting, platform detection, version/bundleId/minimumOS extraction, category extraction, superseded app count, install command and context extraction, and size calculation. Handles 20+ Graph Beta `MobileApp` subtypes across Windows, iOS, Android, macOS, and Web platforms.
- **DialogBridgeService** — native OS file/folder/save picker dialogs bridged to React via `dialog.pickFolder`, `dialog.pickFile`, and `dialog.saveFile` commands. Uses `Application.Current.Dispatcher.InvokeAsync()` for UI thread marshaling with a 5-minute React-side timeout.
- **ICacheService.GetSingle/SetSingle** — new single-object cache overloads on `ICacheService` and `CacheService`. Internally wraps the item in a `List<T>` for LiteDB storage but exposes a clean single-object API. Includes async variants (`GetSingleAsync`/`SetSingleAsync`). `NullCacheService` updated with no-op implementations.
- **MonacoDiffViewer** (`components/shared/MonacoDiffViewer.tsx`) — extracted reusable Monaco diff editor component with vs-dark theme, read-only mode, side-by-side rendering, split view resizing, and word wrap. Used by Policy Comparison and available for Drift Detection.
- **GroupResolutionHelper** — shared helper for resolving Entra ID group GUIDs to display names with in-memory caching, used by multiple bridge services.
- **Bridge client dialogs module** (`bridge/dialogs.ts`) — typed TypeScript wrappers (`pickFolder()`, `pickFile()`, `saveFile()`) returning `{ path: string | null, cancelled: boolean }`.
- **Dynamic bridge command timeouts** — three timeout tiers in the bridge client: `HEAVY_COMMANDS` (60s) for multi-Graph-call commands like `groups.getDetail` and `securityPosture.summary`, `LONG_RUNNING_COMMANDS` (180s) for bulk operations like `appAssignments.list` and `bulkAppAssignments.apply`, and `DIALOG_TIMEOUT` (300s) for native file pickers.
- **Application assignment search integration** — `SearchBridgeService` now includes application assignment matches in global search results, searching across app name, publisher, target group name, and assignment type.
- **New unit tests** — `ApplicationServiceTests` and `GroupServiceTests` added for the new `IApplicationService.ListAssignmentsAsync()` method and group service contract.

### Added — Documentation

- Workspace expansion planning docs: `PLAN-react-workspaces.md` (phased buildout of 16 workspaces with navigation architecture) and `REMAINING-WORK.md` (completed/upcoming workspace tracker with known tech gaps).
- New user guide pages: Groups Workspace, Application Workspaces, Policy Comparison.
- `mkdocs.yml` updated with new navigation entries.

### Changed

- **Installer packaging** — added packaging details for Intune Commander MSI/MSIX builds in README.
- **ApplicationBridgeService refactored** — app type/platform mapping logic extracted to `ApplicationDataMapper`; detail view now caches via `GetSingle`/`SetSingle`; assignment entries now include `filter` and `filterMode` fields; `MapApps` and cache key constants changed to `internal` visibility for reuse by `ApplicationAssignmentsBridgeService`.
- **IApplicationService extended** — added `ListAssignmentsAsync()` for fetching all assignments for an app, used by the new Application Assignments workspace.

## [0.4.6.2] — 2026-03-16

### Fixed
- **App showed a white box when launched from MSI install.** Two root causes: WebView2 couldn't create its data directory inside `C:\Program Files\` (no write access for standard users), and Vite's ES module script tags fail under a `file://` origin. Both are now resolved — the app navigates via a virtual host (`https://app.intunecommander.local`) and stores its WebView2 data in `%LocalAppData%\Intune.Commander\WebView2`.
- **Second instance crashed instead of starting.** When a second copy of the app was launched while another was already running, both tried to open the same `cache.db` file exclusively. The app now detects this, falls back to a no-op cache, and shows a clear warning dialog instead of crashing.

### Added
- **ARM64 installers.** Both MSI and MSIX are now built for ARM64 alongside x64, and are published in every release.
- **Simplified installer toolchain.** The WiX installer has been replaced with Master Packager Dev (mpdev), which produces both MSI and MSIX from a single JSON package definition.

### Updated
- OIB (Operating System Intune Baselines) definitions updated to March 2026.

---

## [0.4.5] — 2026-03-14

### Added

- **BaselineService & Settings Catalog CRUD** — new services for managing Settings Catalog definitions and OIB baseline automation
- **Settings Catalog Editor** — cherry-picked core improvements for settings catalog editing workflows
- **React UI Port** — new `Intune.Commander.DesktopReact` project with WPF+WebView2 host, featuring:
  - Settings Catalog workspace with master-detail layout
  - Overview dashboard with detection & remediation workspace
  - Global search, cache sync, MUI DataGrid, and dev WebSocket bridge
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

### Changed

- `ConditionalAccessPptExportService` constructor now accepts an optional `IDirectoryObjectResolver` for batch GUID resolution
- `AssignedUserWorkload`, `AssignedCloudAppAction`, and `ConditionLocations` constructors accept an optional `IReadOnlyDictionary<string, string>` name lookup
- Desktop `MainWindowViewModel.Detail.cs` now delegates app-ID resolution to the shared `WellKnownAppRegistry` instead of a local dictionary

---

### Added

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

### Changed

- Moved **Permissions** toolbar button into the **Help** menu as "🔑 Permissions..." to reduce toolbar clutter; item is disabled when not connected

### Fixed

- **Settings Catalog HTTP 500** — Cosmos DB skip-token cursor failures caused by over-large page requests
  - Reduced `$top` from 999 → 100 (more stable for Cosmos-backed stores)
  - Added retry loop with exponential backoff (2 s / 4 s) for transient 500 errors
  - Returns partial results instead of throwing on retry exhaustion
- **Quality Update Profiles HTTP 400** — `$top=999` exceeded the endpoint's hard cap of 200; reduced to `$top=200`
- **Driver Update Profiles HTTP 400** — same fix as Quality Update Profiles

### Documentation

- Updated `docs/GRAPH-PERMISSIONS.md`:
  - Added "Windows 365 — Cloud PC" section (`CloudPC.ReadWrite.All` permission + notes on Windows 365 licence requirement)
  - Added 9 previously missing service rows to the endpoint permission table (QualityUpdate, DriverUpdate, DeviceShellScript, ComplianceScript, AdmxFile, AppleDep, DeviceCategory, CloudPcProvisioning, CloudPcUserSettings)
  - Expanded existing permission rows to document all services that rely on each scope
- Updated `scripts/Setup-IntegrationTestApp.ps1`: added `CloudPC.ReadWrite.All` to `$requiredPermissions`

---

### Added

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
