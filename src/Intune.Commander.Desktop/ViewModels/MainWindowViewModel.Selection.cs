using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{



    // --- Refresh single item from Graph ---



    [RelayCommand]

    private async Task RefreshSelectedItemAsync(CancellationToken cancellationToken)

    {

        try

        {

            if (IsDeviceConfigCategory && SelectedConfiguration?.Id != null && _configProfileService != null)

            {

                StatusText = $"Refreshing {SelectedConfiguration.DisplayName}...";

                var updated = await _configProfileService.GetDeviceConfigurationAsync(SelectedConfiguration.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = DeviceConfigurations.IndexOf(SelectedConfiguration);

                    if (idx >= 0)

                    {

                        DeviceConfigurations[idx] = updated;

                        SelectedConfiguration = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed device configuration: {updated.DisplayName}");

                }

            }

            else if (IsCompliancePolicyCategory && SelectedCompliancePolicy?.Id != null && _compliancePolicyService != null)

            {

                StatusText = $"Refreshing {SelectedCompliancePolicy.DisplayName}...";

                var updated = await _compliancePolicyService.GetCompliancePolicyAsync(SelectedCompliancePolicy.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = CompliancePolicies.IndexOf(SelectedCompliancePolicy);

                    if (idx >= 0)

                    {

                        CompliancePolicies[idx] = updated;

                        SelectedCompliancePolicy = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed compliance policy: {updated.DisplayName}");

                }

            }

            else if (IsApplicationCategory && SelectedApplication?.Id != null && _applicationService != null)

            {

                StatusText = $"Refreshing {SelectedApplication.DisplayName}...";

                var updated = await _applicationService.GetApplicationAsync(SelectedApplication.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = Applications.IndexOf(SelectedApplication);

                    if (idx >= 0)

                    {

                        Applications[idx] = updated;

                        SelectedApplication = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed application: {updated.DisplayName}");

                }

            }

            else if (IsSettingsCatalogCategory && SelectedSettingsCatalogPolicy?.Id != null && _settingsCatalogService != null)

            {

                StatusText = $"Refreshing {SelectedSettingsCatalogPolicy.Name}...";

                var updated = await _settingsCatalogService.GetSettingsCatalogPolicyAsync(SelectedSettingsCatalogPolicy.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = SettingsCatalogPolicies.IndexOf(SelectedSettingsCatalogPolicy);

                    if (idx >= 0)

                    {

                        SettingsCatalogPolicies[idx] = updated;

                        SelectedSettingsCatalogPolicy = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed settings catalog policy: {updated.Name}");

                }

            }

            else if (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent?.Id != null && _endpointSecurityService != null)

            {

                StatusText = $"Refreshing {SelectedEndpointSecurityIntent.DisplayName}...";

                var updated = await _endpointSecurityService.GetEndpointSecurityIntentAsync(SelectedEndpointSecurityIntent.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = EndpointSecurityIntents.IndexOf(SelectedEndpointSecurityIntent);

                    if (idx >= 0)

                    {

                        EndpointSecurityIntents[idx] = updated;

                        SelectedEndpointSecurityIntent = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed endpoint security intent: {updated.DisplayName}");

                }

            }

            else if (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate?.Id != null && _administrativeTemplateService != null)

            {

                StatusText = $"Refreshing {SelectedAdministrativeTemplate.DisplayName}...";

                var updated = await _administrativeTemplateService.GetAdministrativeTemplateAsync(SelectedAdministrativeTemplate.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AdministrativeTemplates.IndexOf(SelectedAdministrativeTemplate);

                    if (idx >= 0)

                    {

                        AdministrativeTemplates[idx] = updated;

                        SelectedAdministrativeTemplate = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed administrative template: {updated.DisplayName}");

                }

            }

            else if (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration?.Id != null && _enrollmentConfigurationService != null)

            {

                StatusText = $"Refreshing {SelectedEnrollmentConfiguration.DisplayName}...";

                var updated = await _enrollmentConfigurationService.GetEnrollmentConfigurationAsync(SelectedEnrollmentConfiguration.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = EnrollmentConfigurations.IndexOf(SelectedEnrollmentConfiguration);

                    if (idx >= 0)

                    {

                        EnrollmentConfigurations[idx] = updated;

                        SelectedEnrollmentConfiguration = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed enrollment configuration: {updated.DisplayName}");

                }

            }

            else if (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy?.Id != null && _appProtectionPolicyService != null)

            {

                StatusText = $"Refreshing {SelectedAppProtectionPolicy.DisplayName}...";

                var updated = await _appProtectionPolicyService.GetAppProtectionPolicyAsync(SelectedAppProtectionPolicy.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AppProtectionPolicies.IndexOf(SelectedAppProtectionPolicy);

                    if (idx >= 0)

                    {

                        AppProtectionPolicies[idx] = updated;

                        SelectedAppProtectionPolicy = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed app protection policy: {updated.DisplayName}");

                }

            }

            else if (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration?.Id != null && _managedAppConfigurationService != null)

            {

                StatusText = $"Refreshing {SelectedManagedDeviceAppConfiguration.DisplayName}...";

                var updated = await _managedAppConfigurationService.GetManagedDeviceAppConfigurationAsync(SelectedManagedDeviceAppConfiguration.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = ManagedDeviceAppConfigurations.IndexOf(SelectedManagedDeviceAppConfiguration);

                    if (idx >= 0)

                    {

                        ManagedDeviceAppConfigurations[idx] = updated;

                        SelectedManagedDeviceAppConfiguration = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed managed device app configuration: {updated.DisplayName}");

                }

            }

            else if (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration?.Id != null && _managedAppConfigurationService != null)

            {

                StatusText = $"Refreshing {SelectedTargetedManagedAppConfiguration.DisplayName}...";

                var updated = await _managedAppConfigurationService.GetTargetedManagedAppConfigurationAsync(SelectedTargetedManagedAppConfiguration.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = TargetedManagedAppConfigurations.IndexOf(SelectedTargetedManagedAppConfiguration);

                    if (idx >= 0)

                    {

                        TargetedManagedAppConfigurations[idx] = updated;

                        SelectedTargetedManagedAppConfiguration = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed targeted managed app configuration: {updated.DisplayName}");

                }

            }

            else if (IsTermsAndConditionsCategory && SelectedTermsAndConditions?.Id != null && _termsAndConditionsService != null)

            {

                StatusText = $"Refreshing {SelectedTermsAndConditions.DisplayName}...";

                var updated = await _termsAndConditionsService.GetTermsAndConditionsAsync(SelectedTermsAndConditions.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = TermsAndConditionsCollection.IndexOf(SelectedTermsAndConditions);

                    if (idx >= 0)

                    {

                        TermsAndConditionsCollection[idx] = updated;

                        SelectedTermsAndConditions = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed terms and conditions: {updated.DisplayName}");

                }

            }

            else if (IsScopeTagsCategory && SelectedScopeTag?.Id != null && _scopeTagService != null)

            {

                StatusText = $"Refreshing {SelectedScopeTag.DisplayName}...";

                var updated = await _scopeTagService.GetScopeTagAsync(SelectedScopeTag.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = ScopeTags.IndexOf(SelectedScopeTag);

                    if (idx >= 0)

                    {

                        ScopeTags[idx] = updated;

                        SelectedScopeTag = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed scope tag: {updated.DisplayName}");

                }

            }

            else if (IsRoleDefinitionsCategory && SelectedRoleDefinition?.Id != null && _roleDefinitionService != null)

            {

                StatusText = $"Refreshing {SelectedRoleDefinition.DisplayName}...";

                var updated = await _roleDefinitionService.GetRoleDefinitionAsync(SelectedRoleDefinition.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = RoleDefinitions.IndexOf(SelectedRoleDefinition);

                    if (idx >= 0)

                    {

                        RoleDefinitions[idx] = updated;

                        SelectedRoleDefinition = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed role definition: {updated.DisplayName}");

                }

            }

            else if (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile?.Id != null && _intuneBrandingService != null)

            {

                StatusText = $"Refreshing {SelectedIntuneBrandingProfile.ProfileName ?? SelectedIntuneBrandingProfile.DisplayName}...";

                var updated = await _intuneBrandingService.GetIntuneBrandingProfileAsync(SelectedIntuneBrandingProfile.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = IntuneBrandingProfiles.IndexOf(SelectedIntuneBrandingProfile);

                    if (idx >= 0)

                    {

                        IntuneBrandingProfiles[idx] = updated;

                        SelectedIntuneBrandingProfile = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed Intune branding profile: {updated.ProfileName ?? updated.DisplayName}");

                }

            }

            else if (IsAzureBrandingCategory && SelectedAzureBrandingLocalization?.Id != null && _azureBrandingService != null)

            {

                StatusText = $"Refreshing {SelectedAzureBrandingLocalization.Id}...";

                var updated = await _azureBrandingService.GetBrandingLocalizationAsync(SelectedAzureBrandingLocalization.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AzureBrandingLocalizations.IndexOf(SelectedAzureBrandingLocalization);

                    if (idx >= 0)

                    {

                        AzureBrandingLocalizations[idx] = updated;

                        SelectedAzureBrandingLocalization = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed Azure branding localization: {updated.Id}");

                }

            }

            else if (IsConditionalAccessCategory && SelectedConditionalAccessPolicy?.Id != null && _conditionalAccessPolicyService != null)

            {

                StatusText = $"Refreshing {SelectedConditionalAccessPolicy.DisplayName}...";

                var updated = await _conditionalAccessPolicyService.GetPolicyAsync(SelectedConditionalAccessPolicy.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = ConditionalAccessPolicies.IndexOf(SelectedConditionalAccessPolicy);

                    if (idx >= 0)

                    {

                        ConditionalAccessPolicies[idx] = updated;

                        SelectedConditionalAccessPolicy = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed conditional access policy: {updated.DisplayName}");

                }

            }

            else if (IsAssignmentFiltersCategory && SelectedAssignmentFilter?.Id != null && _assignmentFilterService != null)

            {

                StatusText = $"Refreshing {SelectedAssignmentFilter.DisplayName}...";

                var updated = await _assignmentFilterService.GetFilterAsync(SelectedAssignmentFilter.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AssignmentFilters.IndexOf(SelectedAssignmentFilter);

                    if (idx >= 0)

                    {

                        AssignmentFilters[idx] = updated;

                        SelectedAssignmentFilter = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed assignment filter: {updated.DisplayName}");

                }

            }

            else if (IsPolicySetsCategory && SelectedPolicySet?.Id != null && _policySetService != null)

            {

                StatusText = $"Refreshing {SelectedPolicySet.DisplayName}...";

                var updated = await _policySetService.GetPolicySetAsync(SelectedPolicySet.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = PolicySets.IndexOf(SelectedPolicySet);

                    if (idx >= 0)

                    {

                        PolicySets[idx] = updated;

                        SelectedPolicySet = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed policy set: {updated.DisplayName}");

                }

            }

            else if (IsAutopilotProfilesCategory && SelectedAutopilotProfile?.Id != null && _autopilotService != null)

            {

                StatusText = "Refreshing autopilot profile...";

                var updated = await _autopilotService.GetAutopilotProfileAsync(SelectedAutopilotProfile.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AutopilotProfiles.IndexOf(SelectedAutopilotProfile);

                    if (idx >= 0)

                    {

                        AutopilotProfiles[idx] = updated;

                        SelectedAutopilotProfile = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed autopilot profile: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsDeviceHealthScriptsCategory && SelectedDeviceHealthScript?.Id != null && _deviceHealthScriptService != null)

            {

                StatusText = "Refreshing device health script...";

                var updated = await _deviceHealthScriptService.GetDeviceHealthScriptAsync(SelectedDeviceHealthScript.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = DeviceHealthScripts.IndexOf(SelectedDeviceHealthScript);

                    if (idx >= 0)

                    {

                        DeviceHealthScripts[idx] = updated;

                        SelectedDeviceHealthScript = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed device health script: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsMacCustomAttributesCategory && SelectedMacCustomAttribute?.Id != null && _macCustomAttributeService != null)

            {

                StatusText = "Refreshing mac custom attribute...";

                var updated = await _macCustomAttributeService.GetMacCustomAttributeAsync(SelectedMacCustomAttribute.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = MacCustomAttributes.IndexOf(SelectedMacCustomAttribute);

                    if (idx >= 0)

                    {

                        MacCustomAttributes[idx] = updated;

                        SelectedMacCustomAttribute = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed mac custom attribute: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsFeatureUpdatesCategory && SelectedFeatureUpdateProfile?.Id != null && _featureUpdateProfileService != null)

            {

                StatusText = "Refreshing feature update profile...";

                var updated = await _featureUpdateProfileService.GetFeatureUpdateProfileAsync(SelectedFeatureUpdateProfile.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = FeatureUpdateProfiles.IndexOf(SelectedFeatureUpdateProfile);

                    if (idx >= 0)

                    {

                        FeatureUpdateProfiles[idx] = updated;

                        SelectedFeatureUpdateProfile = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed feature update profile: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsNamedLocationsCategory && SelectedNamedLocation?.Id != null && _namedLocationService != null)

            {

                StatusText = "Refreshing named location...";

                var updated = await _namedLocationService.GetNamedLocationAsync(SelectedNamedLocation.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = NamedLocations.IndexOf(SelectedNamedLocation);

                    if (idx >= 0)

                    {

                        NamedLocations[idx] = updated;

                        SelectedNamedLocation = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed named location: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsAuthenticationStrengthsCategory && SelectedAuthenticationStrengthPolicy?.Id != null && _authenticationStrengthService != null)

            {

                StatusText = "Refreshing authentication strength...";

                var updated = await _authenticationStrengthService.GetAuthenticationStrengthPolicyAsync(SelectedAuthenticationStrengthPolicy.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AuthenticationStrengthPolicies.IndexOf(SelectedAuthenticationStrengthPolicy);

                    if (idx >= 0)

                    {

                        AuthenticationStrengthPolicies[idx] = updated;

                        SelectedAuthenticationStrengthPolicy = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed authentication strength: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsAuthenticationContextsCategory && SelectedAuthenticationContextClassReference?.Id != null && _authenticationContextService != null)

            {

                StatusText = "Refreshing authentication context...";

                var updated = await _authenticationContextService.GetAuthenticationContextAsync(SelectedAuthenticationContextClassReference.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = AuthenticationContextClassReferences.IndexOf(SelectedAuthenticationContextClassReference);

                    if (idx >= 0)

                    {

                        AuthenticationContextClassReferences[idx] = updated;

                        SelectedAuthenticationContextClassReference = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed authentication context: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsTermsOfUseCategory && SelectedTermsOfUseAgreement?.Id != null && _termsOfUseService != null)

            {

                StatusText = "Refreshing terms of use...";

                var updated = await _termsOfUseService.GetTermsOfUseAgreementAsync(SelectedTermsOfUseAgreement.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = TermsOfUseAgreements.IndexOf(SelectedTermsOfUseAgreement);

                    if (idx >= 0)

                    {

                        TermsOfUseAgreements[idx] = updated;

                        SelectedTermsOfUseAgreement = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed terms of use agreement: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsDeviceManagementScriptsCategory && SelectedDeviceManagementScript?.Id != null && _deviceManagementScriptService != null)

            {

                StatusText = "Refreshing device management script...";

                var updated = await _deviceManagementScriptService.GetDeviceManagementScriptAsync(SelectedDeviceManagementScript.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = DeviceManagementScripts.IndexOf(SelectedDeviceManagementScript);

                    if (idx >= 0)

                    {

                        DeviceManagementScripts[idx] = updated;

                        SelectedDeviceManagementScript = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed device management script: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsDeviceShellScriptsCategory && SelectedDeviceShellScript?.Id != null && _deviceShellScriptService != null)

            {

                StatusText = "Refreshing device shell script...";

                var updated = await _deviceShellScriptService.GetDeviceShellScriptAsync(SelectedDeviceShellScript.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = DeviceShellScripts.IndexOf(SelectedDeviceShellScript);

                    if (idx >= 0)

                    {

                        DeviceShellScripts[idx] = updated;

                        SelectedDeviceShellScript = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed device shell script: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsComplianceScriptsCategory && SelectedComplianceScript?.Id != null && _complianceScriptService != null)

            {

                StatusText = "Refreshing compliance script...";

                var updated = await _complianceScriptService.GetComplianceScriptAsync(SelectedComplianceScript.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = ComplianceScripts.IndexOf(SelectedComplianceScript);

                    if (idx >= 0)

                    {

                        ComplianceScripts[idx] = updated;

                        SelectedComplianceScript = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed compliance script: {TryReadStringProperty(updated, "DisplayName")}");

                }

            }

            else if (IsQualityUpdatesCategory && SelectedQualityUpdateProfile?.Id != null && _qualityUpdateProfileService != null)

            {

                StatusText = "Refreshing quality update profile...";

                var updated = await _qualityUpdateProfileService.GetQualityUpdateProfileAsync(SelectedQualityUpdateProfile.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = QualityUpdateProfiles.IndexOf(SelectedQualityUpdateProfile);

                    if (idx >= 0)

                    {

                        QualityUpdateProfiles[idx] = updated;

                        SelectedQualityUpdateProfile = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed quality update profile: {updated.DisplayName}");

                }

            }

            else if (IsDriverUpdatesCategory && SelectedDriverUpdateProfile?.Id != null && _driverUpdateProfileService != null)

            {

                StatusText = "Refreshing driver update profile...";

                var updated = await _driverUpdateProfileService.GetDriverUpdateProfileAsync(SelectedDriverUpdateProfile.Id, cancellationToken);

                if (updated != null)

                {

                    var idx = DriverUpdateProfiles.IndexOf(SelectedDriverUpdateProfile);

                    if (idx >= 0)

                    {

                        DriverUpdateProfiles[idx] = updated;

                        SelectedDriverUpdateProfile = updated;

                    }

                    DebugLog.Log("Graph", $"Refreshed driver update profile: {updated.DisplayName}");

                }

            }

            else

            {

                return;

            }



            StatusText = "Item refreshed";

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to refresh item: {FormatGraphError(ex)}", ex);

            SetError($"Refresh failed: {FormatGraphError(ex)}");

        }

    }



    public bool CanRefreshSelectedItem =>

        (IsDeviceConfigCategory && SelectedConfiguration != null) ||

        (IsCompliancePolicyCategory && SelectedCompliancePolicy != null) ||

        (IsApplicationCategory && SelectedApplication != null) ||

        (IsSettingsCatalogCategory && SelectedSettingsCatalogPolicy != null) ||

        (IsEndpointSecurityCategory && SelectedEndpointSecurityIntent != null) ||

        (IsAdministrativeTemplatesCategory && SelectedAdministrativeTemplate != null) ||

        (IsEnrollmentConfigurationsCategory && SelectedEnrollmentConfiguration != null) ||

        (IsAppProtectionPoliciesCategory && SelectedAppProtectionPolicy != null) ||

        (IsManagedDeviceAppConfigurationsCategory && SelectedManagedDeviceAppConfiguration != null) ||

        (IsTargetedManagedAppConfigurationsCategory && SelectedTargetedManagedAppConfiguration != null) ||

        (IsTermsAndConditionsCategory && SelectedTermsAndConditions != null) ||

        (IsScopeTagsCategory && SelectedScopeTag != null) ||

        (IsRoleDefinitionsCategory && SelectedRoleDefinition != null) ||

        (IsIntuneBrandingCategory && SelectedIntuneBrandingProfile != null) ||

        (IsAzureBrandingCategory && SelectedAzureBrandingLocalization != null) ||

        (IsConditionalAccessCategory && SelectedConditionalAccessPolicy != null) ||

        (IsAssignmentFiltersCategory && SelectedAssignmentFilter != null) ||

        (IsPolicySetsCategory && SelectedPolicySet != null) ||

        (IsAutopilotProfilesCategory && SelectedAutopilotProfile != null) ||

        (IsDeviceHealthScriptsCategory && SelectedDeviceHealthScript != null) ||

        (IsMacCustomAttributesCategory && SelectedMacCustomAttribute != null) ||

        (IsFeatureUpdatesCategory && SelectedFeatureUpdateProfile != null) ||

        (IsNamedLocationsCategory && SelectedNamedLocation != null) ||

        (IsAuthenticationStrengthsCategory && SelectedAuthenticationStrengthPolicy != null) ||

        (IsAuthenticationContextsCategory && SelectedAuthenticationContextClassReference != null) ||

        (IsTermsOfUseCategory && SelectedTermsOfUseAgreement != null) ||

        (IsDeviceManagementScriptsCategory && SelectedDeviceManagementScript != null) ||

        (IsDeviceShellScriptsCategory && SelectedDeviceShellScript != null) ||

        (IsComplianceScriptsCategory && SelectedComplianceScript != null) ||

        (IsQualityUpdatesCategory && SelectedQualityUpdateProfile != null) ||

        (IsDriverUpdatesCategory && SelectedDriverUpdateProfile != null);



    // --- Selection-changed handlers (load detail + assignments) ---



    partial void OnSelectedConfigurationChanged(DeviceConfiguration? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>((IEnumerable<string>)value.RoleScopeTagIds.Cast<string>())
            : [];
        // OmaSettings is a typed property on subclasses (e.g. Windows10CustomConfiguration),
        // NOT stored in AdditionalData. Use reflection to find it generically.
        var omaSettingsList = value?.GetType()
            .GetProperty("OmaSettings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.GetValue(value) as System.Collections.IList;
        SelectedItemOmaSettingsCount = omaSettingsList?.Count ?? 0;

        // Extract all typed settings from the derived Device Configuration type
        if (value != null)
        {
            var settings = ExtractGraphObjectSettings(value);
            SelectedItemConfigurationSettings = new ObservableCollection<Models.SettingItem>(
                settings.Select(s => new Models.SettingItem(s.Label, s.Value)));
        }
        else
        {
            SelectedItemConfigurationSettings = [];
        }

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)

            _ = LoadConfigAssignmentsAsync(value.Id);

    }



    partial void OnSelectedCompliancePolicyChanged(DeviceCompliancePolicy? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Compliance policy specific - extract settings
        if (value != null)
        {
            var settings = ExtractComplianceSettings(value);
            SelectedItemComplianceSettings = new ObservableCollection<Models.SettingItem>(
                settings.Select(s => new Models.SettingItem(s.Label, s.Value)));
        }
        else
        {
            SelectedItemComplianceSettings = [];
        }

        // Compliance policy specific - extract non-compliance actions
        var configs = value?.ScheduledActionsForRule
            ?.SelectMany(r => r.ScheduledActionConfigurations ?? [])
            .ToList() ?? [];
        SelectedItemNonComplianceActions = new ObservableCollection<Models.NonComplianceActionItem>(
            configs.Select(c => new Models.NonComplianceActionItem(
                c.ActionType?.ToString() ?? "Unknown",
                c.GracePeriodHours ?? 0,
                c.NotificationTemplateId ?? "")));

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)

            _ = LoadCompliancePolicyAssignmentsAsync(value.Id);

    }



    partial void OnSelectedSettingsCatalogPolicyChanged(DeviceManagementConfigurationPolicy? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = value?.Platforms?.ToString() ?? "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Settings Catalog specific
        // Settings collection is NOT populated in list responses (requires $expand=settings).
        // SettingCount is an integer property that IS available from the list API.
        SelectedItemSettingsCount = value?.SettingCount ?? 0;
        SelectedItemTemplateFamilies = !string.IsNullOrEmpty(value?.TemplateReference?.TemplateFamily?.ToString())
            ? new ObservableCollection<string>(new[] { value.TemplateReference.TemplateFamily.ToString() ?? "" })
            : [];

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        SelectedItemCatalogSettings = [];

        if (value?.Id != null)
        {
            _ = LoadSettingsCatalogAssignmentsAsync(value.Id);
            _ = LoadSettingsCatalogSettingsAsync(value.Id);
        }

    }



    partial void OnSelectedApplicationChanged(MobileApp? value)

    {

        SelectedItemAssignments.Clear();

        var odataType = value?.OdataType;

        SelectedItemTypeName = FriendlyODataType(odataType);

        SelectedItemPlatform = InferPlatform(odataType);

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // App-specific properties
        if (value != null)
        {
            SelectedItemVersion = ExtractVersion(value);
            SelectedItemBundleId = ExtractBundleId(value);
            SelectedItemMinimumOS = ExtractMinimumOS(value);
            SelectedItemInstallCommand = ExtractInstallCommand(value);
            SelectedItemUninstallCommand = ExtractUninstallCommand(value);
            SelectedItemInstallContext = ExtractInstallContext(value);
            SelectedItemSizeMB = ExtractSizeInMB(value);
            SelectedItemCategories = ExtractCategories(value);
            SelectedItemSupersededCount = ExtractSupersededCount(value);
            SelectedItemAppStoreUrl = ExtractAppStoreUrl(value);
        }

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)

            _ = LoadApplicationAssignmentsAsync(value.Id);

    }



    partial void OnSelectedConditionalAccessPolicyChanged(ConditionalAccessPolicy? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Conditional Access";

        SelectedItemPlatform = "";


        // Common properties
        SelectedItemDescription = value?.Description ?? "";

        // Resolve location names (local lookup — named locations are not directory objects)
        var inclLocs = value?.Conditions?.Locations?.IncludeLocations ?? [];
        SelectedCAPolicyIncludeLocations = new ObservableCollection<string>(
            inclLocs.Select(id => id switch {
                "All" => "All Locations",
                "AllTrusted" => "All Trusted Locations",
                _ => ResolveNamedLocationId(id)
            }));
        var exclLocs = value?.Conditions?.Locations?.ExcludeLocations ?? [];
        SelectedCAPolicyExcludeLocations = new ObservableCollection<string>(
            exclLocs.Select(id => id switch {
                "All" => "All Locations",
                "AllTrusted" => "All Trusted Locations",
                _ => ResolveNamedLocationId(id)
            }));

        // Seed groups synchronously from local cache as fallback (replaced async below)
        var inclGroups = value?.Conditions?.Users?.IncludeGroups ?? [];
        SelectedCAPolicyIncludeGroups = new ObservableCollection<string>(inclGroups.Select(ResolveGroupId));
        var exclGroups = value?.Conditions?.Users?.ExcludeGroups ?? [];
        SelectedCAPolicyExcludeGroups = new ObservableCollection<string>(exclGroups.Select(ResolveGroupId));

        // Seed users synchronously (raw GUIDs as placeholder — resolved async below)
        var inclUsers = value?.Conditions?.Users?.IncludeUsers ?? [];
        SelectedCAPolicyIncludeUsers = new ObservableCollection<string>(
            inclUsers.Select(id => id switch {
                "All" => "All Users",
                "None" => "None",
                "GuestsOrExternalUsers" => "Guests or external users",
                _ => id
            }));
        var exclUsers = value?.Conditions?.Users?.ExcludeUsers ?? [];
        SelectedCAPolicyExcludeUsers = new ObservableCollection<string>(
            exclUsers.Select(id => id switch {
                "All" => "All Users",
                "None" => "None",
                "GuestsOrExternalUsers" => "Guests or external users",
                _ => id
            }));

        // Seed roles synchronously (raw GUIDs as placeholder — resolved async below)
        var inclRoles = value?.Conditions?.Users?.IncludeRoles ?? [];
        SelectedCAPolicyIncludeRoles = new ObservableCollection<string>(inclRoles);
        var exclRoles = value?.Conditions?.Users?.ExcludeRoles ?? [];
        SelectedCAPolicyExcludeRoles = new ObservableCollection<string>(exclRoles);

        // Resolve app names
        var inclApps = value?.Conditions?.Applications?.IncludeApplications ?? [];
        SelectedCAPolicyIncludeApps = new ObservableCollection<string>(
            inclApps.Select(id => id switch {
                "All" => "All Applications",
                "Office365" => "Office 365",
                _ => ResolveApplicationId(id)
            }));
        var exclApps = value?.Conditions?.Applications?.ExcludeApplications ?? [];
        SelectedCAPolicyExcludeApps = new ObservableCollection<string>(
            exclApps.Select(id => ResolveApplicationId(id)));

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        // Fire async GUID resolution to replace raw GUIDs with display names
        if (value != null)
            _ = ResolveCAPolicyGuidsAsync(value);

    }

    /// <summary>
    /// Resolves all GUIDs in a CA policy (groups, users, roles, apps) to display names
    /// using the DirectoryObjectResolver batch API. Updates the SelectedCAPolicy* collections
    /// once resolved. Named locations are excluded (not directory objects).
    /// </summary>
    private async Task ResolveCAPolicyGuidsAsync(ConditionalAccessPolicy policy)
    {
        if (_directoryObjectResolver == null) return;

        // Collect all raw GUIDs, filtering out special keyword values
        var allGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddGuids(IReadOnlyList<string>? ids)
        {
            if (ids == null) return;
            foreach (var id in ids)
                if (!IsSpecialCaValue(id))
                    allGuids.Add(id);
        }

        AddGuids(policy.Conditions?.Users?.IncludeUsers);
        AddGuids(policy.Conditions?.Users?.ExcludeUsers);
        AddGuids(policy.Conditions?.Users?.IncludeGroups);
        AddGuids(policy.Conditions?.Users?.ExcludeGroups);
        AddGuids(policy.Conditions?.Users?.IncludeRoles);
        AddGuids(policy.Conditions?.Users?.ExcludeRoles);
        AddGuids(policy.Conditions?.Applications?.IncludeApplications);
        AddGuids(policy.Conditions?.Applications?.ExcludeApplications);

        if (allGuids.Count == 0) return;

        IsLoadingDetails = true;
        try
        {
            var nameMap = await _directoryObjectResolver.ResolveAsync([.. allGuids]);

            // Guard: abort if a different policy was selected while we were awaiting
            if (SelectedConditionalAccessPolicy?.Id != policy.Id) return;

            string Resolve(string? id) => id switch
            {
                null or "" => "",
                "All" => "All",
                "None" => "None",
                "AllTrusted" => "All trusted locations",
                "GuestsOrExternalUsers" => "Guests or external users",
                "Office365" => "Office 365",
                "MicrosoftAdminPortals" => "Microsoft Admin Portals",
                _ => nameMap.TryGetValue(id, out var name) ? name : id
            };

            string Resolve2(string? id) => id switch
            {
                null or "" => "",
                "All" => "All Users",
                "None" => "None",
                "GuestsOrExternalUsers" => "Guests or external users",
                _ => nameMap.TryGetValue(id, out var name) ? name : id
            };

            string ResolveApp(string? id) => id switch
            {
                null or "" => "",
                "All" => "All Applications",
                "Office365" => "Office 365",
                "MicrosoftAdminPortals" => "Microsoft Admin Portals",
                _ => nameMap.TryGetValue(id, out var name) ? name : ResolveApplicationId(id)
            };

            SelectedCAPolicyIncludeGroups = new ObservableCollection<string>(
                (policy.Conditions?.Users?.IncludeGroups ?? []).Select(id => Resolve(id)));
            SelectedCAPolicyExcludeGroups = new ObservableCollection<string>(
                (policy.Conditions?.Users?.ExcludeGroups ?? []).Select(id => Resolve(id)));
            SelectedCAPolicyIncludeUsers = new ObservableCollection<string>(
                (policy.Conditions?.Users?.IncludeUsers ?? []).Select(id => Resolve2(id)));
            SelectedCAPolicyExcludeUsers = new ObservableCollection<string>(
                (policy.Conditions?.Users?.ExcludeUsers ?? []).Select(id => Resolve2(id)));
            SelectedCAPolicyIncludeRoles = new ObservableCollection<string>(
                (policy.Conditions?.Users?.IncludeRoles ?? []).Select(id => Resolve(id)));
            SelectedCAPolicyExcludeRoles = new ObservableCollection<string>(
                (policy.Conditions?.Users?.ExcludeRoles ?? []).Select(id => Resolve(id)));
            SelectedCAPolicyIncludeApps = new ObservableCollection<string>(
                (policy.Conditions?.Applications?.IncludeApplications ?? []).Select(id => ResolveApp(id)));
            SelectedCAPolicyExcludeApps = new ObservableCollection<string>(
                (policy.Conditions?.Applications?.ExcludeApplications ?? []).Select(id => ResolveApp(id)));
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to resolve CA policy GUIDs: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    /// <summary>Returns true for CA keyword values that are not directory object GUIDs.</summary>
    private static bool IsSpecialCaValue(string id)
    {
        return id is
        "All" or "None" or "AllTrusted" or "GuestsOrExternalUsers" or
        "Office365" or "MicrosoftAdminPortals";
    }

    partial void OnSelectedEndpointSecurityIntentChanged(DeviceManagementIntent? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Endpoint Security";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Endpoint Security specific
        SelectedItemTemplateDisplayName = value?.TemplateId ?? "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)

            _ = LoadEndpointSecurityAssignmentsAsync(value.Id);

    }



    partial void OnSelectedAdministrativeTemplateChanged(GroupPolicyConfiguration? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Administrative Template";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Admin Template specific
        SelectedItemIngestionType = value?.PolicyConfigurationIngestionType?.ToString() ?? "";
        SelectedItemCreatedDateTime = value?.CreatedDateTime;

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)

            _ = LoadAdministrativeTemplateAssignmentsAsync(value.Id);

    }



    partial void OnSelectedEnrollmentConfigurationChanged(DeviceEnrollmentConfiguration? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadEnrollmentConfigurationAssignmentsAsync(value.Id);

    }



    partial void OnSelectedAppProtectionPolicyChanged(ManagedAppPolicy? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // App Protection specific — cast to ManagedAppProtection to get version requirements
        if (value is ManagedAppProtection prot)
        {
            SelectedItemMinAppVersion = prot.MinimumRequiredAppVersion ?? "";
            SelectedItemMinOSVersion = prot.MinimumRequiredOsVersion ?? "";
        }
        else
        {
            SelectedItemMinAppVersion = "";
            SelectedItemMinOSVersion = "";
        }


        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadAppProtectionPolicyAssignmentsAsync(value);

    }



    partial void OnSelectedManagedDeviceAppConfigurationChanged(ManagedDeviceMobileAppConfiguration? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadManagedDeviceAppConfigurationAssignmentsAsync(value.Id);

    }



    partial void OnSelectedTargetedManagedAppConfigurationChanged(TargetedManagedAppConfiguration? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = FriendlyODataType(value?.OdataType);

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadTargetedManagedAppConfigurationAssignmentsAsync(value.Id);

    }



    partial void OnSelectedTermsAndConditionsChanged(TermsAndConditions? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Terms and Conditions";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        
        // Terms of Use specific
        // Note: AcceptanceStat not directly available on TermsAndConditions

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadTermsAndConditionsAssignmentsAsync(value.Id);

    }



    partial void OnSelectedScopeTagChanged(RoleScopeTag? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Scope Tag";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedRoleDefinitionChanged(RoleDefinition? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Role Definition";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedIntuneBrandingProfileChanged(IntuneBrandingProfile? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Intune Branding";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedAzureBrandingLocalizationChanged(OrganizationalBrandingLocalization? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Azure Branding";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedAssignmentFilterChanged(DeviceAndAppManagementAssignmentFilter? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Assignment Filter";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedPolicySetChanged(PolicySet? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Policy Set";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedAutopilotProfileChanged(WindowsAutopilotDeploymentProfile? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Autopilot Profile";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Autopilot specific
        SelectedItemProfileType = value?.DeviceType?.ToString() ?? "";
        SelectedItemLanguage = value?.Language ?? "";
        SelectedItemDeviceNameTemplate = value?.DeviceNameTemplate ?? "";

        var oobe = value?.OutOfBoxExperienceSettings;
        var oobeFlags = new List<string>();
        if (oobe?.HideEULA == true) oobeFlags.Add("Skip EULA");
        if (oobe?.HidePrivacySettings == true) oobeFlags.Add("Skip Privacy");
        if (oobe?.SkipKeyboardSelectionPage == true) oobeFlags.Add("Skip Keyboard");
        if (oobe?.HideEscapeLink == true) oobeFlags.Add("No Escape Link");
        SelectedItemOobeSkipFlags = new ObservableCollection<string>(oobeFlags);

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadAutopilotProfileAssignmentsAsync(value.Id);

    }



    partial void OnSelectedDeviceHealthScriptChanged(DeviceHealthScript? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Device Health Script";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Health Script specific
        SelectedItemPublisher = value?.Publisher ?? "";
        SelectedItemRunAsAccount = value?.RunAsAccount.HasValue == true
            ? value.RunAsAccount.Value.ToString()
            : "";
        SelectedItemRunAs32BitText = value?.RunAs32Bit == true ? "Yes" : "No";
        SelectedItemDetectionScript = "Loading...";
        SelectedItemRemediationScript = "Loading...";
        SelectedItemEnforceSignatureCheck = value?.EnforceSignatureCheck ?? false;
        SelectedItemRunAs32Bit = value?.RunAs32Bit ?? false;
        SelectedItemVersion = value?.Version ?? "";

        if (value?.Id != null)
        {
            _ = LoadDeviceHealthScriptAssignmentsAsync(value.Id);
            _ = LoadDeviceHealthScriptDetailsAsync(value.Id);
        }

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedMacCustomAttributeChanged(DeviceCustomAttributeShellScript? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Mac Custom Attribute";

        SelectedItemPlatform = "";

        SelectedItemRunAsAccount = value?.RunAsAccount.HasValue == true
            ? value.RunAsAccount.Value.ToString()
            : "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadMacCustomAttributeAssignmentsAsync(value.Id);

    }



    partial void OnSelectedFeatureUpdateProfileChanged(WindowsFeatureUpdateProfile? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Feature Update Profile";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        SelectedItemRoleScopeTags = value?.RoleScopeTagIds != null
            ? new ObservableCollection<string>(value.RoleScopeTagIds.Select(t => t ?? ""))
            : [];
        
        // Feature Update specific
        SelectedItemFeatureUpdateVersion = value?.FeatureUpdateVersion ?? "";
        SelectedItemRolloutStartDate = value?.RolloutSettings?.OfferStartDateTimeInUTC;
        SelectedItemRolloutEndDate = value?.RolloutSettings?.OfferEndDateTimeInUTC;
        SelectedItemInstallLatestOnEOL = value?.InstallLatestWindows10OnWindows11IneligibleDevice ?? false;
        SelectedItemCreatedDateTime = value?.CreatedDateTime;

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
            _ = LoadFeatureUpdateProfileAssignmentsAsync(value.Id);

    }



    partial void OnSelectedNamedLocationChanged(NamedLocation? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Named Location";

        SelectedItemPlatform = "";

        
        // Named Location specific (depends on type - IP or country)
        SelectedItemIpRanges = [];
        SelectedItemCountryCodes = [];
        SelectedItemIsTrusted = false;
        if (value is Microsoft.Graph.Beta.Models.IpNamedLocation ipLoc)
        {
            SelectedItemIpRanges = ipLoc.IpRanges != null
                ? new ObservableCollection<string>(ipLoc.IpRanges.Select(r => r switch {
                    Microsoft.Graph.Beta.Models.IPv4CidrRange v4 => v4.CidrAddress ?? "",
                    Microsoft.Graph.Beta.Models.IPv6CidrRange v6 => v6.CidrAddress ?? "",
                    _ => ""
                }).Where(s => s.Length > 0))
                : [];
            SelectedItemIsTrusted = ipLoc.IsTrusted ?? false;
        }
        else if (value is Microsoft.Graph.Beta.Models.CountryNamedLocation countryLoc)
        {
            SelectedItemCountryCodes = countryLoc.CountriesAndRegions != null
                ? new ObservableCollection<string>(countryLoc.CountriesAndRegions)
                : [];
        }

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedAuthenticationStrengthPolicyChanged(AuthenticationStrengthPolicy? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Authentication Strength";

        SelectedItemPlatform = "";

        
        // Common properties
        SelectedItemDescription = value?.Description ?? "";
        
        // Auth Strength specific
        SelectedItemPolicyType = value?.PolicyType?.ToString() ?? "";
        SelectedItemCreatedDateTime = value?.CreatedDateTime;
        SelectedItemAllowedCombinations = value?.AllowedCombinations != null
            ? new ObservableCollection<string>(
                value.AllowedCombinations
                    .Where(c => c.HasValue)
                    .Select(c => FormatAuthMethodMode(c!.Value)))
            : [];

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedAuthenticationContextClassReferenceChanged(AuthenticationContextClassReference? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Authentication Context";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedTermsOfUseAgreementChanged(Agreement? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Terms of Use";

        SelectedItemPlatform = "";

        // Terms of Use specific (Agreement has no Description or CreatedDateTime property)
        SelectedItemDescription = "";
        SelectedItemCreatedDateTime = null;
        SelectedItemIsPerDeviceAcceptance = value?.IsPerDeviceAcceptanceRequired ?? false;
        SelectedItemExpirationFrequency = value?.UserReacceptRequiredFrequency?.ToString() ?? "Never";

        if (value?.Id != null)
            _ = LoadTermsOfUseAssignmentsAsync(value.Id);

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedDeviceManagementScriptChanged(DeviceManagementScript? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Device Management Script";

        SelectedItemPlatform = "";

        SelectedItemScriptContent = "Loading...";

        SelectedItemRoleScopeTags = new ObservableCollection<string>(value?.RoleScopeTagIds ?? []);

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
        {
            _ = LoadDeviceManagementScriptAssignmentsAsync(value.Id);
            _ = LoadDeviceManagementScriptDetailsAsync(value.Id);
        }

    }



    partial void OnSelectedDeviceShellScriptChanged(DeviceShellScript? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Device Shell Script";

        SelectedItemPlatform = "";

        SelectedItemScriptContent = "Loading...";

        SelectedItemRoleScopeTags = new ObservableCollection<string>(value?.RoleScopeTagIds ?? []);

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

        if (value?.Id != null)
        {
            _ = LoadDeviceShellScriptAssignmentsAsync(value.Id);
            _ = LoadDeviceShellScriptDetailsAsync(value.Id);
        }

    }



    partial void OnSelectedComplianceScriptChanged(DeviceComplianceScript? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Compliance Script";

        SelectedItemPlatform = "";

        SelectedItemPublisher = value?.Publisher ?? "";
        SelectedItemRunAsAccount = value?.RunAsAccount.HasValue == true
            ? value.RunAsAccount.Value.ToString()
            : "";
        SelectedItemRunAs32BitText = value?.RunAs32Bit == true ? "Yes" : "No";
        SelectedItemEnforceSignatureCheck = value?.EnforceSignatureCheck ?? false;
        SelectedItemRoleScopeTags = new ObservableCollection<string>(value?.RoleScopeTagIds ?? []);

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedQualityUpdateProfileChanged(WindowsQualityUpdateProfile? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Quality Update Profile";

        SelectedItemPlatform = "";

        SelectedItemDaysUntilForcedReboot = value?.ExpeditedUpdateSettings?.DaysUntilForcedReboot;

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }



    partial void OnSelectedDriverUpdateProfileChanged(WindowsDriverUpdateProfile? value)

    {

        SelectedItemAssignments.Clear();

        SelectedItemTypeName = "Driver Update Profile";

        SelectedItemPlatform = "";

        OnPropertyChanged(nameof(CanRefreshSelectedItem));

    }

    partial void OnSelectedAdmxFileChanged(GroupPolicyUploadedDefinitionFile? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "ADMX File";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedReusablePolicySettingChanged(DeviceManagementReusablePolicySetting? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Reusable Policy Setting";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedNotificationTemplateChanged(NotificationMessageTemplate? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Notification Template";
        SelectedItemPlatform = "";
        SelectedItemNotificationMessages.Clear();
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedAppleDepSettingChanged(DepOnboardingSetting? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Apple DEP Setting";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedDeviceCategoryChanged(DeviceCategory? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Device Category";
        SelectedItemPlatform = "";
        SelectedItemRoleScopeTags = new ObservableCollection<string>(value?.RoleScopeTagIds ?? []);
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedVppTokenChanged(VppToken? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "VPP Token";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedCloudPcProvisioningPolicyChanged(CloudPcProvisioningPolicy? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Cloud PC Provisioning Policy";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedCloudPcUserSettingChanged(CloudPcUserSetting? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Cloud PC User Setting";
        SelectedItemPlatform = "";
        SelectedItemRestorePointFrequency = value?.RestorePointSetting?.FrequencyType?.ToString() ?? "";
        SelectedItemUserRestoreEnabled = value?.RestorePointSetting?.UserRestoreEnabled ?? false;
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedRoleAssignmentChanged(DeviceAndAppManagementRoleAssignment? value)
    {
        SelectedItemAssignments.Clear();
        SelectedItemTypeName = "Role Assignment";
        SelectedItemPlatform = "";
        OnPropertyChanged(nameof(CanRefreshSelectedItem));
    }

    partial void OnSelectedDynamicGroupRowChanged(GroupRow? value)

    {

        SelectedGroupMembers.Clear();

        if (value?.GroupId != null && !string.IsNullOrEmpty(value.GroupId))

            _ = LoadGroupMembersAsync(value.GroupId);

    }



    partial void OnSelectedAssignedGroupRowChanged(GroupRow? value)

    {

        SelectedGroupMembers.Clear();

        if (value?.GroupId != null && !string.IsNullOrEmpty(value.GroupId))

            _ = LoadGroupMembersAsync(value.GroupId);

    }



    private async Task LoadGroupMembersAsync(string groupId)

    {

        if (_groupService == null) return;

        IsLoadingGroupMembers = true;

        try

        {

            var members = await _groupService.ListGroupMembersAsync(groupId);

            var items = members.Select(m => new GroupMemberItem

            {

                MemberType = m.MemberType,

                DisplayName = m.DisplayName,

                SecondaryInfo = m.SecondaryInfo,

                TertiaryInfo = m.TertiaryInfo,

                Status = m.Status,

                Id = m.Id

            }).ToList();

            SelectedGroupMembers = new ObservableCollection<GroupMemberItem>(items);

            DebugLog.Log("Graph", $"Loaded {items.Count} member(s) for group {groupId}");

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load group members: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingGroupMembers = false; }

    }



    private async Task LoadConfigAssignmentsAsync(string configId)

    {

        if (_configProfileService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _configProfileService.GetAssignmentsAsync(configId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load configuration assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadCompliancePolicyAssignmentsAsync(string policyId)

    {

        if (_compliancePolicyService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _compliancePolicyService.GetAssignmentsAsync(policyId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load compliance policy assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadSettingsCatalogAssignmentsAsync(string policyId)

    {

        if (_settingsCatalogService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _settingsCatalogService.GetAssignmentsAsync(policyId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load settings catalog assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadApplicationAssignmentsAsync(string appId)

    {

        if (_applicationService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _applicationService.GetAssignmentsAsync(appId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

            {

                var item = await MapAssignmentAsync(a.Target);

                items.Add(new AssignmentDisplayItem

                {

                    Target = item.Target,

                    GroupId = item.GroupId,

                    TargetKind = item.TargetKind,

                    Intent = a.Intent?.ToString() ?? ""

                });

            }

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load application assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadEndpointSecurityAssignmentsAsync(string intentId)

    {

        if (_endpointSecurityService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _endpointSecurityService.GetAssignmentsAsync(intentId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load endpoint security assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadAdministrativeTemplateAssignmentsAsync(string templateId)

    {

        if (_administrativeTemplateService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _administrativeTemplateService.GetAssignmentsAsync(templateId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load administrative template assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadEnrollmentConfigurationAssignmentsAsync(string configurationId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceManagement.DeviceEnrollmentConfigurations[configurationId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedEnrollmentConfiguration?.Id == configurationId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load enrollment configuration assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadAppProtectionPolicyAssignmentsAsync(ManagedAppPolicy policy)

    {

        if (_graphClient == null || policy.Id == null) return;

        IsLoadingDetails = true;

        try

        {

            IEnumerable<TargetedManagedAppPolicyAssignment> assignments = [];

            var odataType = policy.OdataType ?? "";

            if (odataType.Contains("android", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _graphClient.DeviceAppManagement.AndroidManagedAppProtections[policy.Id]
                    .Assignments.GetAsync();
                assignments = response?.Value ?? [];
            }
            else if (odataType.Contains("ios", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _graphClient.DeviceAppManagement.IosManagedAppProtections[policy.Id]
                    .Assignments.GetAsync();
                assignments = response?.Value ?? [];
            }
            else if (odataType.Contains("windows", StringComparison.OrdinalIgnoreCase))
            {
                var response = await _graphClient.DeviceAppManagement.WindowsManagedAppProtections[policy.Id]
                    .Assignments.GetAsync();
                assignments = response?.Value ?? [];
            }

            var items = new List<AssignmentDisplayItem>();
            foreach (var a in assignments)
                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedAppProtectionPolicy?.Id == policy.Id)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load app protection policy assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadManagedDeviceAppConfigurationAssignmentsAsync(string configurationId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceAppManagement.MobileAppConfigurations[configurationId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedManagedDeviceAppConfiguration?.Id == configurationId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load managed device app configuration assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadTargetedManagedAppConfigurationAssignmentsAsync(string configurationId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceAppManagement.TargetedManagedAppConfigurations[configurationId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedTargetedManagedAppConfiguration?.Id == configurationId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load targeted managed app configuration assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadTermsAndConditionsAssignmentsAsync(string termsId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceManagement.TermsAndConditions[termsId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedTermsAndConditions?.Id == termsId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load terms and conditions assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadAutopilotProfileAssignmentsAsync(string profileId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[profileId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedAutopilotProfile?.Id == profileId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load autopilot profile assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadMacCustomAttributeAssignmentsAsync(string attributeId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceManagement.DeviceCustomAttributeShellScripts[attributeId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedMacCustomAttribute?.Id == attributeId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load mac custom attribute assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadFeatureUpdateProfileAssignmentsAsync(string profileId)

    {

        if (_graphClient == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _graphClient.DeviceManagement.WindowsFeatureUpdateProfiles[profileId]
                .Assignments.GetAsync();

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments?.Value ?? [])

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedFeatureUpdateProfile?.Id == profileId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load feature update profile assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadTermsOfUseAssignmentsAsync(string agreementId)

    {

        IsLoadingDetails = true;

        try

        {

            var items = await Task.Run(() =>
                ConditionalAccessPolicies
                    .Where(p => p.GrantControls?.TermsOfUse?.Contains(agreementId) == true)
                    .Select(p => new AssignmentDisplayItem
                    {
                        TargetKind = "CA Policy",
                        Target = p.DisplayName ?? p.Id ?? "Conditional Access Policy",
                        GroupId = p.Id ?? string.Empty
                    })
                    .ToList());

            if (SelectedTermsOfUseAgreement?.Id == agreementId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load terms of use references: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task<AssignmentDisplayItem> MapAssignmentAsync(DeviceAndAppManagementAssignmentTarget? target)

    {

        switch (target)

        {

            case AllDevicesAssignmentTarget:

                return new AssignmentDisplayItem { Target = "All Devices", TargetKind = "Include" };

            case AllLicensedUsersAssignmentTarget:

                return new AssignmentDisplayItem { Target = "All Users", TargetKind = "Include" };

            case ExclusionGroupAssignmentTarget excl:

                return new AssignmentDisplayItem

                {

                    Target = await ResolveGroupNameAsync(excl.GroupId),

                    GroupId = excl.GroupId ?? "",

                    TargetKind = "Exclude"

                };

            case GroupAssignmentTarget grp:

                return new AssignmentDisplayItem

                {

                    Target = await ResolveGroupNameAsync(grp.GroupId),

                    GroupId = grp.GroupId ?? "",

                    TargetKind = "Include"

                };

            default:

                return new AssignmentDisplayItem { Target = "Unknown", TargetKind = "Include" };

        }

    }



    private readonly Dictionary<string, string> _groupNameCache = new();



    private async Task<string> ResolveGroupNameAsync(string? groupId)

    {

        if (string.IsNullOrEmpty(groupId)) return "Unknown Group";

        if (!Guid.TryParse(groupId, out _)) return groupId; // Not a valid GUID — skip Graph call

        if (_groupNameCache.TryGetValue(groupId, out var cached)) return cached;



        try

        {

            if (_graphClient != null)

            {

                // Use $filter to avoid 404 ODataError for deleted/inaccessible groups

                var response = await _graphClient.Groups

                    .GetAsync(req =>

                    {

                        req.QueryParameters.Filter = $"id eq '{groupId}'";

                        req.QueryParameters.Select = new[] { "displayName" };

                        req.QueryParameters.Top = 1;

                    });

                var group = response?.Value?.FirstOrDefault();

                var name = group?.DisplayName ?? groupId;

                _groupNameCache[groupId] = name;

                return name;

            }

        }

        catch (Exception ex)

        {

            DebugLog.Log("Graph", $"Could not resolve group {groupId}: {FormatGraphError(ex)}");

        }



        _groupNameCache[groupId] = groupId;

        return groupId;

    }



    private async Task LoadDeviceManagementScriptAssignmentsAsync(string scriptId)

    {

        if (_deviceManagementScriptService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _deviceManagementScriptService.GetAssignmentsAsync(scriptId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedDeviceManagementScript?.Id == scriptId)

                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load device management script assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }



    private async Task LoadDeviceShellScriptAssignmentsAsync(string scriptId)

    {

        if (_deviceShellScriptService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _deviceShellScriptService.GetAssignmentsAsync(scriptId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)

                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedDeviceShellScript?.Id == scriptId)

                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load device shell script assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }

    private async Task LoadDeviceHealthScriptDetailsAsync(string scriptId)
    {
        if (_deviceHealthScriptService == null) return;

        IsLoadingDetails = true;
        try
        {
            var fullScript = await _deviceHealthScriptService.GetDeviceHealthScriptAsync(scriptId);

            // Guard: selection may have changed while loading
            if (SelectedDeviceHealthScript?.Id != scriptId) return;

            if (fullScript != null)
            {
                SelectedItemDetectionScript = fullScript.DetectionScriptContent != null
                    ? System.Text.Encoding.UTF8.GetString(fullScript.DetectionScriptContent)
                    : "(No detection script)";
                SelectedItemRemediationScript = fullScript.RemediationScriptContent != null
                    ? System.Text.Encoding.UTF8.GetString(fullScript.RemediationScriptContent)
                    : "(No remediation script)";
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load device health script details: {FormatGraphError(ex)}", ex);
            SelectedItemDetectionScript = "(Failed to load script)";
            SelectedItemRemediationScript = "(Failed to load script)";
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadDeviceManagementScriptDetailsAsync(string scriptId)
    {
        if (_deviceManagementScriptService == null) return;

        IsLoadingDetails = true;
        try
        {
            var fullScript = await _deviceManagementScriptService.GetDeviceManagementScriptAsync(scriptId);

            if (SelectedDeviceManagementScript?.Id != scriptId) return;

            if (fullScript != null)
            {
                SelectedItemScriptContent = fullScript.ScriptContent != null
                    ? System.Text.Encoding.UTF8.GetString(fullScript.ScriptContent)
                    : "(No script content)";
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load device management script details: {FormatGraphError(ex)}", ex);
            SelectedItemScriptContent = "(Failed to load script)";
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadDeviceShellScriptDetailsAsync(string scriptId)
    {
        if (_deviceShellScriptService == null) return;

        IsLoadingDetails = true;
        try
        {
            var fullScript = await _deviceShellScriptService.GetDeviceShellScriptAsync(scriptId);

            if (SelectedDeviceShellScript?.Id != scriptId) return;

            if (fullScript != null)
            {
                SelectedItemScriptContent = fullScript.ScriptContent != null
                    ? System.Text.Encoding.UTF8.GetString(fullScript.ScriptContent)
                    : "(No script content)";
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load device shell script details: {FormatGraphError(ex)}", ex);
            SelectedItemScriptContent = "(Failed to load script)";
        }
        finally { IsLoadingDetails = false; }
    }

    private async Task LoadDeviceHealthScriptAssignmentsAsync(string scriptId)

    {

        if (_deviceHealthScriptService == null) return;

        IsLoadingDetails = true;

        try

        {

            var assignments = await _deviceHealthScriptService.GetAssignmentsAsync(scriptId);

            var items = new List<AssignmentDisplayItem>();

            foreach (var a in assignments)
                items.Add(await MapAssignmentAsync(a.Target));

            if (SelectedDeviceHealthScript?.Id == scriptId)
                SelectedItemAssignments = new ObservableCollection<AssignmentDisplayItem>(items);

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load device health script assignments: {FormatGraphError(ex)}", ex);

        }

        finally { IsLoadingDetails = false; }

    }

    private async Task LoadSettingsCatalogSettingsAsync(string policyId)
    {
        if (_settingsCatalogService == null) return;

        IsLoadingDetails = true;
        try
        {
            var settings = await _settingsCatalogService.GetPolicySettingsAsync(policyId);
            var items = new List<Models.SettingItem>();

            // Metadata properties to exclude
            var excludedProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id", "@odata.type", "settingInstance@odata.type",
                "settingInstanceTemplateReference", "settingDefinitionId",
                "simpleSettingValue@odata.type", "choiceSettingValue@odata.type",
                "groupSettingValue", "children"
            };

            foreach (var setting in settings)
            {
                var defId = setting.SettingInstance?.SettingDefinitionId;
                var label = FormatCatalogSettingLabel(defId);

                // Serialize and extract values using JSON approach
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(setting, setting.GetType());
                    using var doc = System.Text.Json.JsonDocument.Parse(json);

                    // Walk the JSON to extract leaf values
                    FlattenCatalogSettingJson(doc.RootElement, label, items, excludedProps, 0);
                }
                catch
                {
                    // Fallback: use the SDK type extraction
                    items.Add(new Models.SettingItem(label, ExtractSettingInstanceValue(setting.SettingInstance)));
                }
            }

            // If flattening produced nothing meaningful, fall back
            if (items.Count == 0 && settings.Count > 0)
            {
                items.AddRange(settings.Select(s => new Models.SettingItem(
                    FormatCatalogSettingLabel(s.SettingInstance?.SettingDefinitionId),
                    ExtractSettingInstanceValue(s.SettingInstance))));
            }

            SelectedItemCatalogSettings = new ObservableCollection<Models.SettingItem>(
                items.OrderBy(s => s.Label));
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load settings catalog settings: {FormatGraphError(ex)}", ex);
        }
        finally { IsLoadingDetails = false; }
    }

    /// <summary>Extracts a human-readable label from a Settings Catalog setting definition ID.</summary>
    private static string FormatCatalogSettingLabel(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";

        // Strip known vendor path prefixes
        foreach (var prefix in new[] {
            "device_vendor_msft_policy_config_",
            "user_vendor_msft_policy_config_",
            "device_vendor_msft_",
            "user_vendor_msft_" })
        {
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                id = id[prefix.Length..];
                break;
            }
        }

        // Take meaningful segments (skip very long paths)
        var parts = id.Split('_');
        // Use last 2-3 segments for context, applying camelCase spacing
        var meaningfulParts = parts.Length > 3 ? parts[^3..] : parts;
        return string.Join(" \u203a ", meaningfulParts.Select(FormatPropertyName));
    }

    /// <summary>Recursively walks a Settings Catalog setting JSON tree, extracting leaf values as SettingItems.</summary>
    private static void FlattenCatalogSettingJson(
        System.Text.Json.JsonElement element, string parentLabel,
        List<Models.SettingItem> items, HashSet<string> excludedProps, int depth)
    {
        if (depth > 5) return; // safety cap

        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                // Look for "value" property (the actual setting value)
                if (element.TryGetProperty("value", out var valProp))
                {
                    var formatted = FormatCatalogValue(valProp);
                    if (!string.IsNullOrEmpty(formatted))
                        items.Add(new Models.SettingItem(parentLabel, formatted));
                    return;
                }

                // Look for children array (group settings)
                if (element.TryGetProperty("children", out var childrenProp) && childrenProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var child in childrenProp.EnumerateArray())
                    {
                        // Each child has settingDefinitionId and its own value
                        var childIdStr = child.TryGetProperty("settingDefinitionId", out var childId)
                            ? childId.GetString() : null;
                        var childLabel = FormatCatalogSettingLabel(childIdStr);
                        FlattenCatalogSettingJson(child, childLabel, items, excludedProps, depth + 1);
                    }
                    return;
                }

                // Walk into settingInstance if present
                if (element.TryGetProperty("settingInstance", out var instanceProp))
                {
                    FlattenCatalogSettingJson(instanceProp, parentLabel, items, excludedProps, depth + 1);
                    return;
                }

                // Walk into simpleSettingValue, choiceSettingValue etc.
                foreach (var prop in element.EnumerateObject())
                {
                    if (excludedProps.Contains(prop.Name)) continue;
                    if (prop.Name.EndsWith("Value") || prop.Name.EndsWith("CollectionValue"))
                    {
                        FlattenCatalogSettingJson(prop.Value, parentLabel, items, excludedProps, depth + 1);
                    }
                }
                break;

            case System.Text.Json.JsonValueKind.Array:
                var arrayVals = new List<string>();
                foreach (var arrItem in element.EnumerateArray())
                {
                    if (arrItem.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        FlattenCatalogSettingJson(arrItem, parentLabel, items, excludedProps, depth + 1);
                    }
                    else
                    {
                        arrayVals.Add(FormatCatalogValue(arrItem));
                    }
                }
                if (arrayVals.Count > 0)
                    items.Add(new Models.SettingItem(parentLabel, string.Join(", ", arrayVals)));
                break;
        }
    }

    /// <summary>Formats a leaf JSON value for human-readable display in Settings Catalog.</summary>
    private static string FormatCatalogValue(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.True => "Yes",
            System.Text.Json.JsonValueKind.False => "No",
            System.Text.Json.JsonValueKind.Null or System.Text.Json.JsonValueKind.Undefined => "Not Configured",
            System.Text.Json.JsonValueKind.Number => element.ToString(),
            System.Text.Json.JsonValueKind.String => FormatCatalogStringValue(element.GetString()),
            _ => element.ToString()
        };
    }

    /// <summary>Formats a string value from Settings Catalog, handling choice IDs and plain strings.</summary>
    private static string FormatCatalogStringValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "Not Configured";

        // Choice values look like "device_vendor_msft_policy_config_..._somechoice_0"
        // Take the last meaningful segment
        if (value.Contains("_vendor_msft_") || value.Contains("_config_"))
        {
            var lastSegment = value.Split('_').LastOrDefault() ?? value;
            return FormatPropertyName(lastSegment);
        }

        return value;
    }

    /// <summary>Extracts a display-friendly value from a polymorphic setting instance.</summary>
    private static string ExtractSettingInstanceValue(Microsoft.Graph.Beta.Models.DeviceManagementConfigurationSettingInstance? instance)
    {
        return instance switch
        {
            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationSimpleSettingInstance s =>
                ExtractSimpleValue(s.SimpleSettingValue),

            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationSimpleSettingCollectionInstance sc =>
                sc.SimpleSettingCollectionValue is { Count: > 0 } vals
                    ? string.Join(", ", vals.Select(ExtractSimpleValue))
                    : "(empty)",

            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationChoiceSettingInstance c =>
                // Choice values are full definition IDs; take the last underscore segment for brevity
                c.ChoiceSettingValue?.Value?.Split('_').LastOrDefault() ?? "",

            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationChoiceSettingCollectionInstance cc =>
                cc.ChoiceSettingCollectionValue is { Count: > 0 } cvals
                    ? string.Join(", ", cvals.Select(v => v.Value?.Split('_').LastOrDefault() ?? ""))
                    : "(empty)",

            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationGroupSettingInstance g =>
                $"[{g.GroupSettingValue?.Children?.Count ?? 0} child setting(s)]",

            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationGroupSettingCollectionInstance gc =>
                $"[{gc.GroupSettingCollectionValue?.Count ?? 0} group(s)]",

            _ => instance?.OdataType?.Split('.').LastOrDefault() ?? ""
        };
    }

    private static string ExtractSimpleValue(Microsoft.Graph.Beta.Models.DeviceManagementConfigurationSimpleSettingValue? v)
    {
        return v switch
        {
            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationStringSettingValue sv => sv.Value ?? "",
            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationIntegerSettingValue iv => iv.Value?.ToString() ?? "",
            Microsoft.Graph.Beta.Models.DeviceManagementConfigurationSecretSettingValue sec => $"[secret: {sec.ValueState}]",
            _ => v?.AdditionalData != null && v.AdditionalData.TryGetValue("value", out var raw) ? raw?.ToString() ?? "" : ""
        };
    }
}
