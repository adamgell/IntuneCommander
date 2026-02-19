using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Linq;

using Microsoft.Graph.Beta.Models;



namespace IntuneManager.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{



    public ObservableCollection<NavCategory> NavCategories { get; } = [];



    private static List<NavCategory> BuildDefaultNavCategories() =>

    [

        new NavCategory { Name = "Overview", Icon = "📊" },

        new NavCategory { Name = "Device Configurations", Icon = "⚙" },

        new NavCategory { Name = "Compliance Policies", Icon = "✓" },

        new NavCategory { Name = "Applications", Icon = "📦" },

        new NavCategory { Name = "Application Assignments", Icon = "📋" },

        new NavCategory { Name = "Settings Catalog", Icon = "⚙" },

        new NavCategory { Name = "Endpoint Security", Icon = "🛡" },

        new NavCategory { Name = "Administrative Templates", Icon = "🧾" },

        new NavCategory { Name = "Enrollment Configurations", Icon = "🪪" },

        new NavCategory { Name = "App Protection Policies", Icon = "🔒" },

        new NavCategory { Name = "Managed Device App Configurations", Icon = "📱" },

        new NavCategory { Name = "Targeted Managed App Configurations", Icon = "🎯" },

        new NavCategory { Name = "Terms and Conditions", Icon = "📜" },

        new NavCategory { Name = "Scope Tags", Icon = "🏷" },

        new NavCategory { Name = "Role Definitions", Icon = "💼" },

        new NavCategory { Name = "Intune Branding", Icon = "🎨" },

        new NavCategory { Name = "Azure Branding", Icon = "🟦" },

        new NavCategory { Name = "Autopilot Profiles", Icon = "🚀" },

        new NavCategory { Name = "Device Health Scripts", Icon = "🩺" },

        new NavCategory { Name = "Mac Custom Attributes", Icon = "🍎" },

        new NavCategory { Name = "Feature Updates", Icon = "🪟" },

        new NavCategory { Name = "Named Locations", Icon = "📍" },

        new NavCategory { Name = "Authentication Strengths", Icon = "🔐" },

        new NavCategory { Name = "Authentication Contexts", Icon = "🏷" },

        new NavCategory { Name = "Terms of Use", Icon = "📄" },

        new NavCategory { Name = "Conditional Access", Icon = "🔐" },

        new NavCategory { Name = "Assignment Filters", Icon = "🧩" },

        new NavCategory { Name = "Policy Sets", Icon = "🗂" },

        new NavCategory { Name = "Dynamic Groups", Icon = "🔄" },

        new NavCategory { Name = "Assigned Groups", Icon = "👥" }

    ];





    private void EnsureNavCategories()

    {

        var expected = BuildDefaultNavCategories();



        var isSame = NavCategories.Count == expected.Count &&

                     NavCategories.Select(c => c.Name).SequenceEqual(expected.Select(c => c.Name));



        if (isSame)

        {

            DebugLog.Log("App", $"Nav categories active ({NavCategories.Count})");

            return;

        }



        NavCategories.Clear();

        foreach (var category in expected)

            NavCategories.Add(category);



        DebugLog.Log("App", $"Nav categories rebuilt ({NavCategories.Count}): {string.Join(", ", NavCategories.Select(c => c.Name))}");

    }





    /// <summary>

    /// Returns the column configs for the currently selected nav category.

    /// </summary>

    public ObservableCollection<DataGridColumnConfig>? ActiveColumns => SelectedCategory?.Name switch

