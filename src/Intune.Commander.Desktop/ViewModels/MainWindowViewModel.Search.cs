using System;

using System.Collections.ObjectModel;

using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{

    // --- Search / filter ---

    [ObservableProperty]

    private string _searchText = "";



    partial void OnSearchTextChanged(string value)

    {

        ApplyFilter();

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



    private void ApplyFilter()

    {

        var q = SearchText.Trim();



        if (string.IsNullOrEmpty(q))

        {

            FilteredDeviceConfigurations = new ObservableCollection<DeviceConfiguration>(DeviceConfigurations);

            FilteredCompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(CompliancePolicies);

            FilteredApplications = new ObservableCollection<MobileApp>(Applications);

            FilteredAppAssignmentRows = new ObservableCollection<AppAssignmentRow>(AppAssignmentRows);

            FilteredDynamicGroupRows = new ObservableCollection<GroupRow>(DynamicGroupRows);

            FilteredAssignedGroupRows = new ObservableCollection<GroupRow>(AssignedGroupRows);

            FilteredSettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(SettingsCatalogPolicies);

            FilteredEndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(EndpointSecurityIntents);

            FilteredAdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(AdministrativeTemplates);

            FilteredEnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(EnrollmentConfigurations);

            FilteredAppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(AppProtectionPolicies);

            FilteredManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(ManagedDeviceAppConfigurations);

            FilteredTargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(TargetedManagedAppConfigurations);

            FilteredTermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(TermsAndConditionsCollection);

            FilteredScopeTags = new ObservableCollection<RoleScopeTag>(ScopeTags);

            FilteredRoleDefinitions = new ObservableCollection<RoleDefinition>(RoleDefinitions);

            FilteredIntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(IntuneBrandingProfiles);

            FilteredAzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(AzureBrandingLocalizations);

            FilteredConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(ConditionalAccessPolicies);

            FilteredAssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(AssignmentFilters);

            FilteredPolicySets = new ObservableCollection<PolicySet>(PolicySets);

            FilteredAutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(AutopilotProfiles);

            FilteredDeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(DeviceHealthScripts);

            FilteredMacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(MacCustomAttributes);

            FilteredFeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(FeatureUpdateProfiles);

            FilteredNamedLocations = new ObservableCollection<NamedLocation>(NamedLocations);

            FilteredAuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(AuthenticationStrengthPolicies);

            FilteredAuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(AuthenticationContextClassReferences);

            FilteredTermsOfUseAgreements = new ObservableCollection<Agreement>(TermsOfUseAgreements);

            FilteredDeviceManagementScripts = new ObservableCollection<DeviceManagementScript>(DeviceManagementScripts);

            FilteredDeviceShellScripts = new ObservableCollection<DeviceShellScript>(DeviceShellScripts);

            FilteredComplianceScripts = new ObservableCollection<DeviceComplianceScript>(ComplianceScripts);

            return;

        }



        FilteredDeviceConfigurations = new ObservableCollection<DeviceConfiguration>(

            DeviceConfigurations.Where(c =>

                Contains(c.DisplayName, q) ||

                Contains(c.Description, q) ||

                Contains(c.OdataType, q)));



        FilteredCompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(

            CompliancePolicies.Where(p =>

                Contains(p.DisplayName, q) ||

                Contains(p.Description, q) ||

                Contains(p.OdataType, q)));



        FilteredApplications = new ObservableCollection<MobileApp>(

            Applications.Where(a =>

                Contains(a.DisplayName, q) ||

                Contains(a.Publisher, q) ||

                Contains(a.Description, q) ||

                Contains(a.OdataType, q)));



        FilteredAppAssignmentRows = new ObservableCollection<AppAssignmentRow>(

            AppAssignmentRows.Where(r =>

                Contains(r.AppName, q) ||

                Contains(r.Publisher, q) ||

                Contains(r.TargetName, q) ||

                Contains(r.AppType, q) ||

                Contains(r.Platform, q) ||

                Contains(r.InstallIntent, q)));



        FilteredDynamicGroupRows = new ObservableCollection<GroupRow>(

            DynamicGroupRows.Where(g =>

                Contains(g.GroupName, q) ||

                Contains(g.Description, q) ||

                Contains(g.MembershipRule, q) ||

                Contains(g.GroupType, q) ||

                Contains(g.GroupId, q)));



        FilteredAssignedGroupRows = new ObservableCollection<GroupRow>(

            AssignedGroupRows.Where(g =>

                Contains(g.GroupName, q) ||

                Contains(g.Description, q) ||

                Contains(g.GroupType, q) ||

                Contains(g.GroupId, q)));



        FilteredSettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(

            SettingsCatalogPolicies.Where(p =>

                Contains(p.Name, q) ||

                Contains(p.Description, q) ||

                Contains(p.Platforms?.ToString(), q) ||

                Contains(p.Technologies?.ToString(), q)));



        FilteredEndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(

            EndpointSecurityIntents.Where(i =>

                Contains(i.DisplayName, q) ||

                Contains(i.Description, q) ||

                Contains(i.Id, q)));



        FilteredAdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(

            AdministrativeTemplates.Where(t =>

                Contains(t.DisplayName, q) ||

                Contains(t.Description, q) ||

                Contains(t.Id, q)));



        FilteredEnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(

            EnrollmentConfigurations.Where(c =>

                Contains(c.DisplayName, q) ||

                Contains(c.Description, q) ||

                Contains(c.Id, q) ||

                Contains(c.OdataType, q)));



        FilteredAppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(

            AppProtectionPolicies.Where(p =>

                Contains(p.DisplayName, q) ||

                Contains(p.Description, q) ||

                Contains(p.Id, q) ||

                Contains(p.OdataType, q)));



        FilteredManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(

            ManagedDeviceAppConfigurations.Where(c =>

                Contains(c.DisplayName, q) ||

                Contains(c.Description, q) ||

                Contains(c.Id, q) ||

                Contains(c.OdataType, q)));



        FilteredTargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(

            TargetedManagedAppConfigurations.Where(c =>

                Contains(c.DisplayName, q) ||

                Contains(c.Description, q) ||

                Contains(c.Id, q) ||

                Contains(c.OdataType, q)));



        FilteredTermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(

            TermsAndConditionsCollection.Where(t =>

                Contains(t.DisplayName, q) ||

                Contains(t.Description, q) ||

                Contains(t.Id, q)));



        FilteredScopeTags = new ObservableCollection<RoleScopeTag>(

            ScopeTags.Where(t =>

                Contains(t.DisplayName, q) ||

                Contains(t.Description, q) ||

                Contains(t.Id, q)));



        FilteredRoleDefinitions = new ObservableCollection<RoleDefinition>(

            RoleDefinitions.Where(r =>

                Contains(r.DisplayName, q) ||

                Contains(r.Description, q) ||

                Contains(r.Id, q)));



        FilteredIntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(

            IntuneBrandingProfiles.Where(b =>

                Contains(b.ProfileName, q) ||

                Contains(b.Id, q)));



        FilteredAzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(

            AzureBrandingLocalizations.Where(b =>

                Contains(b.Id, q) ||

                Contains(b.SignInPageText, q)));



        FilteredConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(

            ConditionalAccessPolicies.Where(p =>

                Contains(p.DisplayName, q) ||

                Contains(p.State?.ToString(), q) ||

                Contains(p.Id, q)));



        FilteredAssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(

            AssignmentFilters.Where(f =>

                Contains(f.DisplayName, q) ||

                Contains(f.Platform?.ToString(), q) ||

                Contains(f.AssignmentFilterManagementType?.ToString(), q) ||

                Contains(f.Id, q)));



        FilteredPolicySets = new ObservableCollection<PolicySet>(

            PolicySets.Where(p =>

                Contains(p.DisplayName, q) ||

                Contains(p.Description, q) ||

                Contains(p.Id, q)));



        FilteredAutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(

            AutopilotProfiles.Where(p =>

                Contains(TryReadStringProperty(p, "DisplayName"), q) ||

                Contains(TryReadStringProperty(p, "Description"), q) ||

                Contains(p.Id, q)));



        FilteredDeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(

            DeviceHealthScripts.Where(s =>

                Contains(TryReadStringProperty(s, "DisplayName"), q) ||

                Contains(TryReadStringProperty(s, "Description"), q) ||

                Contains(s.Id, q)));



        FilteredMacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(

            MacCustomAttributes.Where(a =>

                Contains(TryReadStringProperty(a, "DisplayName"), q) ||

                Contains(TryReadStringProperty(a, "Description"), q) ||

                Contains(a.Id, q)));



        FilteredFeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(

            FeatureUpdateProfiles.Where(p =>

                Contains(TryReadStringProperty(p, "DisplayName"), q) ||

                Contains(TryReadStringProperty(p, "Description"), q) ||

                Contains(p.Id, q)));



        FilteredNamedLocations = new ObservableCollection<NamedLocation>(

            NamedLocations.Where(n =>

                Contains(TryReadStringProperty(n, "DisplayName"), q) ||

                Contains(TryReadStringProperty(n, "Description"), q) ||

                Contains(n.Id, q)));



        FilteredAuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(

            AuthenticationStrengthPolicies.Where(p =>

                Contains(TryReadStringProperty(p, "DisplayName"), q) ||

                Contains(TryReadStringProperty(p, "Description"), q) ||

                Contains(p.Id, q)));



        FilteredAuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(

            AuthenticationContextClassReferences.Where(c =>

                Contains(TryReadStringProperty(c, "DisplayName"), q) ||

                Contains(TryReadStringProperty(c, "Description"), q) ||

                Contains(c.Id, q)));



        FilteredTermsOfUseAgreements = new ObservableCollection<Agreement>(

            TermsOfUseAgreements.Where(a =>

                Contains(TryReadStringProperty(a, "DisplayName"), q) ||

                Contains(TryReadStringProperty(a, "Description"), q) ||

                Contains(a.Id, q)));

        FilteredDeviceManagementScripts = new ObservableCollection<DeviceManagementScript>(

            DeviceManagementScripts.Where(s =>

                Contains(TryReadStringProperty(s, "DisplayName"), q) ||

                Contains(TryReadStringProperty(s, "Description"), q) ||

                Contains(TryReadStringProperty(s, "FileName"), q) ||

                Contains(s.Id, q)));

        FilteredDeviceShellScripts = new ObservableCollection<DeviceShellScript>(

            DeviceShellScripts.Where(s =>

                Contains(TryReadStringProperty(s, "DisplayName"), q) ||

                Contains(TryReadStringProperty(s, "Description"), q) ||

                Contains(TryReadStringProperty(s, "FileName"), q) ||

                Contains(s.Id, q)));

        FilteredComplianceScripts = new ObservableCollection<DeviceComplianceScript>(

            ComplianceScripts.Where(s =>

                Contains(TryReadStringProperty(s, "DisplayName"), q) ||

                Contains(TryReadStringProperty(s, "Description"), q) ||

                Contains(TryReadStringProperty(s, "Publisher"), q) ||

                Contains(s.Id, q)));

        OnPropertyChanged(nameof(IsCurrentCategoryEmpty));

    }



    private static bool Contains(string? source, string search)

        => source?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;



}

