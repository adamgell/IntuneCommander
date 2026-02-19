using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Globalization;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.Input;

using IntuneManager.Core.Models;

using IntuneManager.Core.Services;

using Microsoft.Graph.Beta.Models;



namespace IntuneManager.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{



    // --- Conditional Access view ---



    private async Task LoadConditionalAccessPoliciesAsync()

    {

        if (_conditionalAccessPolicyService == null) return;



        IsBusy = true;

        StatusText = "Loading conditional access policies...";



        try

        {

            var policies = await _conditionalAccessPolicyService.ListPoliciesAsync();

            ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(policies);

            _conditionalAccessLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyConditionalAccess, policies);

                DebugLog.Log("Cache", $"Saved {policies.Count} conditional access policy(ies) to cache");

            }



            StatusText = $"Loaded {policies.Count} conditional access policy(ies)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load conditional access policies: {FormatGraphError(ex)}");

            StatusText = "Error loading conditional access policies";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Assignment Filters view ---



    private async Task LoadAssignmentFiltersAsync()

    {

        if (_assignmentFilterService == null) return;



        IsBusy = true;

        StatusText = "Loading assignment filters...";



        try

        {

            var filters = await _assignmentFilterService.ListFiltersAsync();

            AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(filters);

            _assignmentFiltersLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAssignmentFilters, filters);

                DebugLog.Log("Cache", $"Saved {filters.Count} assignment filter(s) to cache");

            }



            StatusText = $"Loaded {filters.Count} assignment filter(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load assignment filters: {FormatGraphError(ex)}");

            StatusText = "Error loading assignment filters";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Policy Sets view ---



    private async Task LoadPolicySetsAsync()

    {

        if (_policySetService == null) return;



        IsBusy = true;

        StatusText = "Loading policy sets...";



        try

        {

            var sets = await _policySetService.ListPolicySetsAsync();

            PolicySets = new ObservableCollection<PolicySet>(sets);

            _policySetsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyPolicySets, sets);

                DebugLog.Log("Cache", $"Saved {sets.Count} policy set(s) to cache");

            }



            StatusText = $"Loaded {sets.Count} policy set(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load policy sets: {FormatGraphError(ex)}");

            StatusText = "Error loading policy sets";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Endpoint Security view ---



    private async Task LoadEndpointSecurityIntentsAsync()

    {

        if (_endpointSecurityService == null) return;



        IsBusy = true;

        StatusText = "Loading endpoint security intents...";



        try

        {

            var intents = await _endpointSecurityService.ListEndpointSecurityIntentsAsync();

            EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(intents);

            _endpointSecurityLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyEndpointSecurity, intents);

                DebugLog.Log("Cache", $"Saved {intents.Count} endpoint security intent(s) to cache");

            }



            StatusText = $"Loaded {intents.Count} endpoint security intent(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load endpoint security intents: {FormatGraphError(ex)}");

            StatusText = "Error loading endpoint security intents";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Administrative Templates view ---



    private async Task LoadAdministrativeTemplatesAsync()

    {

        if (_administrativeTemplateService == null) return;



        IsBusy = true;

        StatusText = "Loading administrative templates...";



        try

        {

            var templates = await _administrativeTemplateService.ListAdministrativeTemplatesAsync();

            AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(templates);

            _administrativeTemplatesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAdministrativeTemplates, templates);

                DebugLog.Log("Cache", $"Saved {templates.Count} administrative template(s) to cache");

            }



            StatusText = $"Loaded {templates.Count} administrative template(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load administrative templates: {FormatGraphError(ex)}");

            StatusText = "Error loading administrative templates";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Enrollment Configurations view ---



    private async Task LoadEnrollmentConfigurationsAsync()

    {

        if (_enrollmentConfigurationService == null) return;



        IsBusy = true;

        StatusText = "Loading enrollment configurations...";



        try

        {

            var configurations = await _enrollmentConfigurationService.ListEnrollmentConfigurationsAsync();

            EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(configurations);

            _enrollmentConfigurationsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyEnrollmentConfigurations, configurations);

                DebugLog.Log("Cache", $"Saved {configurations.Count} enrollment configuration(s) to cache");

            }



            StatusText = $"Loaded {configurations.Count} enrollment configuration(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load enrollment configurations: {FormatGraphError(ex)}");

            StatusText = "Error loading enrollment configurations";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- App Protection Policies view ---



    private async Task LoadAppProtectionPoliciesAsync()

    {

        if (_appProtectionPolicyService == null) return;



        IsBusy = true;

        StatusText = "Loading app protection policies...";



        try

        {

            var policies = await _appProtectionPolicyService.ListAppProtectionPoliciesAsync();

            AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(policies);

            _appProtectionPoliciesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAppProtectionPolicies, policies);

                DebugLog.Log("Cache", $"Saved {policies.Count} app protection policy(ies) to cache");

            }



            StatusText = $"Loaded {policies.Count} app protection policy(ies)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load app protection policies: {FormatGraphError(ex)}");

            StatusText = "Error loading app protection policies";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Managed Device App Configurations view ---



    private async Task LoadManagedDeviceAppConfigurationsAsync()

    {

        if (_managedAppConfigurationService == null) return;



        IsBusy = true;

        StatusText = "Loading managed device app configurations...";



        try

        {

            var configurations = await _managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync();

            ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(configurations);

            _managedDeviceAppConfigurationsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyManagedDeviceAppConfigurations, configurations);

                DebugLog.Log("Cache", $"Saved {configurations.Count} managed device app configuration(s) to cache");

            }



            StatusText = $"Loaded {configurations.Count} managed device app configuration(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load managed device app configurations: {FormatGraphError(ex)}");

            StatusText = "Error loading managed device app configurations";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Targeted Managed App Configurations view ---



    private async Task LoadTargetedManagedAppConfigurationsAsync()

    {

        if (_managedAppConfigurationService == null) return;



        IsBusy = true;

        StatusText = "Loading targeted managed app configurations...";



        try

        {

            var configurations = await _managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync();

            TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(configurations);

            _targetedManagedAppConfigurationsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyTargetedManagedAppConfigurations, configurations);

                DebugLog.Log("Cache", $"Saved {configurations.Count} targeted managed app configuration(s) to cache");

            }



            StatusText = $"Loaded {configurations.Count} targeted managed app configuration(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load targeted managed app configurations: {FormatGraphError(ex)}");

            StatusText = "Error loading targeted managed app configurations";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Terms and Conditions view ---



    private async Task LoadTermsAndConditionsAsync()

    {

        if (_termsAndConditionsService == null) return;



        IsBusy = true;

        StatusText = "Loading terms and conditions...";



        try

        {

            var termsCollection = await _termsAndConditionsService.ListTermsAndConditionsAsync();

            TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);

            _termsAndConditionsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyTermsAndConditions, termsCollection);

                DebugLog.Log("Cache", $"Saved {termsCollection.Count} terms and conditions item(s) to cache");

            }



            StatusText = $"Loaded {termsCollection.Count} terms and conditions item(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load terms and conditions: {FormatGraphError(ex)}");

            StatusText = "Error loading terms and conditions";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Scope Tags view ---



    private async Task LoadScopeTagsAsync()

    {

        if (_scopeTagService == null) return;



        IsBusy = true;

        StatusText = "Loading scope tags...";



        try

        {

            var scopeTags = await _scopeTagService.ListScopeTagsAsync();

            ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);

            _scopeTagsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyScopeTags, scopeTags);

                DebugLog.Log("Cache", $"Saved {scopeTags.Count} scope tag(s) to cache");

            }



            StatusText = $"Loaded {scopeTags.Count} scope tag(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load scope tags: {FormatGraphError(ex)}");

            StatusText = "Error loading scope tags";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Role Definitions view ---



    private async Task LoadRoleDefinitionsAsync()

    {

        if (_roleDefinitionService == null) return;



        IsBusy = true;

        StatusText = "Loading role definitions...";



        try

        {

            var roleDefinitions = await _roleDefinitionService.ListRoleDefinitionsAsync();

            RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);

            _roleDefinitionsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyRoleDefinitions, roleDefinitions);

                DebugLog.Log("Cache", $"Saved {roleDefinitions.Count} role definition(s) to cache");

            }



            StatusText = $"Loaded {roleDefinitions.Count} role definition(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load role definitions: {FormatGraphError(ex)}");

            StatusText = "Error loading role definitions";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Intune Branding view ---



    private async Task LoadIntuneBrandingProfilesAsync()

    {

        if (_intuneBrandingService == null) return;



        IsBusy = true;

        StatusText = "Loading Intune branding profiles...";



        try

        {

            var profiles = await _intuneBrandingService.ListIntuneBrandingProfilesAsync();

            IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(profiles);

            _intuneBrandingProfilesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyIntuneBrandingProfiles, profiles);

                DebugLog.Log("Cache", $"Saved {profiles.Count} Intune branding profile(s) to cache");

            }



            StatusText = $"Loaded {profiles.Count} Intune branding profile(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load Intune branding profiles: {FormatGraphError(ex)}");

            StatusText = "Error loading Intune branding profiles";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Azure Branding view ---



    private async Task LoadAzureBrandingLocalizationsAsync()

    {

        if (_azureBrandingService == null) return;



        IsBusy = true;

        StatusText = "Loading Azure branding localizations...";



        try

        {

            var localizations = await _azureBrandingService.ListBrandingLocalizationsAsync();

            AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(localizations);

            _azureBrandingLocalizationsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAzureBrandingLocalizations, localizations);

                DebugLog.Log("Cache", $"Saved {localizations.Count} Azure branding localization(s) to cache");

            }



            StatusText = $"Loaded {localizations.Count} Azure branding localization(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load Azure branding localizations: {FormatGraphError(ex)}");

            StatusText = "Error loading Azure branding localizations";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Wave 4/5 views ---



    private async Task LoadAutopilotProfilesAsync()

    {

        if (_autopilotService == null) return;



        IsBusy = true;

        StatusText = "Loading autopilot profiles...";



        try

        {

            var profiles = await _autopilotService.ListAutopilotProfilesAsync();

            AutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(profiles);

            _autopilotProfilesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAutopilotProfiles, profiles);



            StatusText = $"Loaded {profiles.Count} autopilot profile(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load autopilot profiles: {FormatGraphError(ex)}");

            StatusText = "Error loading autopilot profiles";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadDeviceHealthScriptsAsync()

    {

        if (_deviceHealthScriptService == null) return;



        IsBusy = true;

        StatusText = "Loading device health scripts...";



        try

        {

            var scripts = await _deviceHealthScriptService.ListDeviceHealthScriptsAsync();

            DeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(scripts);

            _deviceHealthScriptsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyDeviceHealthScripts, scripts);



            StatusText = $"Loaded {scripts.Count} device health script(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load device health scripts: {FormatGraphError(ex)}");

            StatusText = "Error loading device health scripts";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadMacCustomAttributesAsync()

    {

        if (_macCustomAttributeService == null) return;



        IsBusy = true;

        StatusText = "Loading mac custom attributes...";



        try

        {

            var attributes = await _macCustomAttributeService.ListMacCustomAttributesAsync();

            MacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(attributes);

            _macCustomAttributesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyMacCustomAttributes, attributes);



            StatusText = $"Loaded {attributes.Count} mac custom attribute(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load mac custom attributes: {FormatGraphError(ex)}");

            StatusText = "Error loading mac custom attributes";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadFeatureUpdateProfilesAsync()

    {

        if (_featureUpdateProfileService == null) return;



        IsBusy = true;

        StatusText = "Loading feature update profiles...";



        try

        {

            var profiles = await _featureUpdateProfileService.ListFeatureUpdateProfilesAsync();

            FeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(profiles);

            _featureUpdateProfilesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyFeatureUpdateProfiles, profiles);



            StatusText = $"Loaded {profiles.Count} feature update profile(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load feature update profiles: {FormatGraphError(ex)}");

            StatusText = "Error loading feature update profiles";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadNamedLocationsAsync()

    {

        if (_namedLocationService == null) return;



        IsBusy = true;

        StatusText = "Loading named locations...";



        try

        {

            var locations = await _namedLocationService.ListNamedLocationsAsync();

            NamedLocations = new ObservableCollection<NamedLocation>(locations);

            _namedLocationsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyNamedLocations, locations);



            StatusText = $"Loaded {locations.Count} named location(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load named locations: {FormatGraphError(ex)}");

            StatusText = "Error loading named locations";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadAuthenticationStrengthPoliciesAsync()

    {

        if (_authenticationStrengthService == null) return;



        IsBusy = true;

        StatusText = "Loading authentication strengths...";



        try

        {

            var strengths = await _authenticationStrengthService.ListAuthenticationStrengthPoliciesAsync();

            AuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(strengths);

            _authenticationStrengthPoliciesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAuthenticationStrengths, strengths);



            StatusText = $"Loaded {strengths.Count} authentication strength policy(ies)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load authentication strengths: {FormatGraphError(ex)}");

            StatusText = "Error loading authentication strengths";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadAuthenticationContextsAsync()

    {

        if (_authenticationContextService == null) return;



        IsBusy = true;

        StatusText = "Loading authentication contexts...";



        try

        {

            var contexts = await _authenticationContextService.ListAuthenticationContextsAsync();

            AuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(contexts);

            _authenticationContextClassReferencesLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAuthenticationContexts, contexts);



            StatusText = $"Loaded {contexts.Count} authentication context(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load authentication contexts: {FormatGraphError(ex)}");

            StatusText = "Error loading authentication contexts";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private async Task LoadTermsOfUseAgreementsAsync()

    {

        if (_termsOfUseService == null) return;



        IsBusy = true;

        StatusText = "Loading terms of use agreements...";



        try

        {

            var agreements = await _termsOfUseService.ListTermsOfUseAgreementsAsync();

            TermsOfUseAgreements = new ObservableCollection<Agreement>(agreements);

            _termsOfUseAgreementsLoaded = true;

            ApplyFilter();



            if (ActiveProfile?.TenantId != null)

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyTermsOfUseAgreements, agreements);



            StatusText = $"Loaded {agreements.Count} terms of use agreement(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load terms of use agreements: {FormatGraphError(ex)}");

            StatusText = "Error loading terms of use agreements";

        }

        finally

        {

            IsBusy = false;

        }

    }



    private static GroupRow BuildGroupRow(Microsoft.Graph.Beta.Models.Group group, GroupMemberCounts counts)

    {

        return new GroupRow

        {

            GroupName = group.DisplayName ?? "",

            Description = group.Description ?? "",

            MembershipRule = group.MembershipRule ?? "",

            ProcessingState = group.MembershipRuleProcessingState ?? "",

            GroupType = GroupService.InferGroupType(group),

            TotalMembers = counts.Total.ToString(CultureInfo.InvariantCulture),

            Users = counts.Users.ToString(CultureInfo.InvariantCulture),

            Devices = counts.Devices.ToString(CultureInfo.InvariantCulture),

            NestedGroups = counts.NestedGroups.ToString(CultureInfo.InvariantCulture),

            SecurityEnabled = group.SecurityEnabled == true ? "Yes" : "No",

            MailEnabled = group.MailEnabled == true ? "Yes" : "No",

            CreatedDate = group.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",

            GroupId = group.Id ?? ""

        };

    }





    [RelayCommand]

    private async Task RefreshAsync(CancellationToken cancellationToken)

    {

        ClearError();

        IsBusy = true;

        DebugLog.Log("Graph", "Refreshing data from Graph API...");

        var errors = new List<string>();

        var loadConditionalAccess = IsConditionalAccessCategory;

        var loadAssignmentFilters = IsAssignmentFiltersCategory;

        var loadPolicySets = IsPolicySetsCategory;

        var loadEndpointSecurity = IsEndpointSecurityCategory;

        var loadAdministrativeTemplates = IsAdministrativeTemplatesCategory;

        var loadEnrollmentConfigurations = IsEnrollmentConfigurationsCategory;

        var loadAppProtectionPolicies = IsAppProtectionPoliciesCategory;

        var loadManagedDeviceAppConfigurations = IsManagedDeviceAppConfigurationsCategory;

        var loadTargetedManagedAppConfigurations = IsTargetedManagedAppConfigurationsCategory;

        var loadTermsAndConditions = IsTermsAndConditionsCategory;

        var loadScopeTags = IsScopeTagsCategory;

        var loadRoleDefinitions = IsRoleDefinitionsCategory;

        var loadIntuneBranding = IsIntuneBrandingCategory;

        var loadAzureBranding = IsAzureBrandingCategory;

        var loadAutopilotProfiles = IsAutopilotProfilesCategory;

        var loadDeviceHealthScripts = IsDeviceHealthScriptsCategory;

        var loadMacCustomAttributes = IsMacCustomAttributesCategory;

        var loadFeatureUpdates = IsFeatureUpdatesCategory;

        var loadNamedLocations = IsNamedLocationsCategory;

        var loadAuthenticationStrengths = IsAuthenticationStrengthsCategory;

        var loadAuthenticationContexts = IsAuthenticationContextsCategory;

        var loadTermsOfUse = IsTermsOfUseCategory;



        try

        {

            if (_configProfileService != null)

            {

                try

                {

                    StatusText = "Loading device configurations...";

                    var configs = await _configProfileService.ListDeviceConfigurationsAsync(cancellationToken);

                    DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);

                    DebugLog.Log("Graph", $"Loaded {configs.Count} device configuration(s)");

                }

                catch (Exception ex)

                {

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load device configurations: {detail}", ex);

                    errors.Add($"Device Configs: {detail}");

                }

            }



            if (_compliancePolicyService != null)

            {

                try

                {

                    StatusText = "Loading compliance policies...";

                    var policies = await _compliancePolicyService.ListCompliancePoliciesAsync(cancellationToken);

                    CompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(policies);

                    DebugLog.Log("Graph", $"Loaded {policies.Count} compliance policy(ies)");

                }

                catch (Exception ex)

                {

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load compliance policies: {detail}", ex);

                    errors.Add($"Compliance Policies: {detail}");

                }

            }



            if (_applicationService != null)

            {

                try

                {

                    StatusText = "Loading applications...";

                    var apps = await _applicationService.ListApplicationsAsync(cancellationToken);

                    Applications = new ObservableCollection<MobileApp>(apps);

                    DebugLog.Log("Graph", $"Loaded {apps.Count} application(s)");

                }

                catch (Exception ex)

                {

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load applications: {detail}", ex);

                    errors.Add($"Applications: {detail}");

                }

            }



            if (_settingsCatalogService != null)

            {

                try

                {

                    StatusText = "Loading settings catalog policies...";

                    var settingsPolicies = await _settingsCatalogService.ListSettingsCatalogPoliciesAsync(cancellationToken);

                    SettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(settingsPolicies);

                    DebugLog.Log("Graph", $"Loaded {settingsPolicies.Count} settings catalog policy(ies)");

                }

                catch (Exception ex)

                {

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load settings catalog: {detail}", ex);

                    errors.Add($"Settings Catalog: {detail}");

                }

            }



            if (_conditionalAccessPolicyService != null && loadConditionalAccess)

            {

                try

                {

                    StatusText = "Loading conditional access policies...";

                    var policies = await _conditionalAccessPolicyService.ListPoliciesAsync(cancellationToken);

                    ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(policies);

                    _conditionalAccessLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {policies.Count} conditional access policy(ies)");

                }

                catch (Exception ex)

                {

                    _conditionalAccessLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load conditional access policies: {detail}", ex);

                    errors.Add($"Conditional Access: {detail}");

                }

            }

            else if (_conditionalAccessPolicyService != null)

            {

                DebugLog.Log("Graph", "Skipping conditional access refresh (lazy-load when tab selected)");

            }



            if (_endpointSecurityService != null && loadEndpointSecurity)

            {

                try

                {

                    StatusText = "Loading endpoint security intents...";

                    var intents = await _endpointSecurityService.ListEndpointSecurityIntentsAsync(cancellationToken);

                    EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(intents);

                    _endpointSecurityLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {intents.Count} endpoint security intent(s)");

                }

                catch (Exception ex)

                {

                    _endpointSecurityLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load endpoint security intents: {detail}", ex);

                    errors.Add($"Endpoint Security: {detail}");

                }

            }

            else if (_endpointSecurityService != null)

            {

                DebugLog.Log("Graph", "Skipping endpoint security refresh (lazy-load when tab selected)");

            }



            if (_administrativeTemplateService != null && loadAdministrativeTemplates)

            {

                try

                {

                    StatusText = "Loading administrative templates...";

                    var templates = await _administrativeTemplateService.ListAdministrativeTemplatesAsync(cancellationToken);

                    AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(templates);

                    _administrativeTemplatesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {templates.Count} administrative template(s)");

                }

                catch (Exception ex)

                {

                    _administrativeTemplatesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load administrative templates: {detail}", ex);

                    errors.Add($"Administrative Templates: {detail}");

                }

            }

            else if (_administrativeTemplateService != null)

            {

                DebugLog.Log("Graph", "Skipping administrative templates refresh (lazy-load when tab selected)");

            }



            if (_enrollmentConfigurationService != null && loadEnrollmentConfigurations)

            {

                try

                {

                    StatusText = "Loading enrollment configurations...";

                    var configurations = await _enrollmentConfigurationService.ListEnrollmentConfigurationsAsync(cancellationToken);

                    EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(configurations);

                    _enrollmentConfigurationsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {configurations.Count} enrollment configuration(s)");

                }

                catch (Exception ex)

                {

                    _enrollmentConfigurationsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load enrollment configurations: {detail}", ex);

                    errors.Add($"Enrollment Configurations: {detail}");

                }

            }

            else if (_enrollmentConfigurationService != null)

            {

                DebugLog.Log("Graph", "Skipping enrollment configurations refresh (lazy-load when tab selected)");

            }



            if (_appProtectionPolicyService != null && loadAppProtectionPolicies)

            {

                try

                {

                    StatusText = "Loading app protection policies...";

                    var policies = await _appProtectionPolicyService.ListAppProtectionPoliciesAsync(cancellationToken);

                    AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(policies);

                    _appProtectionPoliciesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {policies.Count} app protection policy(ies)");

                }

                catch (Exception ex)

                {

                    _appProtectionPoliciesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load app protection policies: {detail}", ex);

                    errors.Add($"App Protection Policies: {detail}");

                }

            }

            else if (_appProtectionPolicyService != null)

            {

                DebugLog.Log("Graph", "Skipping app protection policies refresh (lazy-load when tab selected)");

            }



            if (_managedAppConfigurationService != null && loadManagedDeviceAppConfigurations)

            {

                try

                {

                    StatusText = "Loading managed device app configurations...";

                    var configurations = await _managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync(cancellationToken);

                    ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(configurations);

                    _managedDeviceAppConfigurationsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {configurations.Count} managed device app configuration(s)");

                }

                catch (Exception ex)

                {

                    _managedDeviceAppConfigurationsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load managed device app configurations: {detail}", ex);

                    errors.Add($"Managed Device App Configurations: {detail}");

                }

            }

            else if (_managedAppConfigurationService != null)

            {

                DebugLog.Log("Graph", "Skipping managed device app configurations refresh (lazy-load when tab selected)");

            }



            if (_managedAppConfigurationService != null && loadTargetedManagedAppConfigurations)

            {

                try

                {

                    StatusText = "Loading targeted managed app configurations...";

                    var configurations = await _managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync(cancellationToken);

                    TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(configurations);

                    _targetedManagedAppConfigurationsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {configurations.Count} targeted managed app configuration(s)");

                }

                catch (Exception ex)

                {

                    _targetedManagedAppConfigurationsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load targeted managed app configurations: {detail}", ex);

                    errors.Add($"Targeted Managed App Configurations: {detail}");

                }

            }

            else if (_managedAppConfigurationService != null)

            {

                DebugLog.Log("Graph", "Skipping targeted managed app configurations refresh (lazy-load when tab selected)");

            }



            if (_termsAndConditionsService != null && loadTermsAndConditions)

            {

                try

                {

                    StatusText = "Loading terms and conditions...";

                    var termsCollection = await _termsAndConditionsService.ListTermsAndConditionsAsync(cancellationToken);

                    TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);

                    _termsAndConditionsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {termsCollection.Count} terms and conditions item(s)");

                }

                catch (Exception ex)

                {

                    _termsAndConditionsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load terms and conditions: {detail}", ex);

                    errors.Add($"Terms and Conditions: {detail}");

                }

            }

            else if (_termsAndConditionsService != null)

            {

                DebugLog.Log("Graph", "Skipping terms and conditions refresh (lazy-load when tab selected)");

            }



            if (_scopeTagService != null && loadScopeTags)

            {

                try

                {

                    StatusText = "Loading scope tags...";

                    var scopeTags = await _scopeTagService.ListScopeTagsAsync(cancellationToken);

                    ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);

                    _scopeTagsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {scopeTags.Count} scope tag(s)");

                }

                catch (Exception ex)

                {

                    _scopeTagsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load scope tags: {detail}", ex);

                    errors.Add($"Scope Tags: {detail}");

                }

            }

            else if (_scopeTagService != null)

            {

                DebugLog.Log("Graph", "Skipping scope tags refresh (lazy-load when tab selected)");

            }



            if (_roleDefinitionService != null && loadRoleDefinitions)

            {

                try

                {

                    StatusText = "Loading role definitions...";

                    var roleDefinitions = await _roleDefinitionService.ListRoleDefinitionsAsync(cancellationToken);

                    RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);

                    _roleDefinitionsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {roleDefinitions.Count} role definition(s)");

                }

                catch (Exception ex)

                {

                    _roleDefinitionsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load role definitions: {detail}", ex);

                    errors.Add($"Role Definitions: {detail}");

                }

            }

            else if (_roleDefinitionService != null)

            {

                DebugLog.Log("Graph", "Skipping role definitions refresh (lazy-load when tab selected)");

            }



            if (_intuneBrandingService != null && loadIntuneBranding)

            {

                try

                {

                    StatusText = "Loading Intune branding profiles...";

                    var profiles = await _intuneBrandingService.ListIntuneBrandingProfilesAsync(cancellationToken);

                    IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(profiles);

                    _intuneBrandingProfilesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {profiles.Count} Intune branding profile(s)");

                }

                catch (Exception ex)

                {

                    _intuneBrandingProfilesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load Intune branding profiles: {detail}", ex);

                    errors.Add($"Intune Branding: {detail}");

                }

            }

            else if (_intuneBrandingService != null)

            {

                DebugLog.Log("Graph", "Skipping Intune branding refresh (lazy-load when tab selected)");

            }



            if (_azureBrandingService != null && loadAzureBranding)

            {

                try

                {

                    StatusText = "Loading Azure branding localizations...";

                    var localizations = await _azureBrandingService.ListBrandingLocalizationsAsync(cancellationToken);

                    AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(localizations);

                    _azureBrandingLocalizationsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {localizations.Count} Azure branding localization(s)");

                }

                catch (Exception ex)

                {

                    _azureBrandingLocalizationsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load Azure branding localizations: {detail}", ex);

                    errors.Add($"Azure Branding: {detail}");

                }

            }

            else if (_azureBrandingService != null)

            {

                DebugLog.Log("Graph", "Skipping Azure branding refresh (lazy-load when tab selected)");

            }



            if (_assignmentFilterService != null && loadAssignmentFilters)

            {

                try

                {

                    StatusText = "Loading assignment filters...";

                    var filters = await _assignmentFilterService.ListFiltersAsync(cancellationToken);

                    AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(filters);

                    _assignmentFiltersLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {filters.Count} assignment filter(s)");

                }

                catch (Exception ex)

                {

                    _assignmentFiltersLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load assignment filters: {detail}", ex);

                    errors.Add($"Assignment Filters: {detail}");

                }

            }

            else if (_assignmentFilterService != null)

            {

                DebugLog.Log("Graph", "Skipping assignment filter refresh (lazy-load when tab selected)");

            }



            if (_policySetService != null && loadPolicySets)

            {

                try

                {

                    StatusText = "Loading policy sets...";

                    var sets = await _policySetService.ListPolicySetsAsync(cancellationToken);

                    PolicySets = new ObservableCollection<PolicySet>(sets);

                    _policySetsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {sets.Count} policy set(s)");

                }

                catch (Exception ex)

                {

                    _policySetsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load policy sets: {detail}", ex);

                    errors.Add($"Policy Sets: {detail}");

                }

            }

            else if (_policySetService != null)

            {

                DebugLog.Log("Graph", "Skipping policy sets refresh (lazy-load when tab selected)");

            }



            if (_autopilotService != null && loadAutopilotProfiles)

            {

                try

                {

                    StatusText = "Loading autopilot profiles...";

                    var profiles = await _autopilotService.ListAutopilotProfilesAsync(cancellationToken);

                    AutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(profiles);

                    _autopilotProfilesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {profiles.Count} autopilot profile(s)");

                }

                catch (Exception ex)

                {

                    _autopilotProfilesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load autopilot profiles: {detail}", ex);

                    errors.Add($"Autopilot Profiles: {detail}");

                }

            }



            if (_deviceHealthScriptService != null && loadDeviceHealthScripts)

            {

                try

                {

                    StatusText = "Loading device health scripts...";

                    var scripts = await _deviceHealthScriptService.ListDeviceHealthScriptsAsync(cancellationToken);

                    DeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(scripts);

                    _deviceHealthScriptsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {scripts.Count} device health script(s)");

                }

                catch (Exception ex)

                {

                    _deviceHealthScriptsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load device health scripts: {detail}", ex);

                    errors.Add($"Device Health Scripts: {detail}");

                }

            }



            if (_macCustomAttributeService != null && loadMacCustomAttributes)

            {

                try

                {

                    StatusText = "Loading mac custom attributes...";

                    var attributes = await _macCustomAttributeService.ListMacCustomAttributesAsync(cancellationToken);

                    MacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(attributes);

                    _macCustomAttributesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {attributes.Count} mac custom attribute(s)");

                }

                catch (Exception ex)

                {

                    _macCustomAttributesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load mac custom attributes: {detail}", ex);

                    errors.Add($"Mac Custom Attributes: {detail}");

                }

            }



            if (_featureUpdateProfileService != null && loadFeatureUpdates)

            {

                try

                {

                    StatusText = "Loading feature update profiles...";

                    var profiles = await _featureUpdateProfileService.ListFeatureUpdateProfilesAsync(cancellationToken);

                    FeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(profiles);

                    _featureUpdateProfilesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {profiles.Count} feature update profile(s)");

                }

                catch (Exception ex)

                {

                    _featureUpdateProfilesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load feature update profiles: {detail}", ex);

                    errors.Add($"Feature Updates: {detail}");

                }

            }



            if (_namedLocationService != null && loadNamedLocations)

            {

                try

                {

                    StatusText = "Loading named locations...";

                    var locations = await _namedLocationService.ListNamedLocationsAsync(cancellationToken);

                    NamedLocations = new ObservableCollection<NamedLocation>(locations);

                    _namedLocationsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {locations.Count} named location(s)");

                }

                catch (Exception ex)

                {

                    _namedLocationsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load named locations: {detail}", ex);

                    errors.Add($"Named Locations: {detail}");

                }

            }



            if (_authenticationStrengthService != null && loadAuthenticationStrengths)

            {

                try

                {

                    StatusText = "Loading authentication strengths...";

                    var strengths = await _authenticationStrengthService.ListAuthenticationStrengthPoliciesAsync(cancellationToken);

                    AuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(strengths);

                    _authenticationStrengthPoliciesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {strengths.Count} authentication strength policy(ies)");

                }

                catch (Exception ex)

                {

                    _authenticationStrengthPoliciesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load authentication strengths: {detail}", ex);

                    errors.Add($"Authentication Strengths: {detail}");

                }

            }



            if (_authenticationContextService != null && loadAuthenticationContexts)

            {

                try

                {

                    StatusText = "Loading authentication contexts...";

                    var contexts = await _authenticationContextService.ListAuthenticationContextsAsync(cancellationToken);

                    AuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(contexts);

                    _authenticationContextClassReferencesLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {contexts.Count} authentication context(s)");

                }

                catch (Exception ex)

                {

                    _authenticationContextClassReferencesLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load authentication contexts: {detail}", ex);

                    errors.Add($"Authentication Contexts: {detail}");

                }

            }



            if (_termsOfUseService != null && loadTermsOfUse)

            {

                try

                {

                    StatusText = "Loading terms of use agreements...";

                    var agreements = await _termsOfUseService.ListTermsOfUseAgreementsAsync(cancellationToken);

                    TermsOfUseAgreements = new ObservableCollection<Agreement>(agreements);

                    _termsOfUseAgreementsLoaded = true;

                    DebugLog.Log("Graph", $"Loaded {agreements.Count} terms of use agreement(s)");

                }

                catch (Exception ex)

                {

                    _termsOfUseAgreementsLoaded = false;

                    var detail = FormatGraphError(ex);

                    DebugLog.LogError($"Failed to load terms of use agreements: {detail}", ex);

                    errors.Add($"Terms Of Use: {detail}");

                }

            }



            var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count + AutopilotProfiles.Count + DeviceHealthScripts.Count + MacCustomAttributes.Count + FeatureUpdateProfiles.Count + NamedLocations.Count + AuthenticationStrengthPolicies.Count + AuthenticationContextClassReferences.Count + TermsOfUseAgreements.Count;

            StatusText = $"Loaded {totalItems} item(s) ({DeviceConfigurations.Count} configs, {CompliancePolicies.Count} compliance, {Applications.Count} apps, {SettingsCatalogPolicies.Count} settings catalog, {EndpointSecurityIntents.Count} endpoint security, {AdministrativeTemplates.Count} admin templates, {EnrollmentConfigurations.Count} enrollment configs, {AppProtectionPolicies.Count} app protection, {ManagedDeviceAppConfigurations.Count} managed device app configs, {TargetedManagedAppConfigurations.Count} targeted app configs, {TermsAndConditionsCollection.Count} terms, {ScopeTags.Count} scope tags, {RoleDefinitions.Count} role definitions, {IntuneBrandingProfiles.Count} intune branding, {AzureBrandingLocalizations.Count} azure branding, {ConditionalAccessPolicies.Count} conditional access, {AssignmentFilters.Count} filters, {PolicySets.Count} policy sets, {AutopilotProfiles.Count} autopilot, {DeviceHealthScripts.Count} device health scripts, {MacCustomAttributes.Count} mac custom attributes, {FeatureUpdateProfiles.Count} feature updates, {NamedLocations.Count} named locations, {AuthenticationStrengthPolicies.Count} auth strengths, {AuthenticationContextClassReferences.Count} auth contexts, {TermsOfUseAgreements.Count} terms of use)";



            if (errors.Count > 0)

                SetError($"Some data failed to load — {string.Join("; ", errors)}");



            // Save successful loads to cache

            if (ActiveProfile?.TenantId != null)

                SaveToCache(ActiveProfile.TenantId);



            ApplyFilter();



            // Reset lazy-load state; actual loading is triggered when navigating to those tabs

            _appAssignmentsLoaded = false;

            _dynamicGroupsLoaded = false;

            _assignedGroupsLoaded = false;



            // Invalidate lazy-load caches so they reload from Graph on next tab visit

            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyAppAssignments);

                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyDynamicGroups);

                _cacheService.Invalidate(ActiveProfile.TenantId, CacheKeyAssignedGroups);

            }

        }

        catch (Exception ex)

        {

            SetError($"Failed to load data: {FormatGraphError(ex)}");

            StatusText = "Error loading data";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Cache helpers ---



    /// <summary>

    /// Attempts to populate all collections from cached data.

    /// Returns how many data types were loaded.

    /// </summary>

    private int TryLoadFromCache(string tenantId)

    {

        if (string.IsNullOrEmpty(tenantId)) return 0;



        var typesLoaded = 0;

        DateTime? oldestCacheTime = null;



        try

        {

            var configs = _cacheService.Get<DeviceConfiguration>(tenantId, CacheKeyDeviceConfigs);

            if (configs != null)

            {

                DeviceConfigurations = new ObservableCollection<DeviceConfiguration>(configs);

                DebugLog.Log("Cache", $"Loaded {configs.Count} device configuration(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyDeviceConfigs);

            }



            var policies = _cacheService.Get<DeviceCompliancePolicy>(tenantId, CacheKeyCompliancePolicies);

            if (policies != null)

            {

                CompliancePolicies = new ObservableCollection<DeviceCompliancePolicy>(policies);

                DebugLog.Log("Cache", $"Loaded {policies.Count} compliance policy(ies) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyCompliancePolicies);

            }



            var apps = _cacheService.Get<MobileApp>(tenantId, CacheKeyApplications);

            if (apps != null)

            {

                Applications = new ObservableCollection<MobileApp>(apps);

                DebugLog.Log("Cache", $"Loaded {apps.Count} application(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyApplications);

            }



            var settingsPolicies = _cacheService.Get<DeviceManagementConfigurationPolicy>(tenantId, CacheKeySettingsCatalog);

            if (settingsPolicies != null)

            {

                SettingsCatalogPolicies = new ObservableCollection<DeviceManagementConfigurationPolicy>(settingsPolicies);

                DebugLog.Log("Cache", $"Loaded {settingsPolicies.Count} settings catalog policy(ies) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeySettingsCatalog);

            }



            var endpointSecurityIntents = _cacheService.Get<DeviceManagementIntent>(tenantId, CacheKeyEndpointSecurity);

            if (endpointSecurityIntents != null)

            {

                EndpointSecurityIntents = new ObservableCollection<DeviceManagementIntent>(endpointSecurityIntents);

                _endpointSecurityLoaded = true;

                DebugLog.Log("Cache", $"Loaded {endpointSecurityIntents.Count} endpoint security intent(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyEndpointSecurity);

            }



            var administrativeTemplates = _cacheService.Get<GroupPolicyConfiguration>(tenantId, CacheKeyAdministrativeTemplates);

            if (administrativeTemplates != null)

            {

                AdministrativeTemplates = new ObservableCollection<GroupPolicyConfiguration>(administrativeTemplates);

                _administrativeTemplatesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {administrativeTemplates.Count} administrative template(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAdministrativeTemplates);

            }



            var enrollmentConfigurations = _cacheService.Get<DeviceEnrollmentConfiguration>(tenantId, CacheKeyEnrollmentConfigurations);

            if (enrollmentConfigurations != null)

            {

                EnrollmentConfigurations = new ObservableCollection<DeviceEnrollmentConfiguration>(enrollmentConfigurations);

                _enrollmentConfigurationsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {enrollmentConfigurations.Count} enrollment configuration(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyEnrollmentConfigurations);

            }



            var appProtectionPolicies = _cacheService.Get<ManagedAppPolicy>(tenantId, CacheKeyAppProtectionPolicies);

            if (appProtectionPolicies != null)

            {

                AppProtectionPolicies = new ObservableCollection<ManagedAppPolicy>(appProtectionPolicies);

                _appProtectionPoliciesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {appProtectionPolicies.Count} app protection policy(ies) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAppProtectionPolicies);

            }



            var managedDeviceAppConfigurations = _cacheService.Get<ManagedDeviceMobileAppConfiguration>(tenantId, CacheKeyManagedDeviceAppConfigurations);

            if (managedDeviceAppConfigurations != null)

            {

                ManagedDeviceAppConfigurations = new ObservableCollection<ManagedDeviceMobileAppConfiguration>(managedDeviceAppConfigurations);

                _managedDeviceAppConfigurationsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {managedDeviceAppConfigurations.Count} managed device app configuration(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyManagedDeviceAppConfigurations);

            }



            var targetedManagedAppConfigurations = _cacheService.Get<TargetedManagedAppConfiguration>(tenantId, CacheKeyTargetedManagedAppConfigurations);

            if (targetedManagedAppConfigurations != null)

            {

                TargetedManagedAppConfigurations = new ObservableCollection<TargetedManagedAppConfiguration>(targetedManagedAppConfigurations);

                _targetedManagedAppConfigurationsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {targetedManagedAppConfigurations.Count} targeted managed app configuration(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyTargetedManagedAppConfigurations);

            }



            var termsCollection = _cacheService.Get<TermsAndConditions>(tenantId, CacheKeyTermsAndConditions);

            if (termsCollection != null)

            {

                TermsAndConditionsCollection = new ObservableCollection<TermsAndConditions>(termsCollection);

                _termsAndConditionsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {termsCollection.Count} terms and conditions item(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyTermsAndConditions);

            }



            var scopeTags = _cacheService.Get<RoleScopeTag>(tenantId, CacheKeyScopeTags);

            if (scopeTags != null)

            {

                ScopeTags = new ObservableCollection<RoleScopeTag>(scopeTags);

                _scopeTagsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {scopeTags.Count} scope tag(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyScopeTags);

            }



            var roleDefinitions = _cacheService.Get<RoleDefinition>(tenantId, CacheKeyRoleDefinitions);

            if (roleDefinitions != null)

            {

                RoleDefinitions = new ObservableCollection<RoleDefinition>(roleDefinitions);

                _roleDefinitionsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {roleDefinitions.Count} role definition(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyRoleDefinitions);

            }



            var intuneBrandingProfiles = _cacheService.Get<IntuneBrandingProfile>(tenantId, CacheKeyIntuneBrandingProfiles);

            if (intuneBrandingProfiles != null)

            {

                IntuneBrandingProfiles = new ObservableCollection<IntuneBrandingProfile>(intuneBrandingProfiles);

                _intuneBrandingProfilesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {intuneBrandingProfiles.Count} Intune branding profile(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyIntuneBrandingProfiles);

            }



            var azureBrandingLocalizations = _cacheService.Get<OrganizationalBrandingLocalization>(tenantId, CacheKeyAzureBrandingLocalizations);

            if (azureBrandingLocalizations != null)

            {

                AzureBrandingLocalizations = new ObservableCollection<OrganizationalBrandingLocalization>(azureBrandingLocalizations);

                _azureBrandingLocalizationsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {azureBrandingLocalizations.Count} Azure branding localization(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAzureBrandingLocalizations);

            }



            var conditionalAccessPolicies = _cacheService.Get<ConditionalAccessPolicy>(tenantId, CacheKeyConditionalAccess);

            if (conditionalAccessPolicies != null)

            {

                ConditionalAccessPolicies = new ObservableCollection<ConditionalAccessPolicy>(conditionalAccessPolicies);

                _conditionalAccessLoaded = true;

                DebugLog.Log("Cache", $"Loaded {conditionalAccessPolicies.Count} conditional access policy(ies) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyConditionalAccess);

            }



            var assignmentFilters = _cacheService.Get<DeviceAndAppManagementAssignmentFilter>(tenantId, CacheKeyAssignmentFilters);

            if (assignmentFilters != null)

            {

                AssignmentFilters = new ObservableCollection<DeviceAndAppManagementAssignmentFilter>(assignmentFilters);

                _assignmentFiltersLoaded = true;

                DebugLog.Log("Cache", $"Loaded {assignmentFilters.Count} assignment filter(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAssignmentFilters);

            }



            var policySets = _cacheService.Get<PolicySet>(tenantId, CacheKeyPolicySets);

            if (policySets != null)

            {

                PolicySets = new ObservableCollection<PolicySet>(policySets);

                _policySetsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {policySets.Count} policy set(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyPolicySets);

            }



            var autopilotProfiles = _cacheService.Get<WindowsAutopilotDeploymentProfile>(tenantId, CacheKeyAutopilotProfiles);

            if (autopilotProfiles != null)

            {

                AutopilotProfiles = new ObservableCollection<WindowsAutopilotDeploymentProfile>(autopilotProfiles);

                _autopilotProfilesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {autopilotProfiles.Count} autopilot profile(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAutopilotProfiles);

            }



            var deviceHealthScripts = _cacheService.Get<DeviceHealthScript>(tenantId, CacheKeyDeviceHealthScripts);

            if (deviceHealthScripts != null)

            {

                DeviceHealthScripts = new ObservableCollection<DeviceHealthScript>(deviceHealthScripts);

                _deviceHealthScriptsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {deviceHealthScripts.Count} device health script(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyDeviceHealthScripts);

            }



            var macCustomAttributes = _cacheService.Get<DeviceCustomAttributeShellScript>(tenantId, CacheKeyMacCustomAttributes);

            if (macCustomAttributes != null)

            {

                MacCustomAttributes = new ObservableCollection<DeviceCustomAttributeShellScript>(macCustomAttributes);

                _macCustomAttributesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {macCustomAttributes.Count} mac custom attribute(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyMacCustomAttributes);

            }



            var featureUpdates = _cacheService.Get<WindowsFeatureUpdateProfile>(tenantId, CacheKeyFeatureUpdateProfiles);

            if (featureUpdates != null)

            {

                FeatureUpdateProfiles = new ObservableCollection<WindowsFeatureUpdateProfile>(featureUpdates);

                _featureUpdateProfilesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {featureUpdates.Count} feature update profile(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyFeatureUpdateProfiles);

            }



            var namedLocations = _cacheService.Get<NamedLocation>(tenantId, CacheKeyNamedLocations);

            if (namedLocations != null)

            {

                NamedLocations = new ObservableCollection<NamedLocation>(namedLocations);

                _namedLocationsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {namedLocations.Count} named location(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyNamedLocations);

            }



            var authenticationStrengths = _cacheService.Get<AuthenticationStrengthPolicy>(tenantId, CacheKeyAuthenticationStrengths);

            if (authenticationStrengths != null)

            {

                AuthenticationStrengthPolicies = new ObservableCollection<AuthenticationStrengthPolicy>(authenticationStrengths);

                _authenticationStrengthPoliciesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {authenticationStrengths.Count} authentication strength policy(ies) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAuthenticationStrengths);

            }



            var authenticationContexts = _cacheService.Get<AuthenticationContextClassReference>(tenantId, CacheKeyAuthenticationContexts);

            if (authenticationContexts != null)

            {

                AuthenticationContextClassReferences = new ObservableCollection<AuthenticationContextClassReference>(authenticationContexts);

                _authenticationContextClassReferencesLoaded = true;

                DebugLog.Log("Cache", $"Loaded {authenticationContexts.Count} authentication context(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyAuthenticationContexts);

            }



            var termsOfUse = _cacheService.Get<Agreement>(tenantId, CacheKeyTermsOfUseAgreements);

            if (termsOfUse != null)

            {

                TermsOfUseAgreements = new ObservableCollection<Agreement>(termsOfUse);

                _termsOfUseAgreementsLoaded = true;

                DebugLog.Log("Cache", $"Loaded {termsOfUse.Count} terms of use agreement(s) from cache");

                typesLoaded++;

                UpdateOldestCacheTime(ref oldestCacheTime, tenantId, CacheKeyTermsOfUseAgreements);

            }



            if (typesLoaded > 0)

            {

                var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count + AutopilotProfiles.Count + DeviceHealthScripts.Count + MacCustomAttributes.Count + FeatureUpdateProfiles.Count + NamedLocations.Count + AuthenticationStrengthPolicies.Count + AuthenticationContextClassReferences.Count + TermsOfUseAgreements.Count;

                var ageText = FormatCacheAge(oldestCacheTime);

                CacheStatusText = oldestCacheTime.HasValue

                    ? $"Cache: {oldestCacheTime.Value.ToLocalTime():MMM dd, h:mm tt}"

                    : "";

                StatusText = $"Loaded {totalItems} item(s) from cache ({ageText})";

                ApplyFilter();

            }

            else

            {

                DebugLog.Log("Cache", "No cached data found");

            }



            // If all primary overview types loaded, also populate Overview dashboard from cache

            if (typesLoaded >= 4)

            {

                var cachedAssignments = _cacheService.Get<AppAssignmentRow>(tenantId, CacheKeyAppAssignments);

                if (cachedAssignments != null && cachedAssignments.Count > 0)

                {

                    AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(cachedAssignments);

                    _appAssignmentsLoaded = true;

                    DebugLog.Log("Cache", $"Loaded {cachedAssignments.Count} app assignment row(s) from cache for dashboard");

                }



                Overview.Update(

                    ActiveProfile,

                    (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,

                    (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,

                    (IReadOnlyList<MobileApp>)Applications,

                    (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);

                DebugLog.Log("Cache", "Updated Overview dashboard from cache");

            }

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to load from cache: {ex.Message}", ex);

        }



        return typesLoaded;

    }



    private void UpdateOldestCacheTime(ref DateTime? oldest, string tenantId, string dataType)

    {

        var meta = _cacheService.GetMetadata(tenantId, dataType);

        if (meta != null)

        {

            if (oldest == null || meta.Value.CachedAt < oldest.Value)

                oldest = meta.Value.CachedAt;

        }

    }



    private static string FormatCacheAge(DateTime? cachedAtUtc)

    {

        if (cachedAtUtc == null) return "unknown age";

        var age = DateTime.UtcNow - cachedAtUtc.Value;

        if (age.TotalMinutes < 1) return "just now";

        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";

        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h {age.Minutes}m ago";

        return $"{(int)age.TotalDays}d ago";

    }





    /// <summary>

    /// Saves all current collections to the cache.

    /// </summary>

    private void SaveToCache(string tenantId)

    {

        if (string.IsNullOrEmpty(tenantId)) return;



        CacheStatusText = $"Cache: {DateTime.Now:MMM dd, h:mm tt}";



        try

        {

            if (DeviceConfigurations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyDeviceConfigs, DeviceConfigurations.ToList());



            if (CompliancePolicies.Count > 0)

                _cacheService.Set(tenantId, CacheKeyCompliancePolicies, CompliancePolicies.ToList());



            if (Applications.Count > 0)

                _cacheService.Set(tenantId, CacheKeyApplications, Applications.ToList());



            if (SettingsCatalogPolicies.Count > 0)

                _cacheService.Set(tenantId, CacheKeySettingsCatalog, SettingsCatalogPolicies.ToList());



            if (EndpointSecurityIntents.Count > 0)

                _cacheService.Set(tenantId, CacheKeyEndpointSecurity, EndpointSecurityIntents.ToList());



            if (AdministrativeTemplates.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAdministrativeTemplates, AdministrativeTemplates.ToList());



            if (EnrollmentConfigurations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyEnrollmentConfigurations, EnrollmentConfigurations.ToList());



            if (AppProtectionPolicies.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAppProtectionPolicies, AppProtectionPolicies.ToList());



            if (ManagedDeviceAppConfigurations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations.ToList());



            if (TargetedManagedAppConfigurations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyTargetedManagedAppConfigurations, TargetedManagedAppConfigurations.ToList());



            if (TermsAndConditionsCollection.Count > 0)

                _cacheService.Set(tenantId, CacheKeyTermsAndConditions, TermsAndConditionsCollection.ToList());



            if (ScopeTags.Count > 0)

                _cacheService.Set(tenantId, CacheKeyScopeTags, ScopeTags.ToList());



            if (RoleDefinitions.Count > 0)

                _cacheService.Set(tenantId, CacheKeyRoleDefinitions, RoleDefinitions.ToList());



            if (IntuneBrandingProfiles.Count > 0)

                _cacheService.Set(tenantId, CacheKeyIntuneBrandingProfiles, IntuneBrandingProfiles.ToList());



            if (AzureBrandingLocalizations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAzureBrandingLocalizations, AzureBrandingLocalizations.ToList());



            if (ConditionalAccessPolicies.Count > 0)

                _cacheService.Set(tenantId, CacheKeyConditionalAccess, ConditionalAccessPolicies.ToList());



            if (AssignmentFilters.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAssignmentFilters, AssignmentFilters.ToList());



            if (PolicySets.Count > 0)

                _cacheService.Set(tenantId, CacheKeyPolicySets, PolicySets.ToList());



            if (AutopilotProfiles.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAutopilotProfiles, AutopilotProfiles.ToList());



            if (DeviceHealthScripts.Count > 0)

                _cacheService.Set(tenantId, CacheKeyDeviceHealthScripts, DeviceHealthScripts.ToList());



            if (MacCustomAttributes.Count > 0)

                _cacheService.Set(tenantId, CacheKeyMacCustomAttributes, MacCustomAttributes.ToList());



            if (FeatureUpdateProfiles.Count > 0)

                _cacheService.Set(tenantId, CacheKeyFeatureUpdateProfiles, FeatureUpdateProfiles.ToList());



            if (NamedLocations.Count > 0)

                _cacheService.Set(tenantId, CacheKeyNamedLocations, NamedLocations.ToList());



            if (AuthenticationStrengthPolicies.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAuthenticationStrengths, AuthenticationStrengthPolicies.ToList());



            if (AuthenticationContextClassReferences.Count > 0)

                _cacheService.Set(tenantId, CacheKeyAuthenticationContexts, AuthenticationContextClassReferences.ToList());



            if (TermsOfUseAgreements.Count > 0)

                _cacheService.Set(tenantId, CacheKeyTermsOfUseAgreements, TermsOfUseAgreements.ToList());



            DebugLog.Log("Cache", "Saved data to disk cache");

        }

        catch (Exception ex)

        {

            DebugLog.LogError($"Failed to save to cache: {ex.Message}", ex);

        }

    }

}