    {

        "Device Configurations" => DeviceConfigColumns,

        "Compliance Policies" => CompliancePolicyColumns,

        "Applications" => ApplicationColumns,

        "Application Assignments" => AppAssignmentColumns,

        "Settings Catalog" => SettingsCatalogColumns,

        "Endpoint Security" => EndpointSecurityColumns,

        "Administrative Templates" => AdministrativeTemplateColumns,

        "Enrollment Configurations" => EnrollmentConfigurationColumns,

        "App Protection Policies" => AppProtectionPolicyColumns,

        "Managed Device App Configurations" => ManagedDeviceAppConfigurationColumns,

        "Targeted Managed App Configurations" => TargetedManagedAppConfigurationColumns,

        "Terms and Conditions" => TermsAndConditionsColumns,

        "Scope Tags" => ScopeTagColumns,

        "Role Definitions" => RoleDefinitionColumns,

        "Intune Branding" => IntuneBrandingColumns,

        "Azure Branding" => AzureBrandingColumns,

        "Conditional Access" => ConditionalAccessColumns,

        "Assignment Filters" => AssignmentFilterColumns,

        "Policy Sets" => PolicySetColumns,

        "Autopilot Profiles" => AutopilotProfileColumns,

        "Device Health Scripts" => DeviceHealthScriptColumns,

        "Mac Custom Attributes" => MacCustomAttributeColumns,

        "Feature Updates" => FeatureUpdateProfileColumns,

        "Named Locations" => NamedLocationColumns,

        "Authentication Strengths" => AuthenticationStrengthColumns,

        "Authentication Contexts" => AuthenticationContextColumns,

        "Terms of Use" => TermsOfUseColumns,

        "Dynamic Groups" => DynamicGroupColumns,

        "Assigned Groups" => AssignedGroupColumns,

        _ => null

    };





    /// <summary>

    /// Maps an OData type string to an inferred platform name.

    /// </summary>

    public static string InferPlatform(string? odataType)

    {

        if (string.IsNullOrEmpty(odataType)) return "";

        var lower = odataType.ToLowerInvariant();

        if (lower.Contains("windows") || lower.Contains("win32") || lower.Contains("msi")) return "Windows";

        if (lower.Contains("ios") || lower.Contains("iphone")) return "iOS";

        if (lower.Contains("macos") || lower.Contains("mac")) return "macOS";

        if (lower.Contains("android")) return "Android";

        if (lower.Contains("webapp")) return "Web";

        return "Cross-platform";

    }



    // Computed visibility helpers for the view

    public bool IsOverviewCategory => SelectedCategory?.Name == "Overview";

    public bool IsDeviceConfigCategory => SelectedCategory?.Name == "Device Configurations";

    public bool IsCompliancePolicyCategory => SelectedCategory?.Name == "Compliance Policies";

    public bool IsApplicationCategory => SelectedCategory?.Name == "Applications";

    public bool IsAppAssignmentsCategory => SelectedCategory?.Name == "Application Assignments";

    public bool IsSettingsCatalogCategory => SelectedCategory?.Name == "Settings Catalog";

    public bool IsEndpointSecurityCategory => SelectedCategory?.Name == "Endpoint Security";

    public bool IsAdministrativeTemplatesCategory => SelectedCategory?.Name == "Administrative Templates";

    public bool IsEnrollmentConfigurationsCategory => SelectedCategory?.Name == "Enrollment Configurations";

    public bool IsAppProtectionPoliciesCategory => SelectedCategory?.Name == "App Protection Policies";

    public bool IsManagedDeviceAppConfigurationsCategory => SelectedCategory?.Name == "Managed Device App Configurations";

    public bool IsTargetedManagedAppConfigurationsCategory => SelectedCategory?.Name == "Targeted Managed App Configurations";

    public bool IsTermsAndConditionsCategory => SelectedCategory?.Name == "Terms and Conditions";

    public bool IsScopeTagsCategory => SelectedCategory?.Name == "Scope Tags";

    public bool IsRoleDefinitionsCategory => SelectedCategory?.Name == "Role Definitions";

    public bool IsIntuneBrandingCategory => SelectedCategory?.Name == "Intune Branding";

    public bool IsAzureBrandingCategory => SelectedCategory?.Name == "Azure Branding";

    public bool IsConditionalAccessCategory => SelectedCategory?.Name == "Conditional Access";

    public bool IsAssignmentFiltersCategory => SelectedCategory?.Name == "Assignment Filters";

    public bool IsPolicySetsCategory => SelectedCategory?.Name == "Policy Sets";

    public bool IsAutopilotProfilesCategory => SelectedCategory?.Name == "Autopilot Profiles";

    public bool IsDeviceHealthScriptsCategory => SelectedCategory?.Name == "Device Health Scripts";

