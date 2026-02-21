# Wave 1: Endpoint Security, Administrative Templates, and Enrollment Configurations

**Reference:** [docs/SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) — Wave 1

## Scope

Wave 1 focuses on implementing three core service types that are critical for Intune policy management:

1. **Endpoint Security** — Security baseline policies and configuration intents
2. **Administrative Templates** — Group Policy configuration objects (ADMX-backed)
3. **Enrollment Configurations** — Device enrollment settings including ESP, restrictions, and co-management

## Service Implementations

### 1. Endpoint Security Service

**Interface:** `IEndpointSecurityService`  
**Class:** `EndpointSecurityService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/intents`
- Assignments: `/deviceManagement/intents/{id}/assignments`
- Assign action: `/deviceManagement/intents/{id}/assign`

**Methods to Implement:**
- [ ] `ListEndpointSecurityIntentsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceManagementIntent>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetEndpointSecurityIntentAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `DeviceManagementIntent`
  - Throw if null response
- [ ] `CreateEndpointSecurityIntentAsync(DeviceManagementIntent intent, CancellationToken cancellationToken = default)`
  - Returns created `DeviceManagementIntent`
  - Throw if null response
- [ ] `UpdateEndpointSecurityIntentAsync(DeviceManagementIntent intent, CancellationToken cancellationToken = default)`
  - Returns updated `DeviceManagementIntent`
  - Throw if null response
- [ ] `DeleteEndpointSecurityIntentAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string intentId, CancellationToken cancellationToken = default)`
  - Returns `List<DeviceManagementIntentAssignment>`
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)
- [ ] `AssignIntentAsync(string intentId, List<DeviceManagementIntentAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`
  - Use assign action endpoint

### 2. Administrative Template Service

**Interface:** `IAdministrativeTemplateService`  
**Class:** `AdministrativeTemplateService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/groupPolicyConfigurations`
- Assignments: `/deviceManagement/groupPolicyConfigurations/{id}/assignments`
- Assign action: `/deviceManagement/groupPolicyConfigurations/{id}/assign`

**Methods to Implement:**
- [ ] `ListAdministrativeTemplatesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<GroupPolicyConfiguration>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetAdministrativeTemplateAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `GroupPolicyConfiguration`
  - Throw if null response
- [ ] `CreateAdministrativeTemplateAsync(GroupPolicyConfiguration config, CancellationToken cancellationToken = default)`
  - Returns created `GroupPolicyConfiguration`
  - Throw if null response
- [ ] `UpdateAdministrativeTemplateAsync(GroupPolicyConfiguration config, CancellationToken cancellationToken = default)`
  - Returns updated `GroupPolicyConfiguration`
  - Throw if null response
- [ ] `DeleteAdministrativeTemplateAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string configId, CancellationToken cancellationToken = default)`
  - Returns `List<GroupPolicyConfigurationAssignment>`
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)
- [ ] `AssignAdministrativeTemplateAsync(string configId, List<GroupPolicyConfigurationAssignment> assignments, CancellationToken cancellationToken = default)`
  - Returns `Task`
  - Use assign action endpoint

### 3. Enrollment Configuration Service

**Interface:** `IEnrollmentConfigurationService`  
**Class:** `EnrollmentConfigurationService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/deviceEnrollmentConfigurations`

**Methods to Implement:**
- [ ] `ListEnrollmentConfigurationsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceEnrollmentConfiguration>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `ListEnrollmentStatusPagesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceEnrollmentConfiguration>` (ESP subset)
  - Filter by OData type for Enrollment Status Page
- [ ] `ListEnrollmentRestrictionsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceEnrollmentConfiguration>` (restrictions subset)
  - Filter by OData type for restrictions
- [ ] `ListCoManagementSettingsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<DeviceEnrollmentConfiguration>` (co-management subset)
  - Filter by OData type for co-management
- [ ] `GetEnrollmentConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `DeviceEnrollmentConfiguration`
  - Throw if null response
- [ ] `CreateEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration config, CancellationToken cancellationToken = default)`
  - Returns created `DeviceEnrollmentConfiguration`
  - Throw if null response
- [ ] `UpdateEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration config, CancellationToken cancellationToken = default)`
  - Returns updated `DeviceEnrollmentConfiguration`
  - Throw if null response
- [ ] `DeleteEnrollmentConfigurationAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`

## Scaffolding Steps

### Core Service Setup
- [ ] Add interface file in `src/Intune.Commander.Core/Services/`
- [ ] Add implementation class in `src/Intune.Commander.Core/Services/`
- [ ] Ensure constructor accepts `GraphServiceClient`
- [ ] Add all service methods with proper signatures
- [ ] Implement manual `@odata.nextLink` pagination in all list methods
- [ ] Ensure all models use `Microsoft.Graph.Beta.Models` types
- [ ] Add null checks and throw on null responses for create/update/get

### Export/Import Integration
- [ ] Extend `ExportService` to handle Wave 1 objects
- [ ] Extend `ImportService` to handle Wave 1 objects
- [ ] Create export wrapper models if needed (e.g., with assignments)
- [ ] Add subfolder paths for each type in export structure
- [ ] Maintain migration-table compatibility

### Desktop UI Integration
- [ ] Add collections in `MainWindowViewModel` for each type
- [ ] Add selection properties (e.g., `SelectedEndpointSecurityIntent`)
- [ ] Add DataGrid column configurations
- [ ] Add navigation category entries
- [ ] Implement lazy-load handlers
- [ ] Add cache invalidation keys
- [ ] Wire up export/import commands for new types

### Testing
- [ ] Add `EndpointSecurityServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations
- [ ] Add `AdministrativeTemplateServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations
- [ ] Add `EnrollmentConfigurationServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get success paths
  - Test filtering methods (ESP, Restrictions, Co-management)
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

- Endpoint Security intents may require additional helper methods for reusable settings resolution
- Administrative Templates may need definition/presentation traversal methods for import/export
- Enrollment Configuration subtypes (ESP/Restrictions/Co-management) need careful OData type filtering
- Assignment endpoints should be validated in Graph API before implementation
