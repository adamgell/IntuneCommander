# Plan: React UI Workspaces Expansion

## Context

The Intune Commander React UI currently has **5 workspaces** (Overview, Settings Catalog, Detection & Remediation, Global Search, Cache Inspector) covering only 2 of 38+ data types available in the .NET Core backend. The goal is to design additional workspaces that take full advantage of React's capabilities — going beyond simple list+detail grids to create cross-cutting, interactive experiences that weren't possible in the Avalonia desktop app.

## Current State

**React UI has:** Overview Dashboard, Settings Catalog, Detection & Remediation, Global Search, Cache Inspector
**Navigation:** 2 primary tabs (Configuration, Devices) with secondary tabs
**Tech:** React 19, Zustand, MUI 7, MUI X DataGrid/Charts, Monaco Editor, IPC bridge to .NET
**Backend:** 38+ Graph API services with full CRUD, caching, export/import, drift detection, assignment checking

---

## Proposed Workspaces

### Wave 1 — High-Value Cross-Cutting Workspaces (P0)

These go beyond porting — they combine multiple data sources into views that don't exist in the desktop app.

#### 1. Security Posture Dashboard
**Purpose:** Single-pane security overview combining Conditional Access + Compliance + Endpoint Security + App Protection
**Data sources:** `ConditionalAccessPolicyService`, `CompliancePolicyService`, `EndpointSecurityService`, `AppProtectionPolicyService`, `AuthenticationStrengthService`, `NamedLocationService`
**Layout:**
- Security score ring chart (computed from coverage gaps)
- Conditional Access policy list with state indicators (enabled/report-only/disabled)
- Compliance policy coverage matrix (platform x policy type heatmap)
- App Protection policy status cards (iOS/Android coverage)
- Named Locations map visualization (IP ranges + countries)
- Authentication strength tier breakdown
**Why React:** Interactive heatmaps, click-through drill-downs, dynamic filtering across policy types — impossible in a static DataGrid

#### 2. Policy Comparison / Diff Workspace
**Purpose:** Side-by-side comparison of any two policies (same type) with highlighted differences
**Data sources:** Any service's `GetDetail` + `DriftDetectionService` + `ExportNormalizer`
**Layout:**
- Two-pane selector (pick Policy A and Policy B from same category)
- Monaco diff editor showing JSON differences (red/green highlighting)
- Summary panel: "12 properties differ, 3 assignments differ"
- Tabbed sections: Settings diff, Assignments diff, Scope Tags diff
- Option to compare against exported baseline (file picker)
**Why React:** Monaco diff editor is a native React component; real-time diff rendering is trivial in React but very hard in Avalonia

#### 3. Assignment Explorer
**Purpose:** Answer "What policies/apps are assigned to this group?" and "What groups does this policy target?"
**Data sources:** `AssignmentCheckerService`, `GroupService`, all policy services (assignments), `AssignmentFilterService`
**Layout:**
- Search bar: type a group name → see every policy, app, script assigned to it
- Reverse view: select a policy → see all target groups with include/exclude/filter details
- Interactive Sankey diagram: Groups → Assignment Filters → Policies (flow visualization)
- Conflict detection: highlight when include and exclude overlap
- Export to CSV/Excel
**Why React:** Interactive graph/flow visualizations, instant filtering, bidirectional navigation

---

### Wave 2 — Core Data Workspaces (P0)

These are the most-used Intune data types that need dedicated workspaces with richer UIs than the desktop app.

#### 4. Applications Workspace
**Purpose:** Full app lifecycle management — the single most-used Intune screen
**Data sources:** `ApplicationService`, `AppProtectionPolicyService`, `ManagedDeviceAppConfigurationService`, `TargetedManagedAppConfigurationService`, `VppTokenService`
**Layout:**
- App gallery grid (card view with icons) + DataGrid toggle
- Platform filter tabs (iOS, Android, Windows, macOS, Web)
- Detail panel: app metadata, assignments, app config policies, protection policies
- App assignment matrix: DataGrid showing App × Group × Intent (Required/Available/Uninstall)
- VPP token status cards (license counts, expiry dates)
**Why React:** Card gallery view with images, platform filter tabs, matrix visualization

#### 5. Conditional Access Workspace
**Purpose:** Visualize and manage CA policies — the most complex Intune/Entra object type
**Data sources:** `ConditionalAccessPolicyService`, `NamedLocationService`, `AuthenticationStrengthService`, `AuthenticationContextService`, `DirectoryObjectResolver`
**Layout:**
- Policy list with state chips (Enabled/Report-only/Disabled) and coverage badges
- Detail panel with resolved names (not just GUIDs) for users, groups, apps, locations
- "What-If" simulator: select a user + app + location → show which CA policies would apply
- Policy flow diagram: visual representation of Conditions → Grant/Session Controls
- Named Location management panel
**Why React:** Interactive "What-If" analysis, policy flow diagrams, GUID-to-name resolution inline

