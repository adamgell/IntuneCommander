# Graph API Permissions — Intune Commander Integration Tests

This document tracks every Microsoft Graph permission required by the Intune Commander app registration,
mapped to the services and endpoints that need them.

## Application Permissions (Required)

All permissions below are **Application** type (not Delegated) and require **admin consent**.

### Intune — Device Management

| Permission | Access | Services |
|---|---|---|
| `DeviceManagementConfiguration.ReadWrite.All` | Read & write | ConfigurationProfile, Compliance, AdministrativeTemplate, EndpointSecurity, SettingsCatalog, FeatureUpdate, AssignmentFilter, QualityUpdateProfile, DriverUpdateProfile, ComplianceScript, AdmxFile |
| `DeviceManagementScripts.ReadWrite.All` | Read & write | DeviceHealthScript, MacCustomAttribute, DeviceShellScript |
| `DeviceManagementApps.ReadWrite.All` | Read & write | Application, AppProtectionPolicy, ManagedAppConfiguration, PolicySet |
| `DeviceManagementServiceConfig.ReadWrite.All` | Read & write | EnrollmentConfiguration, Autopilot, IntuneBranding, TermsAndConditions, AppleDep |
| `DeviceManagementRBAC.ReadWrite.All` | Read & write | RoleDefinition, ScopeTag |
| `DeviceManagementManagedDevices.Read.All` | Read | Device queries (future), DeviceCategory |

### Windows 365 — Cloud PC

| Permission | Access | Services |
|---|---|---|
| `CloudPC.ReadWrite.All` | Read & write | CloudPcProvisioningService, CloudPcUserSettingsService |

> **Note:** Cloud PC permissions require the tenant to have an active Windows 365 licence. Without a licence the endpoints return HTTP 403 regardless of app permissions.

### Entra ID — Conditional Access & Identity

| Permission | Access | Services |
|---|---|---|
| `Policy.ReadWrite.ConditionalAccess` | Read & write | ConditionalAccessPolicy, AuthenticationStrength, AuthenticationContext, NamedLocation |
| `Policy.Read.All` | Read | ConditionalAccessPolicy (read-only fallback) |

### Entra ID — Terms of Use

| Permission | Access | Services |
|---|---|---|
| `Agreement.ReadWrite.All` | Read & write | TermsOfUseService |

### Entra ID — Organization & Branding

| Permission | Access | Services |
|---|---|---|
| `Organization.Read.All` | Read | AzureBranding (org ID resolution) |
| `OrganizationalBranding.ReadWrite.All` | Read & write | AzureBranding |

### Entra ID — Groups

| Permission | Access | Services |
|---|---|---|
| `Group.Read.All` | Read | GroupService (list, search) |
| `GroupMember.Read.All` | Read | GroupService (member enumeration) |

## Graph API Endpoints by Service

| Service | Graph API Path (Beta) | Operations |
|---|---|---|
| ConfigurationProfileService | `/deviceManagement/deviceConfigurations` | List, Get, Create, Update, Delete, GetAssignments |
| CompliancePolicyService | `/deviceManagement/deviceCompliancePolicies` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| ApplicationService | `/deviceAppManagement/mobileApps` | List, Get, GetAssignments |
| AppProtectionPolicyService | `/deviceAppManagement/managedAppPolicies` | List, Get, Create, Update, Delete |
| AdministrativeTemplateService | `/deviceManagement/groupPolicyConfigurations` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| EndpointSecurityService | `/deviceManagement/intents` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| SettingsCatalogService | `/deviceManagement/configurationPolicies` | List, Get, GetAssignments |
| EnrollmentConfigurationService | `/deviceManagement/deviceEnrollmentConfigurations` | List (x4 variants), Get, Create, Update, Delete |
| AutopilotService | `/deviceManagement/windowsAutopilotDeploymentProfiles` | List, Get, Create, Update, Delete |
| FeatureUpdateProfileService | `/deviceManagement/windowsFeatureUpdateProfiles` | List, Get, Create, Update, Delete |
| QualityUpdateProfileService | `/deviceManagement/windowsQualityUpdateProfiles` | List, Get, Create, Update |
| DriverUpdateProfileService | `/deviceManagement/windowsDriverUpdateProfiles` | List, Get, Create, Update, Delete |
| DeviceHealthScriptService | `/deviceManagement/deviceHealthScripts` | List, Get, Create, Update, Delete |
| MacCustomAttributeService | `/deviceManagement/deviceCustomAttributeShellScripts` | List, Get, Create, Update, Delete |
| DeviceShellScriptService | `/deviceManagement/deviceShellScripts` | List, Get (via AssignmentChecker) |
| ComplianceScriptService | `/deviceManagement/deviceComplianceScripts` | List, Get (via AssignmentChecker) |
| AdmxFileService | `/deviceManagement/groupPolicyUploadedDefinitionFiles` | List, Get, Create, Delete |
| AppleDepService | `/deviceManagement/depOnboardingSettings` | List, Get, ListEnrollmentProfiles |
| DeviceCategoryService | `/deviceManagement/deviceCategories` | List, Get |
| IntuneBrandingService | `/deviceManagement/intuneBrandingProfiles` | List, Get, Create, Update, Delete |
| TermsAndConditionsService | `/deviceManagement/termsAndConditions` | List, Get, Create, Update, Delete |
| RoleDefinitionService | `/deviceManagement/roleDefinitions` | List, Get, Create, Update, Delete |
| ScopeTagService | `/deviceManagement/roleScopeTags` | List, Get, Create, Update, Delete |
| AssignmentFilterService | `/deviceManagement/assignmentFilters` | List, Get |
| CloudPcProvisioningService | `/deviceManagement/virtualEndpoint/provisioningPolicies` | List, Get |
| CloudPcUserSettingsService | `/deviceManagement/virtualEndpoint/userSettings` | List, Get |
| ManagedAppConfigurationService | `/deviceAppManagement/mobileAppConfigurations` + `/targetedManagedAppConfigurations` | List, Get, Create, Update, Delete (both) |
| PolicySetService | `/deviceAppManagement/policySets` | List, Get |
| ConditionalAccessPolicyService | `/identity/conditionalAccess/policies` | List, Get |
| AuthenticationStrengthService | `/identity/conditionalAccess/authenticationStrength/policies` | List, Get, Create, Update, Delete |
| AuthenticationContextService | `/identity/conditionalAccess/authenticationContextClassReferences` | List, Get, Create, Update, Delete |
| NamedLocationService | `/identity/conditionalAccess/namedLocations` | List, Get, Create, Update, Delete |
| TermsOfUseService | `/identityGovernance/termsOfUse/agreements` | List, Get, Create, Update, Delete |
| AzureBrandingService | `/organization/{id}/branding/localizations` | List, Get, Create, Update, Delete |
| GroupService | `/groups` + `/groups/{id}/members` | List, Search, GetMembers, GetMemberCounts |

## Notes

- All endpoints use the **Beta** Graph API (`https://graph.microsoft.com/beta/`)
- The app uses `https://graph.microsoft.com/.default` scope, which grants whatever permissions are consented on the registration
- For **GCC/GCC-High/DoD** clouds, the authority and Graph base URL differ — see `CloudEndpoints.GetEndpoints()`
- `ReadWrite` permissions are supersets that include read access, so we don't need separate `Read.All` for services that also write
- `Policy.ReadWrite.ConditionalAccess` covers `Policy.Read.All` for conditional access resources
