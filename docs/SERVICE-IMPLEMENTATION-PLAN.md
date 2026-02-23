# Intune Commander Service Implementation Plan (Current)

## Scope
- Keep migration from legacy PowerShell handlers to typed Core services + Avalonia UI.
- Maintain service-per-type architecture using `Microsoft.Graph.Beta.Models`.
- Preserve reliability rule for all list operations: manual `@odata.nextLink` pagination (`$top=999`, then `.WithUrl(nextLink)`).

> **Note on page-size exceptions:** `configurationfigurationPolicies` (Settings Catalog) uses `$top=100` with retry to avoid Cosmos DB cursor failures. `windowsQualityUpdateProfiles` and `windowsDriverUpdateProfiles` use `$top=200` (hard API cap).

## Completed Since Initial Plan (Now Done)

The original Wave 1–6 backlog plus Waves 7–10 have been fully implemented and integrated:

### Waves 1–6 (original plan)
- Endpoint Security, Administrative Templates, Enrollment Configurations
- App Protection, Managed Device App Configurations, Targeted Managed App Configurations, Terms and Conditions
- Scope Tags, Role Definitions, Intune Branding, Azure Branding
- Autopilot, Device Health Scripts, Mac Custom Attributes, Feature Updates
- Named Locations, Authentication Strength, Authentication Context, Terms of Use
- **CA PowerPoint Export** — `IConditionalAccessPptExportService` / `ConditionalAccessPptExportService` using Syncfusion
- Desktop wiring in `MainWindowViewModel` and detail panes
- Export/Import coverage in `ExportService` / `ImportService`

### Wave 7 — Scripts and Policy Dependencies
- `IDeviceManagementScriptService` / `DeviceManagementScriptService` (PowerShell scripts)
- `IDeviceShellScriptService` / `DeviceShellScriptService` (macOS shell scripts)
- `IComplianceScriptService` / `ComplianceScriptService`
- `IAdmxFileService` / `AdmxFileService` (ADMX uploaded definitions)
- `IReusablePolicySettingService` / `ReusablePolicySettingService`
- `INotificationTemplateService` / `NotificationTemplateService`

### Wave 8 — Update Plane Completion
- `IQualityUpdateProfileService` / `QualityUpdateProfileService` — `$top=200` (hard API cap)
- `IDriverUpdateProfileService` / `DriverUpdateProfileService` — `$top=200` (hard API cap)

### Wave 9 — Enrollment + Apple + Device Admin
- `IAppleDepService` / `AppleDepService`
- `IDeviceCategoryService` / `DeviceCategoryService`
- `IVppTokenService` / `VppTokenService`
- `IUserService` / `UserService`

### Wave 10 — Cloud PC
- `ICloudPcProvisioningService` / `CloudPcProvisioningService` — requires `CloudPC.ReadWrite.All` + active Windows 365 licence
- `ICloudPcUserSettingsService` / `CloudPcUserSettingsService`

### Cross-cutting additions
- `IPermissionCheckService` / `PermissionCheckService` — JWT-based token introspection; returns `PermissionCheckResult` with granted/missing/extra permission sets; fire-and-forget at connect time; never blocks UI
- `PermissionsWindow` in Desktop — non-modal window showing granted/missing Graph permissions; accessible via Help menu

## Remaining Gaps vs `EndpointManager.psm1`

### A) Missing Object Families
1. Additional enrollment variants
   - `AppleEnrollmentTypes` (`/deviceManagement/deviceEnrollmentConfigurations` filtered)
2. Legacy policy object variants
   - `AndroidOEMConfig` (subset of `/deviceManagement/deviceConfigurations`)
   - `CompliancePoliciesV2` (`/deviceManagement/compliancePolicies`)
3. Long-tail inventory
   - `InventoryPolicies` / `HardwareConfigurations`

### B) Feature Parity Gaps on Already-Migrated Objects
- Object-specific pre/post transforms used by legacy import/update pipelines are only partially ported.
- Assignment/update/delete behavior parity is incomplete for several categories.
- Advanced exports (CSV/document/diagram) are not broadly available in the desktop app.
- Legacy split-view depth (especially Conditional Access and Autopilot workflows) is only partially reproduced.

## Prioritized Delivery Plan

### Wave 11 — Behavior Parity Hardening
1. Fill missing pre/post import and update transforms for currently migrated objects.
2. Close assignment/delete parity gaps where Graph supports assignment endpoints/actions.
3. Extend CSV/document exports for additional high-value categories.

### Wave 12 — Rich Detail Panes (in progress)
- Full object-property detail panes for all types (replacing truncated card views).
- Terms of Use enrichment: display Agreement model properties.
- Conditional Access: GUID → display name resolution for group/app/location references.

## Implementation Checklist (apply to each new service)
- [ ] Add `I<Type>Service` and `<Type>Service` in `src/Intune.Commander.Core/Services/`
- [ ] Constructor receives `GraphServiceClient`
- [ ] Async APIs accept `CancellationToken`
- [ ] List methods use manual pagination (`$top=999` + `OdataNextLink`)
- [ ] Add desktop loader + nav category + cache key
- [ ] Add export/import handlers when migration-relevant
- [ ] Add focused Core tests (pagination, CRUD, null/error handling)

## Definition of Done
- Builds cleanly (`dotnet build`) and relevant tests pass (`dotnet test`).
- No UI-thread blocking introduced (startup remains async-first).
- New service is visible in desktop navigation and can load tenant data.
- Export/import (when in scope) is migration-table compatible.
- Documentation updated with endpoint mapping and status.
