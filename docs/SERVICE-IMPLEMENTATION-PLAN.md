# Intune Commander Service Implementation Plan (Current)

## Scope
- Keep migration from legacy PowerShell handlers to typed Core services + Avalonia UI.
- Maintain service-per-type architecture using `Microsoft.Graph.Beta.Models`.
- Preserve reliability rule for all list operations: manual `@odata.nextLink` pagination (`$top=999`, then `.WithUrl(nextLink)`).

## Completed Since Initial Plan (Now Done)

The original Wave 1–6 backlog has been implemented and integrated:

- Endpoint Security, Administrative Templates, Enrollment Configurations
- App Protection, Managed Device App Configurations, Targeted Managed App Configurations, Terms and Conditions
- Scope Tags, Role Definitions, Intune Branding, Azure Branding
- Autopilot, Device Health Scripts, Mac Custom Attributes, Feature Updates
- Named Locations, Authentication Strength, Authentication Context, Terms of Use
- **CA PowerPoint Export** — `IConditionalAccessPptExportService` / `ConditionalAccessPptExportService` using Syncfusion; post-export open-file prompt
- Desktop wiring in `MainWindowViewModel` and detail panes
- Export/Import coverage in `ExportService` / `ImportService`
- Core test expansion for Wave 4–6 contracts and import/export flows

## Remaining Gaps vs `EndpointManager.psm1`

### A) Missing Object Families (No equivalent service/view yet)
1. Scripts family
   - `PowerShellScripts` (`/deviceManagement/deviceManagementScripts`)
   - `MacScripts` (`/deviceManagement/deviceShellScripts`)
   - `ComplianceScripts` (`/deviceManagement/deviceComplianceScripts`)
2. Policy dependencies and support objects
   - `ADMXFiles` (`/deviceManagement/groupPolicyUploadedDefinitionFiles`)
   - `ReusableSettings` (`/deviceManagement/reusablePolicySettings`)
   - `Notifications` (`/deviceManagement/notificationMessageTemplates`)
3. Update families not yet represented
   - `UpdatePolicies` (WUfB subset in `/deviceManagement/deviceConfigurations`)
   - `QualityUpdates` (`/deviceManagement/windowsQualityUpdateProfiles`)
   - `DriverUpdateProfiles` (`/deviceManagement/windowsDriverUpdateProfiles`)
4. Enrollment/admin long-tail
   - `AppleDEPOnboardingSettings`
   - `AppleEnrollmentTypes`
   - `DeviceCategories`
5. Cloud PC
   - `W365ProvisioningPolicies` (`/deviceManagement/virtualEndpoint/provisioningPolicies`)
   - `W365UserSettings` (`/deviceManagement/virtualEndpoint/userSettings`)
6. Additional legacy object variants
   - `InventoryPolicies`
   - `AndroidOEMConfig`
   - `CompliancePoliciesV2`

### B) Feature Parity Gaps on Already-Migrated Objects
- Object-specific pre/post transforms used by legacy import/update pipelines are only partially ported.
- Assignment/update/delete behavior parity is incomplete for several categories.
- Advanced exports (CSV/document/diagram) are not broadly available in the desktop app.
- Legacy split-view depth (especially Conditional Access and Autopilot workflows) is only partially reproduced.

## Prioritized Delivery Plan

### Wave 7 — Scripts and Policy Dependencies (highest impact)
1. Add Core services + interfaces
   - `IDeviceManagementScriptService` / `DeviceManagementScriptService`
   - `IDeviceShellScriptService` / `DeviceShellScriptService`
   - `IComplianceScriptService` / `ComplianceScriptService`
   - `IAdmxFileService` / `AdmxFileService`
   - `IReusableSettingService` / `ReusableSettingService`
2. Add desktop categories, loaders, cache keys, and detail panes.
3. Add export/import support for script objects first (migration-critical path).

### Wave 8 — Update Plane Completion
1. Add services for:
   - WUfB Update Policies (typed subset service over `/deviceManagement/deviceConfigurations`)
   - Quality Updates
   - Driver Update Profiles
2. Integrate with desktop navigation and export/import.
3. Add tests for update profile pagination and serialization compatibility.

### Wave 9 — Enrollment + Apple + Device Admin Extensions
1. Add services for Apple DEP onboarding, Apple enrollment types, and device categories.
2. Surface co-management/ESP/restrictions as clear sub-views in desktop UX.
3. Port required import-order/dependency logic from legacy behavior.

### Wave 10 — Cloud PC + Long-Tail Policy Objects
1. Add `W365ProvisioningPolicies` and `W365UserSettings` services.
2. Add `InventoryPolicies`, `AndroidOEMConfig`, and `CompliancePoliciesV2` support.
3. Decide read-only vs full CRUD/import-export per object before implementation.

### Wave 11 — Behavior Parity Hardening
1. Fill missing pre/post import and update transforms for currently migrated objects.
2. Close assignment/delete parity gaps where Graph supports assignment endpoints/actions.
3. Extend CSV/document exports for additional high-value categories.

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