    public bool IsMacCustomAttributesCategory => SelectedCategory?.Name == "Mac Custom Attributes";

    public bool IsFeatureUpdatesCategory => SelectedCategory?.Name == "Feature Updates";

    public bool IsNamedLocationsCategory => SelectedCategory?.Name == "Named Locations";

    public bool IsAuthenticationStrengthsCategory => SelectedCategory?.Name == "Authentication Strengths";

    public bool IsAuthenticationContextsCategory => SelectedCategory?.Name == "Authentication Contexts";

    public bool IsTermsOfUseCategory => SelectedCategory?.Name == "Terms of Use";

    public bool IsDynamicGroupsCategory => SelectedCategory?.Name == "Dynamic Groups";

    public bool IsAssignedGroupsCategory => SelectedCategory?.Name == "Assigned Groups";



    partial void OnSelectedCategoryChanged(NavCategory? value)

    {

        // Clear selections when switching categories

        SelectedConfiguration = null;

        SelectedCompliancePolicy = null;

        SelectedApplication = null;

        SelectedAppAssignmentRow = null;

        SelectedSettingsCatalogPolicy = null;

        SelectedEndpointSecurityIntent = null;

        SelectedAdministrativeTemplate = null;

        SelectedEnrollmentConfiguration = null;

        SelectedAppProtectionPolicy = null;

        SelectedManagedDeviceAppConfiguration = null;

        SelectedTargetedManagedAppConfiguration = null;

        SelectedTermsAndConditions = null;

        SelectedScopeTag = null;

        SelectedRoleDefinition = null;

        SelectedIntuneBrandingProfile = null;

        SelectedAzureBrandingLocalization = null;

        SelectedConditionalAccessPolicy = null;

        SelectedAssignmentFilter = null;

        SelectedPolicySet = null;

        SelectedAutopilotProfile = null;

        SelectedDeviceHealthScript = null;

        SelectedMacCustomAttribute = null;

        SelectedFeatureUpdateProfile = null;

        SelectedNamedLocation = null;

        SelectedAuthenticationStrengthPolicy = null;

        SelectedAuthenticationContextClassReference = null;

        SelectedTermsOfUseAgreement = null;

        SelectedDynamicGroupRow = null;

        SelectedAssignedGroupRow = null;

        SelectedItemAssignments.Clear();

        SelectedGroupMembers.Clear();

        SelectedItemTypeName = "";

        SelectedItemPlatform = "";



        // Notify category/column changes BEFORE resetting SearchText.

        // SearchText = "" triggers ApplyFilter → data rebinding, and columns

        // must already reflect the new category to avoid binding mismatches

        // (e.g. AppAssignmentColumns applied to MobileApp objects).

        OnPropertyChanged(nameof(IsOverviewCategory));

        OnPropertyChanged(nameof(IsDeviceConfigCategory));

        OnPropertyChanged(nameof(IsCompliancePolicyCategory));

        OnPropertyChanged(nameof(IsApplicationCategory));

        OnPropertyChanged(nameof(IsAppAssignmentsCategory));

        OnPropertyChanged(nameof(IsSettingsCatalogCategory));

        OnPropertyChanged(nameof(IsEndpointSecurityCategory));

        OnPropertyChanged(nameof(IsAdministrativeTemplatesCategory));

        OnPropertyChanged(nameof(IsEnrollmentConfigurationsCategory));

        OnPropertyChanged(nameof(IsAppProtectionPoliciesCategory));

        OnPropertyChanged(nameof(IsManagedDeviceAppConfigurationsCategory));

        OnPropertyChanged(nameof(IsTargetedManagedAppConfigurationsCategory));

        OnPropertyChanged(nameof(IsTermsAndConditionsCategory));

        OnPropertyChanged(nameof(IsScopeTagsCategory));

        OnPropertyChanged(nameof(IsRoleDefinitionsCategory));

        OnPropertyChanged(nameof(IsIntuneBrandingCategory));

        OnPropertyChanged(nameof(IsAzureBrandingCategory));

        OnPropertyChanged(nameof(IsConditionalAccessCategory));

        OnPropertyChanged(nameof(IsAssignmentFiltersCategory));

        OnPropertyChanged(nameof(IsPolicySetsCategory));

        OnPropertyChanged(nameof(IsAutopilotProfilesCategory));

        OnPropertyChanged(nameof(IsDeviceHealthScriptsCategory));

        OnPropertyChanged(nameof(IsMacCustomAttributesCategory));

        OnPropertyChanged(nameof(IsFeatureUpdatesCategory));

        OnPropertyChanged(nameof(IsNamedLocationsCategory));

        OnPropertyChanged(nameof(IsAuthenticationStrengthsCategory));

        OnPropertyChanged(nameof(IsAuthenticationContextsCategory));

        OnPropertyChanged(nameof(IsTermsOfUseCategory));

        OnPropertyChanged(nameof(IsDynamicGroupsCategory));

        OnPropertyChanged(nameof(IsAssignedGroupsCategory));

        OnPropertyChanged(nameof(ActiveColumns));

        OnPropertyChanged(nameof(CanRefreshSelectedItem));



        SearchText = "";



        // Lazy-load assignments when navigating to tabs that require them

        if ((value?.Name == "Application Assignments" || value?.Name == "Overview") && !_appAssignmentsLoaded)

        {

            if (!TryLoadLazyCacheEntry<AppAssignmentRow>(CacheKeyAppAssignments, rows =>

            {

                AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(rows);

                _appAssignmentsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} app assignment(s) from cache";



                // Update Overview dashboard when assignments loaded from cache

                Overview.Update(

                    ActiveProfile,

                    (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,

                    (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,

                    (IReadOnlyList<MobileApp>)Applications,

                    (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);

            }))

            {

                _ = LoadAppAssignmentRowsAsync();

            }

        }



        // Lazy-load group views

        if (value?.Name == "Dynamic Groups" && !_dynamicGroupsLoaded)

        {

            if (!TryLoadLazyCacheEntry<GroupRow>(CacheKeyDynamicGroups, rows =>

            {

                DynamicGroupRows = new ObservableCollection<GroupRow>(rows);

                _dynamicGroupsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} dynamic group(s) from cache";

            }))

            {

                _ = LoadDynamicGroupRowsAsync();

            }

        }

        if (value?.Name == "Assigned Groups" && !_assignedGroupsLoaded)

        {

            if (!TryLoadLazyCacheEntry<GroupRow>(CacheKeyAssignedGroups, rows =>

            {

                AssignedGroupRows = new ObservableCollection<GroupRow>(rows);

                _assignedGroupsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} assigned group(s) from cache";

            }))

            {

                _ = LoadAssignedGroupRowsAsync();

            }

        }



        if (value?.Name == "Conditional Access" && !_conditionalAccessLoaded)

        {

            if (!TryLoadLazyCacheEntry<ConditionalAccessPolicy>(CacheKeyConditionalAccess, rows =>

            {

                ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(rows);

                _conditionalAccessLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} conditional access policy(ies) from cache";

            }))

            {

                _ = LoadConditionalAccessPoliciesAsync();

            }

        }



