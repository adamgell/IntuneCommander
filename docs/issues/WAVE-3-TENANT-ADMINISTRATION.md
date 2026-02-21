# Wave 3: Tenant Administration Services

**Reference:** [docs/SERVICE-IMPLEMENTATION-PLAN.md](../SERVICE-IMPLEMENTATION-PLAN.md) — Wave 3

## Scope

Wave 3 focuses on implementing tenant-level administrative services:

1. **Scope Tags** — Role-based access control scope tags
2. **Role Definitions** — Custom role definitions for Intune
3. **Intune Branding** — Intune Company Portal branding profiles
4. **Azure Branding** — Azure AD organization branding

## Service Implementations

### 1. Scope Tag Service

**Interface:** `IScopeTagService`  
**Class:** `ScopeTagService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/roleScopeTags`

**Methods to Implement:**
- [ ] `ListScopeTagsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<RoleScopeTag>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetScopeTagAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `RoleScopeTag`
  - Throw if null response
- [ ] `CreateScopeTagAsync(RoleScopeTag scopeTag, CancellationToken cancellationToken = default)`
  - Returns created `RoleScopeTag`
  - Throw if null response
- [ ] `UpdateScopeTagAsync(RoleScopeTag scopeTag, CancellationToken cancellationToken = default)`
  - Returns updated `RoleScopeTag`
  - Throw if null response
- [ ] `DeleteScopeTagAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string scopeTagId, CancellationToken cancellationToken = default)`
  - Returns assignments/members if supported by API
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)

### 2. Role Definition Service

**Interface:** `IRoleDefinitionService`  
**Class:** `RoleDefinitionService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/roleDefinitions`

**Methods to Implement:**
- [ ] `ListRoleDefinitionsAsync(CancellationToken cancellationToken = default)`
  - Returns `List<RoleDefinition>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetRoleDefinitionAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `RoleDefinition`
  - Throw if null response
- [ ] `CreateRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default)`
  - Returns created `RoleDefinition`
  - Throw if null response
- [ ] `UpdateRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default)`
  - Returns updated `RoleDefinition`
  - Throw if null response
- [ ] `DeleteRoleDefinitionAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetRoleAssignmentsAsync(string roleDefinitionId, CancellationToken cancellationToken = default)`
  - Returns `List<RoleAssignment>` if supported
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)

### 3. Intune Branding Service

**Interface:** `IIntuneBrandingService`  
**Class:** `IntuneBrandingService`  
**Graph Endpoints:**
- Base collection: `/deviceManagement/intuneBrandingProfiles`

**Methods to Implement:**
- [ ] `ListIntuneBrandingProfilesAsync(CancellationToken cancellationToken = default)`
  - Returns `List<IntuneBrandingProfile>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetIntuneBrandingProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `IntuneBrandingProfile`
  - Throw if null response
- [ ] `CreateIntuneBrandingProfileAsync(IntuneBrandingProfile profile, CancellationToken cancellationToken = default)`
  - Returns created `IntuneBrandingProfile`
  - Throw if null response
- [ ] `UpdateIntuneBrandingProfileAsync(IntuneBrandingProfile profile, CancellationToken cancellationToken = default)`
  - Returns updated `IntuneBrandingProfile`
  - Throw if null response
- [ ] `DeleteIntuneBrandingProfileAsync(string id, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetAssignmentsAsync(string profileId, CancellationToken cancellationToken = default)`
  - Returns assignments if supported
  - Return `response?.Value ?? []` (no manual pagination; consistent with existing services)

### 4. Azure Branding Service

**Interface:** `IAzureBrandingService`  
**Class:** `AzureBrandingService`  
**Graph Endpoints:**
- Base collection: `/organization/{organizationId}/branding/localizations`
- Note: May need to get organization ID first

**Methods to Implement:**
- [ ] `ListOrganizationBrandingsAsync(string organizationId, CancellationToken cancellationToken = default)`
  - Returns `List<OrganizationalBrandingLocalization>`
  - Use manual `@odata.nextLink` pagination with `$top=999`
- [ ] `GetOrganizationBrandingAsync(string organizationId, string localizationId, CancellationToken cancellationToken = default)`
  - Returns `OrganizationalBrandingLocalization`
  - Throw if null response
- [ ] `CreateOrganizationBrandingAsync(string organizationId, OrganizationalBrandingLocalization branding, CancellationToken cancellationToken = default)`
  - Returns created `OrganizationalBrandingLocalization`
  - Throw if null response
- [ ] `UpdateOrganizationBrandingAsync(string organizationId, OrganizationalBrandingLocalization branding, CancellationToken cancellationToken = default)`
  - Returns updated `OrganizationalBrandingLocalization`
  - Throw if null response
- [ ] `DeleteOrganizationBrandingAsync(string organizationId, string localizationId, CancellationToken cancellationToken = default)`
  - Returns `Task`
- [ ] `GetCurrentOrganizationIdAsync(CancellationToken cancellationToken = default)`
  - Helper method to retrieve current organization ID
  - Returns `string`

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
- [ ] Extend `ExportService` to handle Wave 3 objects
- [ ] Extend `ImportService` to handle Wave 3 objects
- [ ] Create export wrapper models if needed
- [ ] Add subfolder paths for each type in export structure
- [ ] Maintain migration-table compatibility
- [ ] Consider special handling for organization-scoped resources

### Desktop UI Integration
- [ ] Add collections in `MainWindowViewModel` for each type
- [ ] Add selection properties for each type
- [ ] Add DataGrid column configurations
- [ ] Add navigation category entries (possibly under "Administration")
- [ ] Implement lazy-load handlers
- [ ] Add cache invalidation keys
- [ ] Wire up export/import commands for new types

### Testing
- [ ] Add `ScopeTagServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test assignment operations if applicable
- [ ] Add `RoleDefinitionServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test role assignment retrieval
- [ ] Add `IntuneBrandingServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation
  - Test list/get/create/update/delete success paths
  - Test null response handling
- [ ] Add `AzureBrandingServiceTests.cs` in `tests/Intune.Commander.Core.Tests/Services/`
  - Test pagination continuation with organization ID
  - Test list/get/create/update/delete success paths
  - Test null response handling
  - Test organization ID retrieval

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

- Scope Tags are fundamental to RBAC in Intune — ensure proper handling
- Role Definitions include both built-in and custom roles — may need filtering
- Azure Branding requires organization ID parameter — may need helper method to get current org
- Branding may include binary assets (logos, backgrounds) — consider special handling for export/import
- Some resources may be read-only (built-in roles) — handle appropriately in UI
