using System;

using System.Collections.ObjectModel;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using Intune.Commander.Desktop.Extensions;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{

    // --- Search / filter ---

    [ObservableProperty]

    private string _searchText = "";



    private CancellationTokenSource? _searchDebounceCancel;

    private const int SearchDebounceMs = 300;



    partial void OnSearchTextChanged(string value)

    {

        _searchDebounceCancel?.Cancel();

        _searchDebounceCancel = new CancellationTokenSource();

        var token = _searchDebounceCancel.Token;



        Task.Delay(SearchDebounceMs, token).ContinueWith(_ =>

        {

            if (!token.IsCancellationRequested)

            {

                Dispatcher.UIThread.Post(ApplyFilter);

            }

        }, token);

    }



    /// <summary>

    /// Filtered views exposed for DataGrid binding.

    /// These are rebuilt whenever the source collection or SearchText changes.

    /// </summary>

    [ObservableProperty]

    private ObservableCollection<DeviceConfiguration> _filteredDeviceConfigurations = [];



    [ObservableProperty]

    private ObservableCollection<DeviceCompliancePolicy> _filteredCompliancePolicies = [];



    [ObservableProperty]

    private ObservableCollection<MobileApp> _filteredApplications = [];



    [ObservableProperty]

    private ObservableCollection<AppAssignmentRow> _filteredAppAssignmentRows = [];



    [ObservableProperty]

    private ObservableCollection<GroupRow> _filteredDynamicGroupRows = [];



    [ObservableProperty]

    private ObservableCollection<GroupRow> _filteredAssignedGroupRows = [];



    [ObservableProperty]

    private ObservableCollection<DeviceManagementConfigurationPolicy> _filteredSettingsCatalogPolicies = [];



    [ObservableProperty]

    private ObservableCollection<DeviceManagementIntent> _filteredEndpointSecurityIntents = [];



    [ObservableProperty]

    private ObservableCollection<GroupPolicyConfiguration> _filteredAdministrativeTemplates = [];



    [ObservableProperty]

    private ObservableCollection<DeviceEnrollmentConfiguration> _filteredEnrollmentConfigurations = [];



    [ObservableProperty]

    private ObservableCollection<ManagedAppPolicy> _filteredAppProtectionPolicies = [];



    [ObservableProperty]

    private ObservableCollection<ManagedDeviceMobileAppConfiguration> _filteredManagedDeviceAppConfigurations = [];



    [ObservableProperty]

    private ObservableCollection<TargetedManagedAppConfiguration> _filteredTargetedManagedAppConfigurations = [];



    [ObservableProperty]

    private ObservableCollection<TermsAndConditions> _filteredTermsAndConditionsCollection = [];



    [ObservableProperty]

    private ObservableCollection<RoleScopeTag> _filteredScopeTags = [];



    [ObservableProperty]

    private ObservableCollection<RoleDefinition> _filteredRoleDefinitions = [];



    [ObservableProperty]

    private ObservableCollection<IntuneBrandingProfile> _filteredIntuneBrandingProfiles = [];



    [ObservableProperty]

    private ObservableCollection<OrganizationalBrandingLocalization> _filteredAzureBrandingLocalizations = [];



    [ObservableProperty]

    private ObservableCollection<ConditionalAccessPolicy> _filteredConditionalAccessPolicies = [];



    [ObservableProperty]

    private ObservableCollection<DeviceAndAppManagementAssignmentFilter> _filteredAssignmentFilters = [];



    [ObservableProperty]

    private ObservableCollection<PolicySet> _filteredPolicySets = [];



    [ObservableProperty]

    private ObservableCollection<WindowsAutopilotDeploymentProfile> _filteredAutopilotProfiles = [];



    [ObservableProperty]

    private ObservableCollection<DeviceHealthScript> _filteredDeviceHealthScripts = [];



    [ObservableProperty]

    private ObservableCollection<DeviceCustomAttributeShellScript> _filteredMacCustomAttributes = [];



    [ObservableProperty]

    private ObservableCollection<WindowsFeatureUpdateProfile> _filteredFeatureUpdateProfiles = [];



    [ObservableProperty]

    private ObservableCollection<NamedLocation> _filteredNamedLocations = [];



    [ObservableProperty]

    private ObservableCollection<AuthenticationStrengthPolicy> _filteredAuthenticationStrengthPolicies = [];



    [ObservableProperty]

    private ObservableCollection<AuthenticationContextClassReference> _filteredAuthenticationContextClassReferences = [];



    [ObservableProperty]

    private ObservableCollection<Agreement> _filteredTermsOfUseAgreements = [];



    [ObservableProperty]

    private ObservableCollection<DeviceManagementScript> _filteredDeviceManagementScripts = [];



    [ObservableProperty]

    private ObservableCollection<DeviceShellScript> _filteredDeviceShellScripts = [];



    [ObservableProperty]

    private ObservableCollection<DeviceComplianceScript> _filteredComplianceScripts = [];



    [ObservableProperty]
    private ObservableCollection<DepOnboardingSetting> _filteredAppleDepSettings = [];

    [ObservableProperty]
    private ObservableCollection<DeviceCategory> _filteredDeviceCategories = [];

    [ObservableProperty]
    private ObservableCollection<CloudPcProvisioningPolicy> _filteredCloudPcProvisioningPolicies = [];

    [ObservableProperty]
    private ObservableCollection<CloudPcUserSetting> _filteredCloudPcUserSettings = [];

    [ObservableProperty]
    private ObservableCollection<VppToken> _filteredVppTokens = [];

    [ObservableProperty]
    private ObservableCollection<DeviceAndAppManagementRoleAssignment> _filteredRoleAssignments = [];

    [ObservableProperty]

    private ObservableCollection<WindowsQualityUpdateProfile> _filteredQualityUpdateProfiles = [];



    [ObservableProperty]

    private ObservableCollection<GroupPolicyUploadedDefinitionFile> _filteredAdmxFiles = [];



    [ObservableProperty]

    private ObservableCollection<WindowsDriverUpdateProfile> _filteredDriverUpdateProfiles = [];



    [ObservableProperty]

    private ObservableCollection<DeviceManagementReusablePolicySetting> _filteredReusablePolicySettings = [];



    [ObservableProperty]

    private ObservableCollection<NotificationMessageTemplate> _filteredNotificationTemplates = [];



    private static void UpdateFilteredCollection<T>(
        ObservableCollection<T> target,
        ObservableCollection<T> source,
        Func<T, bool>? predicate = null)
    {
        if (predicate == null)
        {
            target.ReplaceAll(source);
            return;
        }

        target.ReplaceAll(source.Where(predicate));
    }

    private void ApplyFilter()

    {

        var q = SearchText.Trim();



        if (string.IsNullOrEmpty(q))

        {

            UpdateFilteredCollection(FilteredDeviceConfigurations, DeviceConfigurations);
            UpdateFilteredCollection(FilteredCompliancePolicies, CompliancePolicies);
            UpdateFilteredCollection(FilteredApplications, Applications);
            UpdateFilteredCollection(FilteredAppAssignmentRows, AppAssignmentRows);
            UpdateFilteredCollection(FilteredDynamicGroupRows, DynamicGroupRows);
            UpdateFilteredCollection(FilteredAssignedGroupRows, AssignedGroupRows);
            UpdateFilteredCollection(FilteredSettingsCatalogPolicies, SettingsCatalogPolicies);
            UpdateFilteredCollection(FilteredEndpointSecurityIntents, EndpointSecurityIntents);
            UpdateFilteredCollection(FilteredAdministrativeTemplates, AdministrativeTemplates);
            UpdateFilteredCollection(FilteredEnrollmentConfigurations, EnrollmentConfigurations);
            UpdateFilteredCollection(FilteredAppProtectionPolicies, AppProtectionPolicies);
            UpdateFilteredCollection(FilteredManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations);
            UpdateFilteredCollection(FilteredTargetedManagedAppConfigurations, TargetedManagedAppConfigurations);
            UpdateFilteredCollection(FilteredTermsAndConditionsCollection, TermsAndConditionsCollection);
            UpdateFilteredCollection(FilteredScopeTags, ScopeTags);
            UpdateFilteredCollection(FilteredRoleDefinitions, RoleDefinitions);
            UpdateFilteredCollection(FilteredIntuneBrandingProfiles, IntuneBrandingProfiles);
            UpdateFilteredCollection(FilteredAzureBrandingLocalizations, AzureBrandingLocalizations);
            UpdateFilteredCollection(FilteredConditionalAccessPolicies, ConditionalAccessPolicies);
            UpdateFilteredCollection(FilteredAssignmentFilters, AssignmentFilters);
            UpdateFilteredCollection(FilteredPolicySets, PolicySets);
            UpdateFilteredCollection(FilteredAutopilotProfiles, AutopilotProfiles);
            UpdateFilteredCollection(FilteredDeviceHealthScripts, DeviceHealthScripts);
            UpdateFilteredCollection(FilteredMacCustomAttributes, MacCustomAttributes);
            UpdateFilteredCollection(FilteredFeatureUpdateProfiles, FeatureUpdateProfiles);
            UpdateFilteredCollection(FilteredNamedLocations, NamedLocations);
            UpdateFilteredCollection(FilteredAuthenticationStrengthPolicies, AuthenticationStrengthPolicies);
            UpdateFilteredCollection(FilteredAuthenticationContextClassReferences, AuthenticationContextClassReferences);
            UpdateFilteredCollection(FilteredTermsOfUseAgreements, TermsOfUseAgreements);
            UpdateFilteredCollection(FilteredDeviceManagementScripts, DeviceManagementScripts);
            UpdateFilteredCollection(FilteredDeviceShellScripts, DeviceShellScripts);
            UpdateFilteredCollection(FilteredComplianceScripts, ComplianceScripts);
            UpdateFilteredCollection(FilteredAppleDepSettings, AppleDepSettings);
            UpdateFilteredCollection(FilteredDeviceCategories, DeviceCategories);
            UpdateFilteredCollection(FilteredCloudPcProvisioningPolicies, CloudPcProvisioningPolicies);
            UpdateFilteredCollection(FilteredCloudPcUserSettings, CloudPcUserSettings);
            UpdateFilteredCollection(FilteredVppTokens, VppTokens);
            UpdateFilteredCollection(FilteredRoleAssignments, RoleAssignments);
            UpdateFilteredCollection(FilteredQualityUpdateProfiles, QualityUpdateProfiles);
            UpdateFilteredCollection(FilteredDriverUpdateProfiles, DriverUpdateProfiles);
            UpdateFilteredCollection(FilteredAdmxFiles, AdmxFiles);
            UpdateFilteredCollection(FilteredReusablePolicySettings, ReusablePolicySettings);
            UpdateFilteredCollection(FilteredNotificationTemplates, NotificationTemplates);

            OnPropertyChanged(nameof(IsCurrentCategoryEmpty));
            return;

        }

        UpdateFilteredCollection(FilteredDeviceConfigurations, DeviceConfigurations,
            c => Contains(c.DisplayName, q) || Contains(c.Description, q) || Contains(c.OdataType, q));
        UpdateFilteredCollection(FilteredCompliancePolicies, CompliancePolicies,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.OdataType, q));
        UpdateFilteredCollection(FilteredApplications, Applications,
            a => Contains(a.DisplayName, q) || Contains(a.Publisher, q) || Contains(a.Description, q) || Contains(a.OdataType, q));
        UpdateFilteredCollection(FilteredAppAssignmentRows, AppAssignmentRows,
            r => Contains(r.AppName, q) || Contains(r.Publisher, q) || Contains(r.TargetName, q) || Contains(r.AppType, q) || Contains(r.Platform, q) || Contains(r.InstallIntent, q));
        UpdateFilteredCollection(FilteredDynamicGroupRows, DynamicGroupRows,
            g => Contains(g.GroupName, q) || Contains(g.Description, q) || Contains(g.MembershipRule, q) || Contains(g.GroupType, q) || Contains(g.GroupId, q));
        UpdateFilteredCollection(FilteredAssignedGroupRows, AssignedGroupRows,
            g => Contains(g.GroupName, q) || Contains(g.Description, q) || Contains(g.GroupType, q) || Contains(g.GroupId, q));
        UpdateFilteredCollection(FilteredSettingsCatalogPolicies, SettingsCatalogPolicies,
            p => Contains(p.Name, q) || Contains(p.Description, q) || Contains(p.Platforms?.ToString(), q) || Contains(p.Technologies?.ToString(), q));
        UpdateFilteredCollection(FilteredEndpointSecurityIntents, EndpointSecurityIntents,
            i => Contains(i.DisplayName, q) || Contains(i.Description, q) || Contains(i.Id, q));
        UpdateFilteredCollection(FilteredAdministrativeTemplates, AdministrativeTemplates,
            t => Contains(t.DisplayName, q) || Contains(t.Description, q) || Contains(t.Id, q));
        UpdateFilteredCollection(FilteredEnrollmentConfigurations, EnrollmentConfigurations,
            c => Contains(c.DisplayName, q) || Contains(c.Description, q) || Contains(c.Id, q) || Contains(c.OdataType, q));
        UpdateFilteredCollection(FilteredAppProtectionPolicies, AppProtectionPolicies,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.Id, q) || Contains(p.OdataType, q));
        UpdateFilteredCollection(FilteredManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations,
            c => Contains(c.DisplayName, q) || Contains(c.Description, q) || Contains(c.Id, q) || Contains(c.OdataType, q));
        UpdateFilteredCollection(FilteredTargetedManagedAppConfigurations, TargetedManagedAppConfigurations,
            c => Contains(c.DisplayName, q) || Contains(c.Description, q) || Contains(c.Id, q) || Contains(c.OdataType, q));
        UpdateFilteredCollection(FilteredTermsAndConditionsCollection, TermsAndConditionsCollection,
            t => Contains(t.DisplayName, q) || Contains(t.Description, q) || Contains(t.Id, q));
        UpdateFilteredCollection(FilteredScopeTags, ScopeTags,
            t => Contains(t.DisplayName, q) || Contains(t.Description, q) || Contains(t.Id, q));
        UpdateFilteredCollection(FilteredRoleDefinitions, RoleDefinitions,
            r => Contains(r.DisplayName, q) || Contains(r.Description, q) || Contains(r.Id, q));
        UpdateFilteredCollection(FilteredIntuneBrandingProfiles, IntuneBrandingProfiles,
            b => Contains(b.ProfileName, q) || Contains(b.Id, q));
        UpdateFilteredCollection(FilteredAzureBrandingLocalizations, AzureBrandingLocalizations,
            b => Contains(b.Id, q) || Contains(b.SignInPageText, q));
        UpdateFilteredCollection(FilteredConditionalAccessPolicies, ConditionalAccessPolicies,
            p => Contains(p.DisplayName, q) || Contains(p.State?.ToString(), q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredAssignmentFilters, AssignmentFilters,
            f => Contains(f.DisplayName, q) || Contains(f.Platform?.ToString(), q) || Contains(f.AssignmentFilterManagementType?.ToString(), q) || Contains(f.Id, q));
        UpdateFilteredCollection(FilteredPolicySets, PolicySets,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredAutopilotProfiles, AutopilotProfiles,
            p => Contains(TryReadStringProperty(p, "DisplayName"), q) || Contains(TryReadStringProperty(p, "Description"), q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredDeviceHealthScripts, DeviceHealthScripts,
            s => Contains(TryReadStringProperty(s, "DisplayName"), q) || Contains(TryReadStringProperty(s, "Description"), q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredMacCustomAttributes, MacCustomAttributes,
            a => Contains(TryReadStringProperty(a, "DisplayName"), q) || Contains(TryReadStringProperty(a, "Description"), q) || Contains(a.Id, q));
        UpdateFilteredCollection(FilteredFeatureUpdateProfiles, FeatureUpdateProfiles,
            p => Contains(TryReadStringProperty(p, "DisplayName"), q) || Contains(TryReadStringProperty(p, "Description"), q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredNamedLocations, NamedLocations,
            n => Contains(TryReadStringProperty(n, "DisplayName"), q) || Contains(TryReadStringProperty(n, "Description"), q) || Contains(n.Id, q));
        UpdateFilteredCollection(FilteredAuthenticationStrengthPolicies, AuthenticationStrengthPolicies,
            p => Contains(TryReadStringProperty(p, "DisplayName"), q) || Contains(TryReadStringProperty(p, "Description"), q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredAuthenticationContextClassReferences, AuthenticationContextClassReferences,
            c => Contains(TryReadStringProperty(c, "DisplayName"), q) || Contains(TryReadStringProperty(c, "Description"), q) || Contains(c.Id, q));
        UpdateFilteredCollection(FilteredTermsOfUseAgreements, TermsOfUseAgreements,
            a => Contains(TryReadStringProperty(a, "DisplayName"), q) || Contains(TryReadStringProperty(a, "Description"), q) || Contains(a.Id, q));
        UpdateFilteredCollection(FilteredDeviceManagementScripts, DeviceManagementScripts,
            s => Contains(TryReadStringProperty(s, "DisplayName"), q) || Contains(TryReadStringProperty(s, "Description"), q) || Contains(TryReadStringProperty(s, "FileName"), q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredDeviceShellScripts, DeviceShellScripts,
            s => Contains(TryReadStringProperty(s, "DisplayName"), q) || Contains(TryReadStringProperty(s, "Description"), q) || Contains(TryReadStringProperty(s, "FileName"), q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredComplianceScripts, ComplianceScripts,
            s => Contains(TryReadStringProperty(s, "DisplayName"), q) || Contains(TryReadStringProperty(s, "Description"), q) || Contains(TryReadStringProperty(s, "Publisher"), q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredAppleDepSettings, AppleDepSettings,
            d => Contains(d.TokenName, q) || Contains(d.AppleIdentifier, q) || Contains(d.Id, q));
        UpdateFilteredCollection(FilteredDeviceCategories, DeviceCategories,
            c => Contains(c.DisplayName, q) || Contains(c.Description, q) || Contains(c.Id, q));
        UpdateFilteredCollection(FilteredCloudPcProvisioningPolicies, CloudPcProvisioningPolicies,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredCloudPcUserSettings, CloudPcUserSettings,
            s => Contains(s.DisplayName, q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredVppTokens, VppTokens,
            t => Contains(t.DisplayName, q) || Contains(t.AppleId, q) || Contains(t.OrganizationName, q) || Contains(t.Id, q));
        UpdateFilteredCollection(FilteredRoleAssignments, RoleAssignments,
            r => Contains(r.DisplayName, q) || Contains(r.Description, q) || Contains(r.Id, q));
        UpdateFilteredCollection(FilteredQualityUpdateProfiles, QualityUpdateProfiles,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredDriverUpdateProfiles, DriverUpdateProfiles,
            p => Contains(p.DisplayName, q) || Contains(p.Description, q) || Contains(p.Id, q));
        UpdateFilteredCollection(FilteredAdmxFiles, AdmxFiles,
            f => Contains(f.DisplayName, q) || Contains(f.FileName, q) || Contains(f.Description, q) || Contains(f.Id, q));
        UpdateFilteredCollection(FilteredReusablePolicySettings, ReusablePolicySettings,
            s => Contains(s.DisplayName, q) || Contains(s.Description, q) || Contains(s.SettingDefinitionId, q) || Contains(s.Id, q));
        UpdateFilteredCollection(FilteredNotificationTemplates, NotificationTemplates,
            t => Contains(t.DisplayName, q) || Contains(t.Description, q) || Contains(t.DefaultLocale, q) || Contains(t.Id, q));

        OnPropertyChanged(nameof(IsCurrentCategoryEmpty));

    }



    private static bool Contains(string? source, string search)

        => source?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;



}