        if (value?.Name == "Assignment Filters" && !_assignmentFiltersLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceAndAppManagementAssignmentFilter>(CacheKeyAssignmentFilters, rows =>

            {

                AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(rows);

                _assignmentFiltersLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} assignment filter(s) from cache";

            }))

            {

                _ = LoadAssignmentFiltersAsync();

            }

        }



        if (value?.Name == "Policy Sets" && !_policySetsLoaded)

        {

            if (!TryLoadLazyCacheEntry<PolicySet>(CacheKeyPolicySets, rows =>

            {

                PolicySets = new ObservableCollection<PolicySet>(rows);

                _policySetsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} policy set(s) from cache";

            }))

            {

                _ = LoadPolicySetsAsync();

            }

        }



        if (value?.Name == "Endpoint Security" && !_endpointSecurityLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceManagementIntent>(CacheKeyEndpointSecurity, rows =>

            {

                EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(rows);

                _endpointSecurityLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} endpoint security intent(s) from cache";

            }))

            {

                _ = LoadEndpointSecurityIntentsAsync();

            }

        }



        if (value?.Name == "Administrative Templates" && !_administrativeTemplatesLoaded)

        {

            if (!TryLoadLazyCacheEntry<GroupPolicyConfiguration>(CacheKeyAdministrativeTemplates, rows =>

            {

                AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(rows);

                _administrativeTemplatesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} administrative template(s) from cache";

            }))

            {

                _ = LoadAdministrativeTemplatesAsync();

            }

        }



        if (value?.Name == "Enrollment Configurations" && !_enrollmentConfigurationsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceEnrollmentConfiguration>(CacheKeyEnrollmentConfigurations, rows =>

            {

                EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(rows);

                _enrollmentConfigurationsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} enrollment configuration(s) from cache";

            }))

            {

                _ = LoadEnrollmentConfigurationsAsync();

            }

        }



        if (value?.Name == "App Protection Policies" && !_appProtectionPoliciesLoaded)

        {

            if (!TryLoadLazyCacheEntry<ManagedAppPolicy>(CacheKeyAppProtectionPolicies, rows =>

            {

                AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(rows);

                _appProtectionPoliciesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} app protection policy(ies) from cache";

            }))

            {

                _ = LoadAppProtectionPoliciesAsync();

            }

        }



        if (value?.Name == "Managed Device App Configurations" && !_managedDeviceAppConfigurationsLoaded)

        {

            if (!TryLoadLazyCacheEntry<ManagedDeviceMobileAppConfiguration>(CacheKeyManagedDeviceAppConfigurations, rows =>

            {

                ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(rows);

                _managedDeviceAppConfigurationsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} managed device app configuration(s) from cache";

            }))

            {

                _ = LoadManagedDeviceAppConfigurationsAsync();

            }

        }



        if (value?.Name == "Targeted Managed App Configurations" && !_targetedManagedAppConfigurationsLoaded)

        {

            if (!TryLoadLazyCacheEntry<TargetedManagedAppConfiguration>(CacheKeyTargetedManagedAppConfigurations, rows =>

            {

                TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(rows);

                _targetedManagedAppConfigurationsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} targeted managed app configuration(s) from cache";

            }))

            {

                _ = LoadTargetedManagedAppConfigurationsAsync();

            }

        }



        if (value?.Name == "Terms and Conditions" && !_termsAndConditionsLoaded)

        {

            if (!TryLoadLazyCacheEntry<TermsAndConditions>(CacheKeyTermsAndConditions, rows =>

            {

                TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(rows);

                _termsAndConditionsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} terms and conditions item(s) from cache";

            }))

            {

                _ = LoadTermsAndConditionsAsync();

            }

        }



        if (value?.Name == "Scope Tags" && !_scopeTagsLoaded)

        {

            if (!TryLoadLazyCacheEntry<RoleScopeTag>(CacheKeyScopeTags, rows =>

            {

                ScopeTags = new ObservableCollection<RoleScopeTag>(rows);

                _scopeTagsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} scope tag(s) from cache";

            }))

            {

                _ = LoadScopeTagsAsync();

            }

        }



        if (value?.Name == "Role Definitions" && !_roleDefinitionsLoaded)

        {

            if (!TryLoadLazyCacheEntry<RoleDefinition>(CacheKeyRoleDefinitions, rows =>

            {

                RoleDefinitions = new ObservableCollection<RoleDefinition>(rows);

                _roleDefinitionsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} role definition(s) from cache";

            }))

            {

                _ = LoadRoleDefinitionsAsync();

            }

        }



        if (value?.Name == "Intune Branding" && !_intuneBrandingProfilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<IntuneBrandingProfile>(CacheKeyIntuneBrandingProfiles, rows =>

            {

                IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(rows);

                _intuneBrandingProfilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} Intune branding profile(s) from cache";

            }))

            {

                _ = LoadIntuneBrandingProfilesAsync();

            }

        }



        if (value?.Name == "Azure Branding" && !_azureBrandingLocalizationsLoaded)

        {

            if (!TryLoadLazyCacheEntry<OrganizationalBrandingLocalization>(CacheKeyAzureBrandingLocalizations, rows =>

            {

                AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(rows);

                _azureBrandingLocalizationsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} Azure branding localization(s) from cache";

            }))

            {

                _ = LoadAzureBrandingLocalizationsAsync();

            }

        }



        if (value?.Name == "Autopilot Profiles" && !_autopilotProfilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<WindowsAutopilotDeploymentProfile>(CacheKeyAutopilotProfiles, rows =>

            {

                AutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(rows);

                _autopilotProfilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} autopilot profile(s) from cache";

            }))

            {

                _ = LoadAutopilotProfilesAsync();

            }

        }



        if (value?.Name == "Device Health Scripts" && !_deviceHealthScriptsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceHealthScript>(CacheKeyDeviceHealthScripts, rows =>

            {

                DeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(rows);

                _deviceHealthScriptsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} device health script(s) from cache";

            }))

            {

                _ = LoadDeviceHealthScriptsAsync();

            }

        }



        if (value?.Name == "Mac Custom Attributes" && !_macCustomAttributesLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceCustomAttributeShellScript>(CacheKeyMacCustomAttributes, rows =>

            {

                MacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(rows);

                _macCustomAttributesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} mac custom attribute(s) from cache";

            }))

            {

                _ = LoadMacCustomAttributesAsync();

            }

        }



        if (value?.Name == "Feature Updates" && !_featureUpdateProfilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<WindowsFeatureUpdateProfile>(CacheKeyFeatureUpdateProfiles, rows =>

            {

                FeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(rows);

                _featureUpdateProfilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} feature update profile(s) from cache";

            }))

            {

                _ = LoadFeatureUpdateProfilesAsync();

            }

        }



        if (value?.Name == "Named Locations" && !_namedLocationsLoaded)

        {

            if (!TryLoadLazyCacheEntry<NamedLocation>(CacheKeyNamedLocations, rows =>

            {

                NamedLocations = new ObservableCollection<NamedLocation>(rows);

                _namedLocationsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} named location(s) from cache";

            }))

            {

                _ = LoadNamedLocationsAsync();

            }

        }



        if (value?.Name == "Authentication Strengths" && !_authenticationStrengthPoliciesLoaded)

        {

            if (!TryLoadLazyCacheEntry<AuthenticationStrengthPolicy>(CacheKeyAuthenticationStrengths, rows =>

            {

                AuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(rows);

                _authenticationStrengthPoliciesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} authentication strength policy(ies) from cache";

            }))

            {

                _ = LoadAuthenticationStrengthPoliciesAsync();

            }

        }



        if (value?.Name == "Authentication Contexts" && !_authenticationContextClassReferencesLoaded)

        {

            if (!TryLoadLazyCacheEntry<AuthenticationContextClassReference>(CacheKeyAuthenticationContexts, rows =>

            {

                AuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(rows);

                _authenticationContextClassReferencesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} authentication context(s) from cache";

            }))

            {

                _ = LoadAuthenticationContextsAsync();

            }

        }



        if (value?.Name == "Terms of Use" && !_termsOfUseAgreementsLoaded)

        {

            if (!TryLoadLazyCacheEntry<Agreement>(CacheKeyTermsOfUseAgreements, rows =>

            {

                TermsOfUseAgreements = new ObservableCollection<Agreement>(rows);

                _termsOfUseAgreementsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} terms of use agreement(s) from cache";

            }))

            {

                _ = LoadTermsOfUseAgreementsAsync();

            }

        }

    }



    /// <summary>

    /// Tries to load a lazy-loaded view (app assignments, groups) from cache.

    /// Invokes the onLoaded callback with the data if found. Returns true if cache hit.

    /// </summary>

    private bool TryLoadLazyCacheEntry<T>(string cacheKey, Action<List<T>> onLoaded)

    {

        var tenantId = ActiveProfile?.TenantId;

        if (string.IsNullOrEmpty(tenantId)) return false;



        try

        {

            var cached = _cacheService.Get<T>(tenantId, cacheKey);

            if (cached != null && cached.Count > 0)

            {

                DebugLog.Log("Cache", $"Loaded {cached.Count} {cacheKey} row(s) from cache");

                onLoaded(cached);

                return true;

            }

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load {cacheKey} from cache: {ex.Message}", ex);

        }



        return false;

    }

}

