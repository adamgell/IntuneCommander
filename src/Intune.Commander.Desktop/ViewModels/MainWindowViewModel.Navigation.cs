using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Linq;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{



    public ObservableCollection<NavCategory> NavCategories { get; } = [];

    public ObservableCollection<NavCategoryGroup> NavGroups { get; } = [];



    private static readonly NavCategory OverviewCategory = new() { Name = "Overview", Icon = "📊" };



    private static List<NavCategoryGroup> BuildDefaultNavGroups() =>

    [

        new NavCategoryGroup

        {

            Name = "Devices", Icon = "💻",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Device Configurations", Icon = "⚙" },

                new() { Name = "Compliance Policies", Icon = "✓" },

                new() { Name = "Settings Catalog", Icon = "⚙" },

                new() { Name = "Administrative Templates", Icon = "🧾" },

                new() { Name = "Endpoint Security", Icon = "🛡" },

                new() { Name = "Enrollment Configurations", Icon = "🪪" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Applications", Icon = "📦",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Applications", Icon = "📦" },

                new() { Name = "Application Assignments", Icon = "📋" },

                new() { Name = "App Protection Policies", Icon = "🔒" },

                new() { Name = "Managed Device App Configurations", Icon = "📱" },

                new() { Name = "Targeted Managed App Configurations", Icon = "🎯" },

                new() { Name = "VPP Tokens", Icon = "🎟" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Identity & Access", Icon = "🔐",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Conditional Access", Icon = "🔐" },

                new() { Name = "Named Locations", Icon = "📍" },

                new() { Name = "Authentication Strengths", Icon = "🔐" },

                new() { Name = "Authentication Contexts", Icon = "🏷" },

                new() { Name = "Terms of Use", Icon = "📄" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Tenant Admin", Icon = "🏢",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Scope Tags", Icon = "🏷" },

                new() { Name = "Role Definitions", Icon = "💼" },

                new() { Name = "Role Assignments", Icon = "🔑" },

                new() { Name = "Assignment Filters", Icon = "🧩" },

                new() { Name = "Policy Sets", Icon = "🗂" },

                new() { Name = "Intune Branding", Icon = "🎨" },

                new() { Name = "Azure Branding", Icon = "🟦" },

                new() { Name = "Terms and Conditions", Icon = "📜" },

                new() { Name = "Autopilot Profiles", Icon = "🚀" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Cloud PC", Icon = "🖥",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Cloud PC Provisioning Policies", Icon = "🖥" },

                new() { Name = "Cloud PC User Settings", Icon = "👤" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Monitoring", Icon = "🩺",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Device Health Scripts", Icon = "🩺" },

                new() { Name = "Mac Custom Attributes", Icon = "🍎" },

                new() { Name = "Feature Updates", Icon = "🪟" },

                new() { Name = "Device Management Scripts", Icon = "📜" },

                new() { Name = "Device Shell Scripts", Icon = "🐚" },

                new() { Name = "Compliance Scripts", Icon = "✅" },

                new() { Name = "ADMX Files", Icon = "📁" },

                new() { Name = "Reusable Policy Settings", Icon = "🔗" },

                new() { Name = "Notification Templates", Icon = "🔔" },

            }

        },

        new NavCategoryGroup

        {

            Name = "Groups", Icon = "👥",

            Children = new ObservableCollection<NavCategory>

            {

                new() { Name = "Dynamic Groups", Icon = "🔄" },

                new() { Name = "Assigned Groups", Icon = "👥" },

            }

        },

    ];



    /// <summary>

    /// Flat list of all category names (derived from groups) for backward compatibility.

    /// </summary>

    private static List<NavCategory> BuildDefaultNavCategories()

    {

        var groups = BuildDefaultNavGroups();

        var result = new List<NavCategory> { OverviewCategory };

        foreach (var g in groups)

            result.AddRange(g.Children);

        return result;

    }





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



        // Rebuild grouped nav structure

        var groups = BuildDefaultNavGroups();

        NavGroups.Clear();

        foreach (var group in groups)

            NavGroups.Add(group);



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

        "Quality Updates" => QualityUpdateProfileColumns,

        "Driver Updates" => DriverUpdateProfileColumns,

        "Named Locations" => NamedLocationColumns,

        "Authentication Strengths" => AuthenticationStrengthColumns,

        "Authentication Contexts" => AuthenticationContextColumns,

        "Terms of Use" => TermsOfUseColumns,

        "Device Management Scripts" => DeviceManagementScriptColumns,

        "Device Shell Scripts" => DeviceShellScriptColumns,

        "Compliance Scripts" => ComplianceScriptColumns,

        "ADMX Files" => AdmxFileColumns,

        "Reusable Policy Settings" => ReusablePolicySettingColumns,

        "Notification Templates" => NotificationTemplateColumns,

        "Dynamic Groups" => DynamicGroupColumns,

        "Assigned Groups" => AssignedGroupColumns,

        "Cloud PC Provisioning Policies" => CloudPcProvisioningColumns,

        "Cloud PC User Settings" => CloudPcUserSettingColumns,

        "VPP Tokens" => VppTokenColumns,

        "Role Assignments" => RoleAssignmentColumns,

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

    public bool IsQualityUpdatesCategory => SelectedCategory?.Name == "Quality Updates";

    public bool IsDriverUpdatesCategory => SelectedCategory?.Name == "Driver Updates";

    public bool IsNamedLocationsCategory => SelectedCategory?.Name == "Named Locations";

    public bool IsAuthenticationStrengthsCategory => SelectedCategory?.Name == "Authentication Strengths";

    public bool IsAuthenticationContextsCategory => SelectedCategory?.Name == "Authentication Contexts";

    public bool IsTermsOfUseCategory => SelectedCategory?.Name == "Terms of Use";

    public bool IsDeviceManagementScriptsCategory => SelectedCategory?.Name == "Device Management Scripts";

    public bool IsDeviceShellScriptsCategory => SelectedCategory?.Name == "Device Shell Scripts";

    public bool IsComplianceScriptsCategory => SelectedCategory?.Name == "Compliance Scripts";

    public bool IsAdmxFilesCategory => SelectedCategory?.Name == "ADMX Files";

    public bool IsReusablePolicySettingsCategory => SelectedCategory?.Name == "Reusable Policy Settings";

    public bool IsNotificationTemplatesCategory => SelectedCategory?.Name == "Notification Templates";

    public bool IsDynamicGroupsCategory => SelectedCategory?.Name == "Dynamic Groups";

    public bool IsAssignedGroupsCategory => SelectedCategory?.Name == "Assigned Groups";

    public bool IsCloudPcProvisioningCategory => SelectedCategory?.Name == "Cloud PC Provisioning Policies";

    public bool IsCloudPcUserSettingsCategory => SelectedCategory?.Name == "Cloud PC User Settings";

    public bool IsVppTokensCategory => SelectedCategory?.Name == "VPP Tokens";

    public bool IsRoleAssignmentsCategory => SelectedCategory?.Name == "Role Assignments";

    public bool IsCurrentCategoryEmpty =>
        !IsBusy &&
        SelectedCategory is not null &&
        !IsOverviewCategory &&
        GetCurrentFilteredCount() == 0;

    private int GetCurrentFilteredCount() => SelectedCategory?.Name switch
    {
        "Device Configurations" => FilteredDeviceConfigurations.Count,
        "Compliance Policies" => FilteredCompliancePolicies.Count,
        "Applications" => FilteredApplications.Count,
        "Application Assignments" => FilteredAppAssignmentRows.Count,
        "Dynamic Groups" => FilteredDynamicGroupRows.Count,
        "Assigned Groups" => FilteredAssignedGroupRows.Count,
        "Settings Catalog" => FilteredSettingsCatalogPolicies.Count,
        "Endpoint Security" => FilteredEndpointSecurityIntents.Count,
        "Administrative Templates" => FilteredAdministrativeTemplates.Count,
        "Enrollment Configurations" => FilteredEnrollmentConfigurations.Count,
        "App Protection Policies" => FilteredAppProtectionPolicies.Count,
        "Managed Device App Configurations" => FilteredManagedDeviceAppConfigurations.Count,
        "Targeted Managed App Configurations" => FilteredTargetedManagedAppConfigurations.Count,
        "Terms and Conditions" => FilteredTermsAndConditionsCollection.Count,
        "Scope Tags" => FilteredScopeTags.Count,
        "Role Definitions" => FilteredRoleDefinitions.Count,
        "Intune Branding" => FilteredIntuneBrandingProfiles.Count,
        "Azure Branding" => FilteredAzureBrandingLocalizations.Count,
        "Conditional Access" => FilteredConditionalAccessPolicies.Count,
        "Assignment Filters" => FilteredAssignmentFilters.Count,
        "Policy Sets" => FilteredPolicySets.Count,
        "Autopilot Profiles" => FilteredAutopilotProfiles.Count,
        "Device Health Scripts" => FilteredDeviceHealthScripts.Count,
        "Mac Custom Attributes" => FilteredMacCustomAttributes.Count,
        "Feature Updates" => FilteredFeatureUpdateProfiles.Count,
        "Quality Updates" => FilteredQualityUpdateProfiles.Count,
        "Driver Updates" => FilteredDriverUpdateProfiles.Count,
        "Named Locations" => FilteredNamedLocations.Count,
        "Authentication Strengths" => FilteredAuthenticationStrengthPolicies.Count,
        "Authentication Contexts" => FilteredAuthenticationContextClassReferences.Count,
        "Terms of Use" => FilteredTermsOfUseAgreements.Count,
        "Device Management Scripts" => FilteredDeviceManagementScripts.Count,
        "Device Shell Scripts" => FilteredDeviceShellScripts.Count,
        "Compliance Scripts" => FilteredComplianceScripts.Count,
        "Cloud PC Provisioning Policies" => FilteredCloudPcProvisioningPolicies.Count,
        "Cloud PC User Settings" => FilteredCloudPcUserSettings.Count,
        "VPP Tokens" => FilteredVppTokens.Count,
        "Role Assignments" => FilteredRoleAssignments.Count,
        "ADMX Files" => FilteredAdmxFiles.Count,
        "Reusable Policy Settings" => FilteredReusablePolicySettings.Count,
        "Notification Templates" => FilteredNotificationTemplates.Count,
        _ => -1
    };

    /// <summary>Navigates to the category with the given name. Used by OverviewViewModel card commands.</summary>
    public void ActivateCategoryByName(string name)
    {
        var cat = NavCategories.FirstOrDefault(c => c.Name == name);
        if (cat != null)
            SelectedCategory = cat;
    }



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

        SelectedQualityUpdateProfile = null;

        SelectedDriverUpdateProfile = null;

        SelectedNamedLocation = null;

        SelectedAuthenticationStrengthPolicy = null;

        SelectedAuthenticationContextClassReference = null;

        SelectedTermsOfUseAgreement = null;

        SelectedDeviceManagementScript = null;

        SelectedDeviceShellScript = null;

        SelectedComplianceScript = null;

        SelectedAdmxFile = null;

        SelectedReusablePolicySetting = null;

        SelectedNotificationTemplate = null;

        SelectedDynamicGroupRow = null;

        SelectedAssignedGroupRow = null;

        SelectedCloudPcProvisioningPolicy = null;

        SelectedCloudPcUserSetting = null;

        SelectedVppToken = null;

        SelectedRoleAssignment = null;

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

        OnPropertyChanged(nameof(IsQualityUpdatesCategory));

        OnPropertyChanged(nameof(IsDriverUpdatesCategory));

        OnPropertyChanged(nameof(IsNamedLocationsCategory));

        OnPropertyChanged(nameof(IsAuthenticationStrengthsCategory));

        OnPropertyChanged(nameof(IsAuthenticationContextsCategory));

        OnPropertyChanged(nameof(IsTermsOfUseCategory));

        OnPropertyChanged(nameof(IsDeviceManagementScriptsCategory));

        OnPropertyChanged(nameof(IsDeviceShellScriptsCategory));

        OnPropertyChanged(nameof(IsComplianceScriptsCategory));

        OnPropertyChanged(nameof(IsAdmxFilesCategory));

        OnPropertyChanged(nameof(IsReusablePolicySettingsCategory));

        OnPropertyChanged(nameof(IsNotificationTemplatesCategory));

        OnPropertyChanged(nameof(IsDynamicGroupsCategory));

        OnPropertyChanged(nameof(IsAssignedGroupsCategory));

        OnPropertyChanged(nameof(IsCloudPcProvisioningCategory));

        OnPropertyChanged(nameof(IsCloudPcUserSettingsCategory));

        OnPropertyChanged(nameof(IsVppTokensCategory));

        OnPropertyChanged(nameof(IsRoleAssignmentsCategory));

        OnPropertyChanged(nameof(ActiveColumns));

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        // Update IsSelected on all nav categories
        foreach (var cat in NavCategories)
            cat.IsSelected = cat == value;

        OnPropertyChanged(nameof(IsCurrentCategoryEmpty));



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

                _appAssignmentsLoaded = true;

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

                _dynamicGroupsLoaded = true;

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

                _assignedGroupsLoaded = true;

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

                _conditionalAccessLoaded = true;

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

                _assignmentFiltersLoaded = true;

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

                _policySetsLoaded = true;

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

                _endpointSecurityLoaded = true;

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

                _administrativeTemplatesLoaded = true;

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

                _enrollmentConfigurationsLoaded = true;

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

                _appProtectionPoliciesLoaded = true;

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

                _managedDeviceAppConfigurationsLoaded = true;

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

                _targetedManagedAppConfigurationsLoaded = true;

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

                _termsAndConditionsLoaded = true;

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

                _scopeTagsLoaded = true;

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

                _roleDefinitionsLoaded = true;

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

                _intuneBrandingProfilesLoaded = true;

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

                _azureBrandingLocalizationsLoaded = true;

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

                _autopilotProfilesLoaded = true;

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

                _deviceHealthScriptsLoaded = true;

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

                _macCustomAttributesLoaded = true;

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

                _featureUpdateProfilesLoaded = true;

                _ = LoadFeatureUpdateProfilesAsync();

            }

        }



        if (value?.Name == "Quality Updates" && !_qualityUpdateProfilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<WindowsQualityUpdateProfile>(CacheKeyQualityUpdateProfiles, rows =>

            {

                QualityUpdateProfiles = new ObservableCollection<WindowsQualityUpdateProfile>(rows);

                _qualityUpdateProfilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} quality update profile(s) from cache";

            }))

            {

                _qualityUpdateProfilesLoaded = true;

                _ = LoadQualityUpdateProfilesAsync();

            }

        }



        if (value?.Name == "Driver Updates" && !_driverUpdateProfilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<WindowsDriverUpdateProfile>(CacheKeyDriverUpdateProfiles, rows =>

            {

                DriverUpdateProfiles = new ObservableCollection<WindowsDriverUpdateProfile>(rows);

                _driverUpdateProfilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} driver update profile(s) from cache";

            }))

            {

                _driverUpdateProfilesLoaded = true;

                _ = LoadDriverUpdateProfilesAsync();

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

                _namedLocationsLoaded = true;

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

                _authenticationStrengthPoliciesLoaded = true;

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

                _authenticationContextClassReferencesLoaded = true;

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

                _termsOfUseAgreementsLoaded = true;

                _ = LoadTermsOfUseAgreementsAsync();

            }

        }

        if (value?.Name == "Device Management Scripts" && !_deviceManagementScriptsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceManagementScript>(CacheKeyDeviceManagementScripts, rows =>

            {

                DeviceManagementScripts = new ObservableCollection<DeviceManagementScript>(rows);

                _deviceManagementScriptsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} device management script(s) from cache";

            }))

            {

                _deviceManagementScriptsLoaded = true;

                _ = LoadDeviceManagementScriptsAsync();

            }

        }

        if (value?.Name == "Device Shell Scripts" && !_deviceShellScriptsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceShellScript>(CacheKeyDeviceShellScripts, rows =>

            {

                DeviceShellScripts = new ObservableCollection<DeviceShellScript>(rows);

                _deviceShellScriptsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} device shell script(s) from cache";

            }))

            {

                _deviceShellScriptsLoaded = true;

                _ = LoadDeviceShellScriptsAsync();

            }

        }

        if (value?.Name == "Compliance Scripts" && !_complianceScriptsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceComplianceScript>(CacheKeyComplianceScripts, rows =>

            {

                ComplianceScripts = new ObservableCollection<DeviceComplianceScript>(rows);

                _complianceScriptsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} compliance script(s) from cache";

            }))

            {

                _complianceScriptsLoaded = true;

                _ = LoadComplianceScriptsAsync();

            }

        }

        if (value?.Name == "Cloud PC Provisioning Policies" && !_cloudPcProvisioningPoliciesLoaded)

        {

            if (!TryLoadLazyCacheEntry<CloudPcProvisioningPolicy>(CacheKeyCloudPcProvisioningPolicies, rows =>

            {

                CloudPcProvisioningPolicies = new ObservableCollection<CloudPcProvisioningPolicy>(rows);

                _cloudPcProvisioningPoliciesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} Cloud PC provisioning policy(ies) from cache";

            }))

            {

                _cloudPcProvisioningPoliciesLoaded = true;

                _ = LoadCloudPcProvisioningPoliciesAsync();

            }

        }

        if (value?.Name == "ADMX Files" && !_admxFilesLoaded)

        {

            if (!TryLoadLazyCacheEntry<GroupPolicyUploadedDefinitionFile>(CacheKeyAdmxFiles, rows =>

            {

                AdmxFiles = new ObservableCollection<GroupPolicyUploadedDefinitionFile>(rows);

                _admxFilesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} ADMX file(s) from cache";

            }))

            {

                _admxFilesLoaded = true;

                _ = LoadAdmxFilesAsync();

            }

        }

        if (value?.Name == "Cloud PC User Settings" && !_cloudPcUserSettingsLoaded)

        {

            if (!TryLoadLazyCacheEntry<CloudPcUserSetting>(CacheKeyCloudPcUserSettings, rows =>

            {

                CloudPcUserSettings = new ObservableCollection<CloudPcUserSetting>(rows);

                _cloudPcUserSettingsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} Cloud PC user setting(s) from cache";

            }))

            {

                _cloudPcUserSettingsLoaded = true;

                _ = LoadCloudPcUserSettingsAsync();

            }

        }

        if (value?.Name == "Reusable Policy Settings" && !_reusablePolicySettingsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceManagementReusablePolicySetting>(CacheKeyReusablePolicySettings, rows =>

            {

                ReusablePolicySettings = new ObservableCollection<DeviceManagementReusablePolicySetting>(rows);

                _reusablePolicySettingsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} reusable policy setting(s) from cache";

            }))

            {

                _reusablePolicySettingsLoaded = true;

                _ = LoadReusablePolicySettingsAsync();

            }

        }

        if (value?.Name == "VPP Tokens" && !_vppTokensLoaded)

        {

            if (!TryLoadLazyCacheEntry<VppToken>(CacheKeyVppTokens, rows =>

            {

                VppTokens = new ObservableCollection<VppToken>(rows);

                _vppTokensLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} VPP token(s) from cache";

            }))

            {

                _vppTokensLoaded = true;

                _ = LoadVppTokensAsync();

            }

        }

        if (value?.Name == "Notification Templates" && !_notificationTemplatesLoaded)

        {

            if (!TryLoadLazyCacheEntry<NotificationMessageTemplate>(CacheKeyNotificationTemplates, rows =>

            {

                NotificationTemplates = new ObservableCollection<NotificationMessageTemplate>(rows);

                _notificationTemplatesLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} notification template(s) from cache";

            }))

            {

                _notificationTemplatesLoaded = true;

                _ = LoadNotificationTemplatesAsync();

            }

        }

        if (value?.Name == "Role Assignments" && !_roleAssignmentsLoaded)

        {

            if (!TryLoadLazyCacheEntry<DeviceAndAppManagementRoleAssignment>(CacheKeyRoleAssignments, rows =>

            {

                RoleAssignments = new ObservableCollection<DeviceAndAppManagementRoleAssignment>(rows);

                _roleAssignmentsLoaded = true;

                ApplyFilter();

                StatusText = $"Loaded {rows.Count} role assignment(s) from cache";

            }))

            {

                _roleAssignmentsLoaded = true;

                _ = LoadRoleAssignmentsAsync();

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