#### 6. Scripts Hub (Unified)
**Purpose:** Manage all script types in one place: PowerShell, Shell, Compliance, Health Scripts
**Data sources:** `DeviceManagementScriptService`, `DeviceShellScriptService`, `ComplianceScriptService`, `DeviceHealthScriptService`
**Layout:**
- Tabbed view: PowerShell | Shell (macOS/Linux) | Compliance | Health Scripts
- Monaco editor for each script type (syntax highlighting per language)
- Run status dashboard per script (device success/failure counts)
- Deploy-on-demand panel (reuse existing DeployPanel pattern)
- Script template library (common scripts as starting points)
**Why React:** Reuse Monaco editor pattern from Detection & Remediation; unify 4 script types into one tabbed view

---

### Wave 3 — Configuration Workspaces (P1)

#### 7. Device Configurations Workspace
**Purpose:** Manage device configuration profiles with platform grouping
**Data sources:** `ConfigurationProfileService`
**Layout:**
- Platform-grouped accordion view (Windows, iOS, macOS, Android)
- Detail panel: OMA-URI settings table, assignments, scope tags
- Settings search across all profiles
- Configuration conflict detector (same OMA-URI in multiple profiles)

#### 8. Compliance Policies Workspace
**Purpose:** Compliance policy management with posture overview
**Data sources:** `CompliancePolicyService`
**Layout:**
- Policy list with platform icons and action counts
- Detail panel: compliance settings, non-compliance actions (escalation timeline)
- Non-compliance action timeline visualization (mark as non-compliant → notify → retire)

#### 9. Endpoint Security Workspace
**Purpose:** Security baselines and intents
**Data sources:** `EndpointSecurityService`, `BaselineService`
**Layout:**
- Category grouping: Antivirus, Firewall, Disk Encryption, EDR, Attack Surface Reduction, Account Protection
- Baseline comparison view (current vs. recommended)
- Template-based detail view

#### 10. Enrollment & Autopilot Workspace
**Purpose:** Enrollment configuration + Autopilot + Apple DEP in one view
**Data sources:** `EnrollmentConfigurationService`, `AutopilotService`, `AppleDepService`, `CloudPcProvisioningService`
**Layout:**
- Platform tabs: Windows Autopilot | Apple DEP | Cloud PC
- Autopilot profile detail: OOBE settings visualization (which screens are skipped)
- DEP token status + profile assignment
- Cloud PC provisioning overview

---

### Wave 4 — Admin & Operational Workspaces (P1)

#### 11. Drift Detection Workspace
**Purpose:** Compare current tenant config against a saved baseline and highlight changes
**Data sources:** `DriftDetectionService`, `ExportService`, `ExportNormalizer`
**Layout:**
- Baseline selector (pick an export directory)
- "Scan" button → drift report
- Tree view: Object Type → Changed items (Added/Modified/Removed with color coding)
- Click any item → Monaco diff view of the change
- Severity filtering (High/Medium/Low)
- Export drift report
**Why React:** Tree view with color-coded badges, inline Monaco diffs, filtering — a purely React-native experience

#### 12. Export / Import Workspace
**Purpose:** Visual export/import wizard with progress tracking
**Data sources:** `ExportService`, `ImportService`, migration table
**Layout:**
- Export wizard: multi-step (select categories → select items → choose directory → progress)
- Import wizard: select backup → preview what will be created → dry-run validation → import with progress
- Migration table viewer (old ID → new ID mappings)
- Diff preview before import (compare backup vs. current tenant)
**Why React:** Multi-step wizard with stepper component, progress bars, validation previews

#### 13. Tenant Admin Workspace
**Purpose:** Consolidate low-frequency admin objects
**Data sources:** `ScopeTagService`, `RoleDefinitionService`, `RoleAssignmentService`, `PolicySetService`, `IntuneBrandingService`, `AzureBrandingService`, `TermsAndConditionsService`, `AdmxFileService`, `ReusablePolicySettingService`, `NotificationTemplateService`
**Layout:**
- Accordion sections for each admin object type
- RBAC visualizer: Role Definitions → Role Assignments → Scope Tags (tree/graph view)
- Branding preview (live preview of branding settings applied to a mock sign-in page)
- Policy Sets: grouped view showing which policies are bundled together

#### 14. Groups Workspace
**Purpose:** Group management with membership rule builder
**Data sources:** `GroupService` (dynamic + assigned groups)
**Layout:**
- Dynamic groups: membership rule editor with syntax highlighting
- Group membership counts with user/device/nested group breakdown
- "Test membership rule" — enter a device/user, see if it would match
- Assigned groups: member list with add/remove

---

### Wave 5 — Advanced / Innovative (P2)

