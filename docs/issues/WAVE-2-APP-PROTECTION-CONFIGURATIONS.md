# Wave 2: App Protection and Managed App Configurations

**Reference:** [docs/SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) — Wave 2

## Scope

Wave 2 focuses on implementing services for application protection policies and mobile app configurations:

1. **App Protection Policies** — Managed app policies for data protection
2. **Managed App Configurations** — Mobile app configuration profiles
3. **Terms and Conditions** — End-user terms and conditions for enrollment

## Service Implementations

### 1. App Protection Service

**Interface:** `IAppProtectionService`  
**Class:** `AppProtectionService`  
**Graph Endpoints:**
- Base collection: `/deviceAppManagement/managedAppPolicies`
- Note: May need separate endpoints for iOS/Android/Windows policies

**Methods to Implement:**
- [ ] `ListManagedAppPoliciesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<ManagedAppPolicy>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetManagedAppPolicyAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `ManagedAppPolicy`
  - Throw if null response
- [ ] `CreateManagedAppPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default)`
  - Returns created `ManagedAppPolicy`
  - Throw if null response
- [ ] `UpdateManagedAppPolicyAsync(ManagedAppPolicy policy, CancellationToken cancellationToken = default)`
  - Returns updated `ManagedAppPolicy`
  - Throw if null response
- [ ] `DeleteManagedAppPolicyAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string policyId, CancellationToken cancellationToken = default)`
  - Returns assignments if supported by API
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)
- [ ] `AssignPolicyAsync(string policyId, List<ManagedAppPolicyAssignment> assignments, CancellationToken cancellationToken = default)`
  - If supported by API

### 2. Managed App Configuration Service

**Interface:** `IManagedAppConfigurationService`  
**Class:** `ManagedAppConfigurationService`  
**Graph Endpoints:**
- Mobile app configurations: `/deviceAppManagement/mobileAppConfigurations`
- Targeted managed app configurations: `/deviceAppManagement/targetedManagedAppConfigurations`

**Methods to Implement:**
- [ ] `ListMobileAppConfigurationsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<ManagedDeviceMobileAppConfiguration>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetMobileAppConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `ManagedDeviceMobileAppConfiguration`
  - Throw if null response
- [ ] `CreateMobileAppConfigurationAsync(ManagedDeviceMobileAppConfiguration config, CancellationToken cancellationToken = default)`
  - Returns created `ManagedDeviceMobileAppConfiguration`
  - Throw if null response
- [ ] `UpdateMobileAppConfigurationAsync(ManagedDeviceMobileAppConfiguration config, CancellationToken cancellationToken = default)`
  - Returns updated `ManagedDeviceMobileAppConfiguration`
  - Throw if null response
- [ ] `DeleteMobileAppConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `ListTargetedManagedAppConfigurationsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<TargetedManagedAppConfiguration>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetTargetedManagedAppConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `TargetedManagedAppConfiguration`
  - Throw if null response
- [ ] `CreateTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration config, CancellationToken cancellationToken = default)`
  - Returns created `TargetedManagedAppConfiguration`
  - Throw if null response
- [ ] `UpdateTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration config, CancellationToken cancellationToken = default)`
  - Returns updated `TargetedManagedAppConfiguration`
  - Throw if null response
- [ ] `DeleteTargetedManagedAppConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default)`
  - Returns assignments if supported
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)

### 3. Terms and Conditions Service

**Interface:** `ITermsAndConditionsService`  
**Class:** `TermsAndConditionsService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/termsAndConditions`

**Methods to Implement:**
- [ ] `ListTermsAndConditionsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<TermsAndConditions>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetTermsAndConditionsAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `TermsAndConditions`
  - Throw if null response
- [ ] `CreateTermsAndConditionsAsync(TermsAndConditions terms, CancellationToken cancellationToken = default)`
  - Returns created `TermsAndConditions`
  - Throw if null response
- [ ] `UpdateTermsAndConditionsAsync(TermsAndConditions terms, CancellationToken cancellationToken = default)`
  - Returns updated `TermsAndConditions`
  - Throw if null response
- [ ] `DeleteTermsAndConditionsAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string termsId, CancellationToken cancellationToken = default)`
  - Returns assignments if supported
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)

## Scaffolding Steps

### Core Service Setup
- [ ] Add interface files in `src/Intune.Commander.Core/Services/`
- [ ] Add implementation classes in `src/Intune.Commander.Core/Services/`
- [ ] Ensure constructors accept `GraphServiceClient`
- [ ] Add all service methods with proper signatures
- [ ] Implement manual `@odata.nextLink` pagination in all list methods
- [ ] Ensure all models use `Microsoft.Graph.Beta.Models` types
- [ ] Add null checks and throw on null responses for create/update/get

### Export/Import Integration
- [ ] Extend `ExportService` to handle Wave 2 objects
- [ ] Extend `ImportService` to handle Wave 2 objects
- [ ] Create export wrapper models if needed (e.g., with assignments)
- [ ] Add subfolder paths for each type in export structure
- [ ] Maintain migration-table compatibility

### Desktop UI Integration
- [ ] Add collections in `MainWindowViewModel` for each type
- [ ] Add selection properties for each type
- [ ] Add DataGrid column configurations
- [ ] Add navigation category entries
- [ ] Implement lazy-load handlers
- [ ] Add cache invalidation keys
- [ ] Wire up export/import commands for new types

### Testing
- [ ] Add `AppProtectionServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations if applicable
- [ ] Add `ManagedAppConfigurationServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation for both mobile and targeted configurations
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations if applicable
- [ ] Add `TermsAndConditionsServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling

## Definition of Done

- [ ] All services compile without errors
- [ ] Unit tests added and passing for all services
- [ ] Manual pagination used on every list method
- [ ] CancellationToken passed to all Graph API calls
- [ ] No UI-thread sync blocking introduced
- [ ] Services consumed by desktop ViewModel
- [ ] Export/Import functionality working for all types
- [ ] UI displays and allows interaction with all new types

## Notes

- App Protection policies may have platform-specific variants (iOS/Android/Windows)
- Managed App Configurations include both device-targeted and app-targeted configurations
- Assignment patterns may differ between policy types — validate in Graph API documentation
- Terms and Conditions may have acceptance tracking endpoints to explore
