# Remaining Work — React UI Workspaces

## Completed (Phases 1–4)

| Phase | Workspaces | Status |
|-------|-----------|--------|
| **Phase 1** | Applications, Conditional Access | Done |
| **Phase 2** | Security Posture Dashboard, Assignment Explorer | Done |
| **Phase 3** | Scripts Hub, Policy Comparison (Diff) | Done |
| **Phase 4** | Device Configurations, Compliance Policies, Endpoint Security, Enrollment & Autopilot | Done |

**Total delivered:** 15 workspaces across 5 navigation tabs.

---

## Phase 5 — Operational Workflows

### 11. Drift Detection Workspace
**Tab:** Operations
**Backend:** `IDriftDetectionService`, `IExportService`, `IExportNormalizer` (all exist in Core)
**What to build:**
- Baseline selector (file picker to choose an export directory)
- "Scan" button that runs `DriftDetectionService.CompareAsync`
- Tree view grouped by object type showing Added / Modified / Removed items
- Color-coded severity badges (Critical / High / Medium / Low)
- Click any changed item → inline Monaco diff view
- Filter by severity and object type
- Export drift report to JSON/CSV

### 12. Export / Import Workspace
**Tab:** Operations
**Backend:** `IExportService`, `IImportService`, `MigrationTable` (all exist in Core)
**What to build:**
- **Export wizard:** multi-step flow — select categories → select items → choose output directory → progress bar → done
- **Import wizard:** select backup directory → preview what will be created → dry-run validation → import with progress
- Migration table viewer (original ID → new ID mappings)
- MUI Stepper component for wizard flow
- Per-item progress indicators during export/import

### 13. Tenant Admin Workspace
**Tab:** New "Admin" primary tab
**Backend:** `IScopeTagService`, `IRoleDefinitionService`, `IIntuneBrandingService`, `IAzureBrandingService`, `ITermsAndConditionsService`, `IAdmxFileService`, `IReusablePolicySettingService`, `INotificationTemplateService`
**What to build:**
- Accordion sections for each admin object type (scope tags, roles, branding, T&C, ADMX, notification templates)
- RBAC visualizer: Role Definitions → Role Assignments → Scope Tags
- Branding preview panel
- Reusable Policy Settings list with detail
- Each section follows standard list + detail DataGrid pattern

### 14. Groups Workspace
**Tab:** Admin
**Backend:** `IGroupService` (if it exists; may need a new bridge service wrapping Graph Groups API)
**What to build:**
- Group list with type indicators (Dynamic Device / Dynamic User / Assigned)
- Dynamic group membership rule viewer with syntax highlighting
- Group membership count breakdown (users / devices / nested groups)
- Search/filter by group name
- Detail panel with membership rule and member list

**Phase 5 estimate:** 4 workspaces, ~4 bridge services, ~4 stores, ~4 workspace components

---

## Phase 6 — Advanced / Stretch Goals

### 15. Tenant Health / Monitoring Dashboard
**Tab:** Admin
**Backend:** Aggregates from all existing services + cache metadata
**What to build:**
- Stale policy detector (policies not modified in 6+ months)
- Unassigned policy finder (policies with zero assignments across all types)
- Duplicate policy detector (similar names or identical settings)
- Cache freshness indicators per data type
- License utilization widgets (VPP tokens if available)
- Overall "tenant hygiene score" computed from the above

### 16. Multi-Tenant Comparison
**Tab:** Operations or Admin
**Backend:** Two separate Graph connections + `IDriftDetectionService`
**What to build:**
- Side-by-side tenant selector (pick two connected profiles)
- Category-by-category comparison using `ExportNormalizer` + `DriftDetectionService`
- Monaco diff view per policy/config
- "Sync" action to copy policies between tenants (uses Export + Import)
- Requires supporting multiple simultaneous `GraphServiceClient` instances

**Phase 6 estimate:** 2 workspaces, significant new infrastructure for multi-tenant

---

## Navigation Additions Needed

| Phase | New sidebar items | New primary tabs |
|-------|------------------|-----------------|
| Phase 5 | Drift Detection + Export/Import under Operations; Tenant Admin + Groups under new **Admin** tab | Admin |
| Phase 6 | Tenant Health under Admin; Multi-Tenant under Operations | — |

## Known Gaps / Tech Debt

- **No `IAutopilotProfileService`** in Core — Enrollment workspace currently only uses `IEnrollmentConfigurationService`. Autopilot-specific features (OOBE visualization, deployment profiles) need a new Core service.
- **No `IGroupService`** confirmed — Groups workspace may need a new service or direct Graph calls in the bridge.
- **File picker for drift/export** — WebView2 bridge needs a file/folder picker command (dialog from .NET, path returned to React).
- **Multi-tenant** requires architectural changes to support two simultaneous Graph connections.
- **Assignment count fetching** in Phase 4 workspaces makes N+1 Graph calls (one per policy for assignments). Consider batch fetching or caching assignment counts in the list response.