#### 15. Tenant Health / Monitoring Dashboard
**Purpose:** Real-time tenant health monitoring
**Data sources:** All services (aggregate counts), cache timestamps, connection status
**Layout:**
- Stale policy detector (policies not modified in 6+ months)
- Unassigned policy finder (policies with zero assignments)
- Duplicate policy detector (similar names/settings)
- License utilization widgets (VPP tokens, Cloud PC)
- Cache freshness indicators per data type

#### 16. Multi-Tenant Comparison (Future)
**Purpose:** Compare configurations across two connected tenants
**Data sources:** Two separate Graph connections + `DriftDetectionService`
**Layout:**
- Side-by-side tenant selector
- Category-by-category comparison
- "Sync" button to copy policies between tenants
- This builds on the existing export/import + drift detection infrastructure

---

## Implementation Pattern (per workspace)

Each new workspace follows the established pattern:

1. **Types file** (`src/types/<workspace>.ts`) — TypeScript interfaces for list items + details
2. **Zustand store** (`src/store/<workspace>Store.ts`) — state + bridge commands + actions
3. **Bridge commands** — new commands in .NET `BridgeCommandHandler` to expose Core services
4. **Workspace component** (`src/components/workspace/<Name>Workspace.tsx`) — main layout
5. **Sub-components** — DataGrid, detail panels, charts, editors as needed
6. **Sidebar + ContentArea** — register in navigation and routing

### Files to modify for each workspace:
- `intune-commander-react/src/components/shell/Sidebar.tsx` — add nav item
- `intune-commander-react/src/components/shell/ContentArea.tsx` — add route
- `intune-commander-react/src/components/shell/TopBar.tsx` — add secondary tab if needed
- `intune-commander-react/src/store/appStore.ts` — add nav constants
- `src/Intune.Commander.DesktopReact/` — add bridge command handlers

### Existing patterns to reuse:
- `PolicyTable.tsx` pattern for any list DataGrid
- `PolicyDetailPanel.tsx` pattern for detail side panels
- `DeployPanel.tsx` pattern for device search + action flows
- `ScriptDetailDashboard.tsx` pattern for Monaco editor integration
- Store pattern from `settingsCatalogStore.ts` (lazy load, bridge commands, loading states)

---

## Navigation Architecture: Hybrid Approach

Keep primary tabs for major areas, use sidebar for workspaces within each area.

**Primary Tabs (TopBar):**
| Tab | Sidebar Workspaces |
|-----|-------------------|
| **Configuration** | Overview, Settings Catalog, Device Configs, Compliance, Admin Templates, Endpoint Security |
| **Applications** | App Gallery, App Assignments, App Protection, App Config |
| **Security** | Security Posture Dashboard, Conditional Access, Named Locations, Auth Strengths |
| **Devices** | Detection & Remediation, Scripts Hub (PS/Shell/Compliance), Enrollment & Autopilot |
| **Operations** | Assignment Explorer, Policy Diff, Drift Detection, Export/Import |
| **Admin** | Tenant Admin, Groups, Tenant Health |

Each primary tab shows its workspaces in the sidebar with collapsible groups. The sidebar updates contextually when switching tabs.

**Files to modify:**
- `intune-commander-react/src/types/models.ts` — update `PrimaryNavTab` / `SecondaryNavTab` definitions
- `intune-commander-react/src/store/appStore.ts` — new tab definitions + sidebar items per tab
- `intune-commander-react/src/components/shell/TopBar.tsx` — add new primary tabs
- `intune-commander-react/src/components/shell/Sidebar.tsx` — contextual sidebar per primary tab

---

## Recommended Build Order

| Phase | Workspaces | Rationale |
|-------|-----------|-----------|
| **Phase 1** | Applications (4), Conditional Access (5) | Highest daily-use Intune screens |
| **Phase 2** | Security Posture (1), Assignment Explorer (3) | Cross-cutting "React advantage" — combine multiple data sources |
| **Phase 3** | Scripts Hub (6), Policy Diff (2) | Complete the scripting story + comparison tool |
| **Phase 4** | Device Configs (7), Compliance (8), Endpoint Security (9), Enrollment (10) | Complete core data coverage |
| **Phase 5** | Drift Detection (11), Export/Import (12), Tenant Admin (13), Groups (14) | Operational workflows |
| **Phase 6** | Tenant Health (15), Multi-Tenant (16) | Advanced / stretch goals |

## Verification

- Each workspace renders with mock data when bridge is unavailable (dev mode)
- Navigation correctly routes to each new workspace
- Bridge commands successfully fetch data from .NET Core services
- DataGrids load lazily on first navigation (existing `hasAttemptedLoad` pattern)
- `npm run build` succeeds with no TypeScript errors
- Existing workspaces continue to function (no regressions)
