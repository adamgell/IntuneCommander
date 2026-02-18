# Intune Commander Service Build Plan

## Goals
- Continue migration from PowerShell module object handlers to typed Core services.
- Keep service-per-type architecture with Graph SDK models and async/cancellation support.
- Preserve reliability requirements: manual `@odata.nextLink` pagination, no `PageIterator`.

## Core Service Conventions (Current)
- Interface + implementation per object type in `src/IntuneManager.Core/Services/`.
- Constructor takes `GraphServiceClient`.
- Async methods with `CancellationToken cancellationToken = default`.
- List methods return `List<T>` and implement manual pagination:
  - Initial query uses `$top=999`
  - Follow `response.OdataNextLink` with `.WithUrl(response.OdataNextLink)`
- CRUD methods throw on null create/update responses.
- Assignment pattern where supported:
  - `GetAssignmentsAsync(id)`
  - `Assign...Async(id, List<TAssignment>)`

## Service Backlog (Prioritized)

### Wave 1 (Scaffold now, complete first)
1. Endpoint Security (`/deviceManagement/intents`)
2. Administrative Templates (`/deviceManagement/groupPolicyConfigurations`)
3. Enrollment Configurations (`/deviceManagement/deviceEnrollmentConfigurations`)

### Wave 2 (App and policy plane)
4. App Protection (`/deviceAppManagement/managedAppPolicies`)
5. Managed App Configurations:
   - `/deviceAppManagement/mobileAppConfigurations`
   - `/deviceAppManagement/targetedManagedAppConfigurations`
6. Terms and Conditions (`/deviceManagement/termsAndConditions`)

### Wave 3 (Tenant admin plane)
7. Scope Tags (`/deviceManagement/roleScopeTags`)
8. Role Definitions (`/deviceManagement/roleDefinitions`)
9. Intune Branding (`/deviceManagement/intuneBrandingProfiles`)
10. Azure Branding (`/organization/{organizationId}/branding/localizations`)

### Wave 4 (Enrollment and updates)
11. Autopilot (`/deviceManagement/windowsAutopilotDeploymentProfiles`)
12. Device Health Scripts (`/deviceManagement/deviceHealthScripts`)
13. Mac Custom Attributes (`/deviceManagement/deviceCustomAttributeShellScripts`)
14. Feature Updates (`/deviceManagement/windowsFeatureUpdateProfiles`)

### Wave 5 (Conditional Access adjacent)
15. Named Locations (`/identity/conditionalAccess/namedLocations`)
16. Authentication Strengths (`/identity/conditionalAccess/authenticationStrengths/policies`)
17. Authentication Contexts (`/identity/conditionalAccess/authenticationContextClassReferences`)
18. Terms of Use (`/identityGovernance/termsOfUse/agreements`)

## Wave 1 Concrete Scaffold Checklist

For each service:
- [ ] Add `I<Type>Service` interface in `Core/Services/`
- [ ] Add `<Type>Service` implementation in `Core/Services/`
- [ ] Add list/get/create/update/delete methods
- [ ] Add assignments methods where API exposes assignments + assign action
- [ ] Use manual `@odata.nextLink` pagination in all list methods
- [ ] Ensure model types are from `Microsoft.Graph.Beta.Models`

### 1) Endpoint Security
- Interface: `IEndpointSecurityService`
- Class: `EndpointSecurityService`
- Endpoints:
  - List/Get/Create/Update/Delete: `/deviceManagement/intents`
  - Assignments: `/deviceManagement/intents/{id}/assignments`
  - Assign action: `/deviceManagement/intents/{id}/assign`
- Methods:
  - `ListEndpointSecurityIntentsAsync`
  - `GetEndpointSecurityIntentAsync`
  - `CreateEndpointSecurityIntentAsync`
  - `UpdateEndpointSecurityIntentAsync`
  - `DeleteEndpointSecurityIntentAsync`
  - `GetAssignmentsAsync`
  - `AssignIntentAsync`

### 2) Administrative Templates
- Interface: `IAdministrativeTemplateService`
- Class: `AdministrativeTemplateService`
- Endpoints:
  - List/Get/Create/Update/Delete: `/deviceManagement/groupPolicyConfigurations`
  - Assignments: `/deviceManagement/groupPolicyConfigurations/{id}/assignments`
  - Assign action: `/deviceManagement/groupPolicyConfigurations/{id}/assign`
- Methods:
  - `ListAdministrativeTemplatesAsync`
  - `GetAdministrativeTemplateAsync`
  - `CreateAdministrativeTemplateAsync`
  - `UpdateAdministrativeTemplateAsync`
  - `DeleteAdministrativeTemplateAsync`
  - `GetAssignmentsAsync`
  - `AssignAdministrativeTemplateAsync`

### 3) Enrollment Configurations
- Interface: `IEnrollmentConfigurationService`
- Class: `EnrollmentConfigurationService`
- Endpoint:
  - Base collection: `/deviceManagement/deviceEnrollmentConfigurations`
- Methods:
  - `ListEnrollmentConfigurationsAsync`
  - `ListEnrollmentStatusPagesAsync` (ESP subset)
  - `ListEnrollmentRestrictionsAsync` (restrictions subset)
  - `ListCoManagementSettingsAsync` (co-management subset)
  - `GetEnrollmentConfigurationAsync`
  - `CreateEnrollmentConfigurationAsync`
  - `UpdateEnrollmentConfigurationAsync`
  - `DeleteEnrollmentConfigurationAsync`

## Delivery Plan

### Phase A — Scaffold (this change)
- Add Wave 1 interfaces and service classes with full list/get/create/update/delete signatures.
- Implement manual pagination loops and baseline filtering helpers.

### Phase B — Functional Completion
- Endpoint Security:
  - Add intent-specific import/export normalization helpers.
  - Add reusable-settings resolution path.
- Administrative Templates:
  - Add definition/presentation traversal methods used by import/export.
  - Add file import/copy helper entry points.
- Enrollment:
  - Validate and harden subtype filters for ESP/Restrictions/Co-management.
  - Add assignment methods if graph model path is confirmed in SDK.

### Phase C — Integrate Into Desktop
- Add collections + selection properties in `MainWindowViewModel`.
- Add category entries, lazy-load handlers, and cache keys.
- Add DataGrid column configs for each new type.

### Phase D — Export/Import Integration
- Extend `ExportService` + `ImportService` for Wave 1 objects.
- Preserve migration-table compatibility.

### Phase E — Tests
- Add `IntuneManager.Core.Tests` for each new service:
  - pagination continuation
  - list/get/create/update/delete success + null handling
  - assignment operations (where implemented)

## Definition of Done (per service)
- Compiles and unit tests added/passing.
- Manual pagination used on every list method.
- Cancellation token passed to all graph calls.
- No UI-thread sync blocking introduced.
- Service methods are consumed by desktop view model paths (for user-facing features).
