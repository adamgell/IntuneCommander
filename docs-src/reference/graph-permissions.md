# Graph API Permissions

This page lists every Microsoft Graph permission required by an Intune Commander app registration, mapped to the services that use them.

!!! info "Delegated permissions"
    All permissions listed here are **Delegated** (user context). They require **admin consent** in your tenant. The app uses the `https://graph.microsoft.com/.default` scope, which grants whatever permissions are consented on the registration.

## Intune — Device Management

| Permission | Access | Used by |
|---|---|---|
| `DeviceManagementConfiguration.ReadWrite.All` | Read & write | Device Configs, Compliance, Admin Templates, Endpoint Security, Settings Catalog, Feature Updates, Assignment Filters, Quality/Driver Updates, Compliance Scripts, ADMX Files |
| `DeviceManagementScripts.ReadWrite.All` | Read & write | Device Health Scripts, Mac Custom Attributes, Device Shell Scripts |
| `DeviceManagementApps.ReadWrite.All` | Read & write | Applications, App Protection Policies, Managed App Configurations, Policy Sets |
| `DeviceManagementServiceConfig.ReadWrite.All` | Read & write | Enrollment Configurations, Autopilot, Intune Branding, Terms & Conditions, Apple DEP |
| `DeviceManagementRBAC.ReadWrite.All` | Read & write | Role Definitions, Scope Tags |
| `DeviceManagementManagedDevices.Read.All` | Read | Device Categories |

## Windows 365 — Cloud PC

| Permission | Access | Used by |
|---|---|---|
| `CloudPC.ReadWrite.All` | Read & write | Cloud PC Provisioning Policies, Cloud PC User Settings |

!!! note
    Cloud PC permissions require the tenant to have an active Windows 365 licence. Without a licence, these endpoints return HTTP 403 regardless of app permissions.

## Entra ID — Conditional Access & Identity

| Permission | Access | Used by |
|---|---|---|
| `Policy.ReadWrite.ConditionalAccess` | Read & write | Conditional Access Policies, Authentication Strengths, Authentication Contexts, Named Locations |
| `Policy.Read.All` | Read | Conditional Access (read-only fallback) |

## Entra ID — Terms of Use

| Permission | Access | Used by |
|---|---|---|
| `Agreement.ReadWrite.All` | Read & write | Terms of Use |

## Entra ID — Organization & Branding

| Permission | Access | Used by |
|---|---|---|
| `Organization.Read.All` | Read | Azure Branding (org ID resolution) |
| `OrganizationalBranding.ReadWrite.All` | Read & write | Azure Branding |

## Entra ID — Groups

| Permission | Access | Used by |
|---|---|---|
| `Group.Read.All` | Read | Group lookup and search |
| `GroupMember.Read.All` | Read | Group member enumeration |

---

## Graph API endpoints by service

All endpoints use the **Beta** Graph API (`https://graph.microsoft.com/beta/`).

| Service | Graph API path | Operations |
|---|---|---|
| ConfigurationProfileService | `/deviceManagement/deviceConfigurations` | List, Get, Create, Update, Delete, GetAssignments |
| CompliancePolicyService | `/deviceManagement/deviceCompliancePolicies` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| ApplicationService | `/deviceAppManagement/mobileApps` | List, Get, GetAssignments |
| AppProtectionPolicyService | `/deviceAppManagement/managedAppPolicies` | List, Get, Create, Update, Delete |
| AdministrativeTemplateService | `/deviceManagement/groupPolicyConfigurations` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| EndpointSecurityService | `/deviceManagement/intents` | List, Get, Create, Update, Delete, GetAssignments, Assign |
| SettingsCatalogService | `/deviceManagement/configurationPolicies` | List, Get, GetAssignments |
| EnrollmentConfigurationService | `/deviceManagement/deviceEnrollmentConfigurations` | List, Get, Create, Update, Delete |
| AutopilotService | `/deviceManagement/windowsAutopilotDeploymentProfiles` | List, Get, Create, Update, Delete |
| FeatureUpdateProfileService | `/deviceManagement/windowsFeatureUpdateProfiles` | List, Get, Create, Update, Delete |
| QualityUpdateProfileService | `/deviceManagement/windowsQualityUpdateProfiles` | List, Get, Create, Update |
| DriverUpdateProfileService | `/deviceManagement/windowsDriverUpdateProfiles` | List, Get, Create, Update, Delete |
| DeviceHealthScriptService | `/deviceManagement/deviceHealthScripts` | List, Get, Create, Update, Delete |
| MacCustomAttributeService | `/deviceManagement/deviceCustomAttributeShellScripts` | List, Get, Create, Update, Delete |
| DeviceShellScriptService | `/deviceManagement/deviceShellScripts` | List, Get |
| ComplianceScriptService | `/deviceManagement/deviceComplianceScripts` | List, Get |
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
| ManagedAppConfigurationService | `/deviceAppManagement/mobileAppConfigurations` + `/targetedManagedAppConfigurations` | List, Get, Create, Update, Delete |
| PolicySetService | `/deviceAppManagement/policySets` | List, Get |
| ConditionalAccessPolicyService | `/identity/conditionalAccess/policies` | List, Get |
| AuthenticationStrengthService | `/identity/conditionalAccess/authenticationStrength/policies` | List, Get, Create, Update, Delete |
| AuthenticationContextService | `/identity/conditionalAccess/authenticationContextClassReferences` | List, Get, Create, Update, Delete |
| NamedLocationService | `/identity/conditionalAccess/namedLocations` | List, Get, Create, Update, Delete |
| TermsOfUseService | `/identityGovernance/termsOfUse/agreements` | List, Get, Create, Update, Delete |
| AzureBrandingService | `/organization/{id}/branding/localizations` | List, Get, Create, Update, Delete |
| GroupService | `/groups` + `/groups/{id}/members` | List, Search, GetMembers, GetMemberCounts |

## Notes

- `ReadWrite` permissions are supersets that include read access — no separate `Read.All` permission is needed for services that also write.
- `Policy.ReadWrite.ConditionalAccess` covers `Policy.Read.All` for Conditional Access resources.
- For GCC-High and DoD clouds, the authority and Graph base URL differ from Commercial — Intune Commander handles this automatically based on the cloud selected in your profile.
