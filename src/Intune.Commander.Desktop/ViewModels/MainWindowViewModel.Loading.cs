using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ─── Generic loading helpers ───────────────────────────────────────────

    /// <summary>
    /// Generic loader for lazy-load methods (triggered by navigation).
    /// Manages IsBusy, StatusText, error display, caching, and loaded-flag.
    /// </summary>
    private async Task LoadCollectionAsync<T>(
        object? serviceGuard,
        Func<CancellationToken, Task<List<T>>> fetch,
        Action<ObservableCollection<T>> setCollection,
        Action setLoadedFlag,
        string cacheKey,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        if (serviceGuard == null) return;

        IsBusy = true;
        StatusText = $"Loading {displayName}...";

        try
        {
            var items = await fetch(cancellationToken);
            setCollection(new ObservableCollection<T>(items));
            setLoadedFlag();
            ApplyFilter();

            if (ActiveProfile?.TenantId != null)
            {
                _cacheService.Set(ActiveProfile.TenantId, cacheKey, items);
                DebugLog.Log("Cache", $"Saved {items.Count} {displayName} to cache");
            }

            StatusText = $"Loaded {items.Count} {displayName}";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load {displayName}: {FormatGraphError(ex)}");
            StatusText = $"Error loading {displayName}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Refreshes a single collection during a full refresh.
    /// Does not manage IsBusy (caller owns that). Collects errors into a list.
    /// </summary>
    private async Task RefreshCollectionAsync<T>(
        Func<CancellationToken, Task<List<T>>> fetch,
        Action<ObservableCollection<T>> setCollection,
        Action<bool>? setLoadedFlag,
        string displayName,
        string errorLabel,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        try
        {
            StatusText = $"Loading {displayName}...";
            var items = await fetch(cancellationToken);
            setCollection(new ObservableCollection<T>(items));
            setLoadedFlag?.Invoke(true);
            DebugLog.Log("Graph", $"Loaded {items.Count} {displayName}");
        }
        catch (Exception ex)
        {
            setLoadedFlag?.Invoke(false);
            var detail = FormatGraphError(ex);
            DebugLog.LogError($"Failed to load {displayName}: {detail}", ex);
            errors.Add($"{errorLabel}: {detail}");
        }
    }

    /// <summary>
    /// Tries to load a single collection from cache. Returns true if data was found.
    /// </summary>
    private bool TryLoadCollectionFromCache<T>(
        string tenantId,
        string cacheKey,
        Action<ObservableCollection<T>> setCollection,
        Action? setLoadedFlag,
        string displayName,
        ref DateTime? oldestCacheTime)
    {
        var items = _cacheService.Get<T>(tenantId, cacheKey);
        if (items == null) return false;

        setCollection(new ObservableCollection<T>(items));
        setLoadedFlag?.Invoke();
        DebugLog.Log("Cache", $"Loaded {items.Count} {displayName} from cache");
        UpdateOldestCacheTime(ref oldestCacheTime, tenantId, cacheKey);
        return true;
    }

    /// <summary>
    /// Saves a single collection to cache if it contains items.
    /// </summary>
    private void SaveCollectionToCache<T>(string tenantId, string cacheKey, ObservableCollection<T> collection)
    {
        if (collection.Count > 0)
            _cacheService.Set(tenantId, cacheKey, collection.ToList());
    }

    // ─── Lazy-load methods (called from navigation) ────────────────────────

    private Task LoadConditionalAccessPoliciesAsync() =>
        LoadCollectionAsync(
            _conditionalAccessPolicyService,
            ct => _conditionalAccessPolicyService!.ListPoliciesAsync(ct),
            items => ConditionalAccessPolicies = items,
            () => _conditionalAccessLoaded = true,
            CacheKeyConditionalAccess,
            "conditional access policy(ies)");

    private Task LoadAssignmentFiltersAsync() =>
        LoadCollectionAsync(
            _assignmentFilterService,
            ct => _assignmentFilterService!.ListFiltersAsync(ct),
            items => AssignmentFilters = items,
            () => _assignmentFiltersLoaded = true,
            CacheKeyAssignmentFilters,
            "assignment filter(s)");

    private Task LoadPolicySetsAsync() =>
        LoadCollectionAsync(
            _policySetService,
            ct => _policySetService!.ListPolicySetsAsync(ct),
            items => PolicySets = items,
            () => _policySetsLoaded = true,
            CacheKeyPolicySets,
            "policy set(s)");

    private Task LoadEndpointSecurityIntentsAsync() =>
        LoadCollectionAsync(
            _endpointSecurityService,
            ct => _endpointSecurityService!.ListEndpointSecurityIntentsAsync(ct),
            items => EndpointSecurityIntents = items,
            () => _endpointSecurityLoaded = true,
            CacheKeyEndpointSecurity,
            "endpoint security intent(s)");

    private Task LoadAdministrativeTemplatesAsync() =>
        LoadCollectionAsync(
            _administrativeTemplateService,
            ct => _administrativeTemplateService!.ListAdministrativeTemplatesAsync(ct),
            items => AdministrativeTemplates = items,
            () => _administrativeTemplatesLoaded = true,
            CacheKeyAdministrativeTemplates,
            "administrative template(s)");

    private Task LoadEnrollmentConfigurationsAsync() =>
        LoadCollectionAsync(
            _enrollmentConfigurationService,
            ct => _enrollmentConfigurationService!.ListEnrollmentConfigurationsAsync(ct),
            items => EnrollmentConfigurations = items,
            () => _enrollmentConfigurationsLoaded = true,
            CacheKeyEnrollmentConfigurations,
            "enrollment configuration(s)");

    private Task LoadAppProtectionPoliciesAsync() =>
        LoadCollectionAsync(
            _appProtectionPolicyService,
            ct => _appProtectionPolicyService!.ListAppProtectionPoliciesAsync(ct),
            items => AppProtectionPolicies = items,
            () => _appProtectionPoliciesLoaded = true,
            CacheKeyAppProtectionPolicies,
            "app protection policy(ies)");

    private Task LoadManagedDeviceAppConfigurationsAsync() =>
        LoadCollectionAsync(
            _managedAppConfigurationService,
            ct => _managedAppConfigurationService!.ListManagedDeviceAppConfigurationsAsync(ct),
            items => ManagedDeviceAppConfigurations = items,
            () => _managedDeviceAppConfigurationsLoaded = true,
            CacheKeyManagedDeviceAppConfigurations,
            "managed device app configuration(s)");

    private Task LoadTargetedManagedAppConfigurationsAsync() =>
        LoadCollectionAsync(
            _managedAppConfigurationService,
            ct => _managedAppConfigurationService!.ListTargetedManagedAppConfigurationsAsync(ct),
            items => TargetedManagedAppConfigurations = items,
            () => _targetedManagedAppConfigurationsLoaded = true,
            CacheKeyTargetedManagedAppConfigurations,
            "targeted managed app configuration(s)");

    private Task LoadTermsAndConditionsAsync() =>
        LoadCollectionAsync(
            _termsAndConditionsService,
            ct => _termsAndConditionsService!.ListTermsAndConditionsAsync(ct),
            items => TermsAndConditionsCollection = items,
            () => _termsAndConditionsLoaded = true,
            CacheKeyTermsAndConditions,
            "terms and conditions item(s)");

    private Task LoadScopeTagsAsync() =>
        LoadCollectionAsync(
            _scopeTagService,
            ct => _scopeTagService!.ListScopeTagsAsync(ct),
            items => ScopeTags = items,
            () => _scopeTagsLoaded = true,
            CacheKeyScopeTags,
            "scope tag(s)");

    private Task LoadRoleDefinitionsAsync() =>
        LoadCollectionAsync(
            _roleDefinitionService,
            ct => _roleDefinitionService!.ListRoleDefinitionsAsync(ct),
            items => RoleDefinitions = items,
            () => _roleDefinitionsLoaded = true,
            CacheKeyRoleDefinitions,
            "role definition(s)");

    private Task LoadIntuneBrandingProfilesAsync() =>
        LoadCollectionAsync(
            _intuneBrandingService,
            ct => _intuneBrandingService!.ListIntuneBrandingProfilesAsync(ct),
            items => IntuneBrandingProfiles = items,
            () => _intuneBrandingProfilesLoaded = true,
            CacheKeyIntuneBrandingProfiles,
            "Intune branding profile(s)");

    private Task LoadAzureBrandingLocalizationsAsync() =>
        LoadCollectionAsync(
            _azureBrandingService,
            ct => _azureBrandingService!.ListBrandingLocalizationsAsync(ct),
            items => AzureBrandingLocalizations = items,
            () => _azureBrandingLocalizationsLoaded = true,
            CacheKeyAzureBrandingLocalizations,
            "Azure branding localization(s)");

    private Task LoadAutopilotProfilesAsync() =>
        LoadCollectionAsync(
            _autopilotService,
            ct => _autopilotService!.ListAutopilotProfilesAsync(ct),
            items => AutopilotProfiles = items,
            () => _autopilotProfilesLoaded = true,
            CacheKeyAutopilotProfiles,
            "autopilot profile(s)");

    private Task LoadDeviceHealthScriptsAsync() =>
        LoadCollectionAsync(
            _deviceHealthScriptService,
            ct => _deviceHealthScriptService!.ListDeviceHealthScriptsAsync(ct),
            items => DeviceHealthScripts = items,
            () => _deviceHealthScriptsLoaded = true,
            CacheKeyDeviceHealthScripts,
            "device health script(s)");

    private Task LoadMacCustomAttributesAsync() =>
        LoadCollectionAsync(
            _macCustomAttributeService,
            ct => _macCustomAttributeService!.ListMacCustomAttributesAsync(ct),
            items => MacCustomAttributes = items,
            () => _macCustomAttributesLoaded = true,
            CacheKeyMacCustomAttributes,
            "mac custom attribute(s)");

    private Task LoadFeatureUpdateProfilesAsync() =>
        LoadCollectionAsync(
            _featureUpdateProfileService,
            ct => _featureUpdateProfileService!.ListFeatureUpdateProfilesAsync(ct),
            items => FeatureUpdateProfiles = items,
            () => _featureUpdateProfilesLoaded = true,
            CacheKeyFeatureUpdateProfiles,
            "feature update profile(s)");

    private Task LoadNamedLocationsAsync() =>
        LoadCollectionAsync(
            _namedLocationService,
            ct => _namedLocationService!.ListNamedLocationsAsync(ct),
            items => NamedLocations = items,
            () => _namedLocationsLoaded = true,
            CacheKeyNamedLocations,
            "named location(s)");

    private Task LoadAuthenticationStrengthPoliciesAsync() =>
        LoadCollectionAsync(
            _authenticationStrengthService,
            ct => _authenticationStrengthService!.ListAuthenticationStrengthPoliciesAsync(ct),
            items => AuthenticationStrengthPolicies = items,
            () => _authenticationStrengthPoliciesLoaded = true,
            CacheKeyAuthenticationStrengths,
            "authentication strength policy(ies)");

    private Task LoadAuthenticationContextsAsync() =>
        LoadCollectionAsync(
            _authenticationContextService,
            ct => _authenticationContextService!.ListAuthenticationContextsAsync(ct),
            items => AuthenticationContextClassReferences = items,
            () => _authenticationContextClassReferencesLoaded = true,
            CacheKeyAuthenticationContexts,
            "authentication context(s)");

    private Task LoadTermsOfUseAgreementsAsync() =>
        LoadCollectionAsync(
            _termsOfUseService,
            ct => _termsOfUseService!.ListTermsOfUseAgreementsAsync(ct),
            items => TermsOfUseAgreements = items,
            () => _termsOfUseAgreementsLoaded = true,
            CacheKeyTermsOfUseAgreements,
            "terms of use agreement(s)");

    // ─── BuildGroupRow ─────────────────────────────────────────────────────

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

    // ─── RefreshAsync ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        ClearError();
        IsBusy = true;
        DebugLog.Log("Graph", "Refreshing data from Graph API...");
        var errors = new List<string>();

        // Capture current tab state to decide which lazy types to refresh
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
            // --- Core types (always refreshed) ---

            if (_configProfileService != null)
                await RefreshCollectionAsync(
                    ct => _configProfileService.ListDeviceConfigurationsAsync(ct),
                    items => DeviceConfigurations = items,
                    null, "device configuration(s)", "Device Configs",
                    errors, cancellationToken);

            if (_compliancePolicyService != null)
                await RefreshCollectionAsync(
                    ct => _compliancePolicyService.ListCompliancePoliciesAsync(ct),
                    items => CompliancePolicies = items,
                    null, "compliance policy(ies)", "Compliance Policies",
                    errors, cancellationToken);

            if (_applicationService != null)
                await RefreshCollectionAsync(
                    ct => _applicationService.ListApplicationsAsync(ct),
                    items => Applications = items,
                    null, "application(s)", "Applications",
                    errors, cancellationToken);

            if (_settingsCatalogService != null)
                await RefreshCollectionAsync(
                    ct => _settingsCatalogService.ListSettingsCatalogPoliciesAsync(ct),
                    items => SettingsCatalogPolicies = items,
                    null, "settings catalog policy(ies)", "Settings Catalog",
                    errors, cancellationToken);

            // --- Lazy types (conditional, with skip logging) ---

            if (_conditionalAccessPolicyService != null && loadConditionalAccess)
                await RefreshCollectionAsync(
                    ct => _conditionalAccessPolicyService.ListPoliciesAsync(ct),
                    items => ConditionalAccessPolicies = items,
                    v => _conditionalAccessLoaded = v,
                    "conditional access policy(ies)", "Conditional Access",
                    errors, cancellationToken);
            else if (_conditionalAccessPolicyService != null)
                DebugLog.Log("Graph", "Skipping conditional access refresh (lazy-load when tab selected)");

            if (_endpointSecurityService != null && loadEndpointSecurity)
                await RefreshCollectionAsync(
                    ct => _endpointSecurityService.ListEndpointSecurityIntentsAsync(ct),
                    items => EndpointSecurityIntents = items,
                    v => _endpointSecurityLoaded = v,
                    "endpoint security intent(s)", "Endpoint Security",
                    errors, cancellationToken);
            else if (_endpointSecurityService != null)
                DebugLog.Log("Graph", "Skipping endpoint security refresh (lazy-load when tab selected)");

            if (_administrativeTemplateService != null && loadAdministrativeTemplates)
                await RefreshCollectionAsync(
                    ct => _administrativeTemplateService.ListAdministrativeTemplatesAsync(ct),
                    items => AdministrativeTemplates = items,
                    v => _administrativeTemplatesLoaded = v,
                    "administrative template(s)", "Administrative Templates",
                    errors, cancellationToken);
            else if (_administrativeTemplateService != null)
                DebugLog.Log("Graph", "Skipping administrative templates refresh (lazy-load when tab selected)");

            if (_enrollmentConfigurationService != null && loadEnrollmentConfigurations)
                await RefreshCollectionAsync(
                    ct => _enrollmentConfigurationService.ListEnrollmentConfigurationsAsync(ct),
                    items => EnrollmentConfigurations = items,
                    v => _enrollmentConfigurationsLoaded = v,
                    "enrollment configuration(s)", "Enrollment Configurations",
                    errors, cancellationToken);
            else if (_enrollmentConfigurationService != null)
                DebugLog.Log("Graph", "Skipping enrollment configurations refresh (lazy-load when tab selected)");

            if (_appProtectionPolicyService != null && loadAppProtectionPolicies)
                await RefreshCollectionAsync(
                    ct => _appProtectionPolicyService.ListAppProtectionPoliciesAsync(ct),
                    items => AppProtectionPolicies = items,
                    v => _appProtectionPoliciesLoaded = v,
                    "app protection policy(ies)", "App Protection Policies",
                    errors, cancellationToken);
            else if (_appProtectionPolicyService != null)
                DebugLog.Log("Graph", "Skipping app protection policies refresh (lazy-load when tab selected)");

            if (_managedAppConfigurationService != null && loadManagedDeviceAppConfigurations)
                await RefreshCollectionAsync(
                    ct => _managedAppConfigurationService.ListManagedDeviceAppConfigurationsAsync(ct),
                    items => ManagedDeviceAppConfigurations = items,
                    v => _managedDeviceAppConfigurationsLoaded = v,
                    "managed device app configuration(s)", "Managed Device App Configurations",
                    errors, cancellationToken);
            else if (_managedAppConfigurationService != null)
                DebugLog.Log("Graph", "Skipping managed device app configurations refresh (lazy-load when tab selected)");

            if (_managedAppConfigurationService != null && loadTargetedManagedAppConfigurations)
                await RefreshCollectionAsync(
                    ct => _managedAppConfigurationService.ListTargetedManagedAppConfigurationsAsync(ct),
                    items => TargetedManagedAppConfigurations = items,
                    v => _targetedManagedAppConfigurationsLoaded = v,
                    "targeted managed app configuration(s)", "Targeted Managed App Configurations",
                    errors, cancellationToken);
            else if (_managedAppConfigurationService != null)
                DebugLog.Log("Graph", "Skipping targeted managed app configurations refresh (lazy-load when tab selected)");

            if (_termsAndConditionsService != null && loadTermsAndConditions)
                await RefreshCollectionAsync(
                    ct => _termsAndConditionsService.ListTermsAndConditionsAsync(ct),
                    items => TermsAndConditionsCollection = items,
                    v => _termsAndConditionsLoaded = v,
                    "terms and conditions item(s)", "Terms and Conditions",
                    errors, cancellationToken);
            else if (_termsAndConditionsService != null)
                DebugLog.Log("Graph", "Skipping terms and conditions refresh (lazy-load when tab selected)");

            if (_scopeTagService != null && loadScopeTags)
                await RefreshCollectionAsync(
                    ct => _scopeTagService.ListScopeTagsAsync(ct),
                    items => ScopeTags = items,
                    v => _scopeTagsLoaded = v,
                    "scope tag(s)", "Scope Tags",
                    errors, cancellationToken);
            else if (_scopeTagService != null)
                DebugLog.Log("Graph", "Skipping scope tags refresh (lazy-load when tab selected)");

            if (_roleDefinitionService != null && loadRoleDefinitions)
                await RefreshCollectionAsync(
                    ct => _roleDefinitionService.ListRoleDefinitionsAsync(ct),
                    items => RoleDefinitions = items,
                    v => _roleDefinitionsLoaded = v,
                    "role definition(s)", "Role Definitions",
                    errors, cancellationToken);
            else if (_roleDefinitionService != null)
                DebugLog.Log("Graph", "Skipping role definitions refresh (lazy-load when tab selected)");

            if (_intuneBrandingService != null && loadIntuneBranding)
                await RefreshCollectionAsync(
                    ct => _intuneBrandingService.ListIntuneBrandingProfilesAsync(ct),
                    items => IntuneBrandingProfiles = items,
                    v => _intuneBrandingProfilesLoaded = v,
                    "Intune branding profile(s)", "Intune Branding",
                    errors, cancellationToken);
            else if (_intuneBrandingService != null)
                DebugLog.Log("Graph", "Skipping Intune branding refresh (lazy-load when tab selected)");

            if (_azureBrandingService != null && loadAzureBranding)
                await RefreshCollectionAsync(
                    ct => _azureBrandingService.ListBrandingLocalizationsAsync(ct),
                    items => AzureBrandingLocalizations = items,
                    v => _azureBrandingLocalizationsLoaded = v,
                    "Azure branding localization(s)", "Azure Branding",
                    errors, cancellationToken);
            else if (_azureBrandingService != null)
                DebugLog.Log("Graph", "Skipping Azure branding refresh (lazy-load when tab selected)");

            if (_assignmentFilterService != null && loadAssignmentFilters)
                await RefreshCollectionAsync(
                    ct => _assignmentFilterService.ListFiltersAsync(ct),
                    items => AssignmentFilters = items,
                    v => _assignmentFiltersLoaded = v,
                    "assignment filter(s)", "Assignment Filters",
                    errors, cancellationToken);
            else if (_assignmentFilterService != null)
                DebugLog.Log("Graph", "Skipping assignment filter refresh (lazy-load when tab selected)");

            if (_policySetService != null && loadPolicySets)
                await RefreshCollectionAsync(
                    ct => _policySetService.ListPolicySetsAsync(ct),
                    items => PolicySets = items,
                    v => _policySetsLoaded = v,
                    "policy set(s)", "Policy Sets",
                    errors, cancellationToken);
            else if (_policySetService != null)
                DebugLog.Log("Graph", "Skipping policy sets refresh (lazy-load when tab selected)");

            // --- Wave 4/5 types (conditional, no skip logging) ---

            if (_autopilotService != null && loadAutopilotProfiles)
                await RefreshCollectionAsync(
                    ct => _autopilotService.ListAutopilotProfilesAsync(ct),
                    items => AutopilotProfiles = items,
                    v => _autopilotProfilesLoaded = v,
                    "autopilot profile(s)", "Autopilot Profiles",
                    errors, cancellationToken);

            if (_deviceHealthScriptService != null && loadDeviceHealthScripts)
                await RefreshCollectionAsync(
                    ct => _deviceHealthScriptService.ListDeviceHealthScriptsAsync(ct),
                    items => DeviceHealthScripts = items,
                    v => _deviceHealthScriptsLoaded = v,
                    "device health script(s)", "Device Health Scripts",
                    errors, cancellationToken);

            if (_macCustomAttributeService != null && loadMacCustomAttributes)
                await RefreshCollectionAsync(
                    ct => _macCustomAttributeService.ListMacCustomAttributesAsync(ct),
                    items => MacCustomAttributes = items,
                    v => _macCustomAttributesLoaded = v,
                    "mac custom attribute(s)", "Mac Custom Attributes",
                    errors, cancellationToken);

            if (_featureUpdateProfileService != null && loadFeatureUpdates)
                await RefreshCollectionAsync(
                    ct => _featureUpdateProfileService.ListFeatureUpdateProfilesAsync(ct),
                    items => FeatureUpdateProfiles = items,
                    v => _featureUpdateProfilesLoaded = v,
                    "feature update profile(s)", "Feature Updates",
                    errors, cancellationToken);

            if (_namedLocationService != null && loadNamedLocations)
                await RefreshCollectionAsync(
                    ct => _namedLocationService.ListNamedLocationsAsync(ct),
                    items => NamedLocations = items,
                    v => _namedLocationsLoaded = v,
                    "named location(s)", "Named Locations",
                    errors, cancellationToken);

            if (_authenticationStrengthService != null && loadAuthenticationStrengths)
                await RefreshCollectionAsync(
                    ct => _authenticationStrengthService.ListAuthenticationStrengthPoliciesAsync(ct),
                    items => AuthenticationStrengthPolicies = items,
                    v => _authenticationStrengthPoliciesLoaded = v,
                    "authentication strength policy(ies)", "Authentication Strengths",
                    errors, cancellationToken);

            if (_authenticationContextService != null && loadAuthenticationContexts)
                await RefreshCollectionAsync(
                    ct => _authenticationContextService.ListAuthenticationContextsAsync(ct),
                    items => AuthenticationContextClassReferences = items,
                    v => _authenticationContextClassReferencesLoaded = v,
                    "authentication context(s)", "Authentication Contexts",
                    errors, cancellationToken);

            if (_termsOfUseService != null && loadTermsOfUse)
                await RefreshCollectionAsync(
                    ct => _termsOfUseService.ListTermsOfUseAgreementsAsync(ct),
                    items => TermsOfUseAgreements = items,
                    v => _termsOfUseAgreementsLoaded = v,
                    "terms of use agreement(s)", "Terms Of Use",
                    errors, cancellationToken);

            // --- Summary ---

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

    // ─── Cache helpers ─────────────────────────────────────────────────────

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
            // Core types (no loaded flag)
            if (TryLoadCollectionFromCache<DeviceConfiguration>(
                tenantId, CacheKeyDeviceConfigs,
                items => DeviceConfigurations = items,
                null, "device configuration(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceCompliancePolicy>(
                tenantId, CacheKeyCompliancePolicies,
                items => CompliancePolicies = items,
                null, "compliance policy(ies)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<MobileApp>(
                tenantId, CacheKeyApplications,
                items => Applications = items,
                null, "application(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceManagementConfigurationPolicy>(
                tenantId, CacheKeySettingsCatalog,
                items => SettingsCatalogPolicies = items,
                null, "settings catalog policy(ies)", ref oldestCacheTime))
                typesLoaded++;

            // Lazy types (with loaded flag)
            if (TryLoadCollectionFromCache<DeviceManagementIntent>(
                tenantId, CacheKeyEndpointSecurity,
                items => EndpointSecurityIntents = items,
                () => _endpointSecurityLoaded = true,
                "endpoint security intent(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<GroupPolicyConfiguration>(
                tenantId, CacheKeyAdministrativeTemplates,
                items => AdministrativeTemplates = items,
                () => _administrativeTemplatesLoaded = true,
                "administrative template(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceEnrollmentConfiguration>(
                tenantId, CacheKeyEnrollmentConfigurations,
                items => EnrollmentConfigurations = items,
                () => _enrollmentConfigurationsLoaded = true,
                "enrollment configuration(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<ManagedAppPolicy>(
                tenantId, CacheKeyAppProtectionPolicies,
                items => AppProtectionPolicies = items,
                () => _appProtectionPoliciesLoaded = true,
                "app protection policy(ies)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<ManagedDeviceMobileAppConfiguration>(
                tenantId, CacheKeyManagedDeviceAppConfigurations,
                items => ManagedDeviceAppConfigurations = items,
                () => _managedDeviceAppConfigurationsLoaded = true,
                "managed device app configuration(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<TargetedManagedAppConfiguration>(
                tenantId, CacheKeyTargetedManagedAppConfigurations,
                items => TargetedManagedAppConfigurations = items,
                () => _targetedManagedAppConfigurationsLoaded = true,
                "targeted managed app configuration(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<TermsAndConditions>(
                tenantId, CacheKeyTermsAndConditions,
                items => TermsAndConditionsCollection = items,
                () => _termsAndConditionsLoaded = true,
                "terms and conditions item(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<RoleScopeTag>(
                tenantId, CacheKeyScopeTags,
                items => ScopeTags = items,
                () => _scopeTagsLoaded = true,
                "scope tag(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<RoleDefinition>(
                tenantId, CacheKeyRoleDefinitions,
                items => RoleDefinitions = items,
                () => _roleDefinitionsLoaded = true,
                "role definition(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<IntuneBrandingProfile>(
                tenantId, CacheKeyIntuneBrandingProfiles,
                items => IntuneBrandingProfiles = items,
                () => _intuneBrandingProfilesLoaded = true,
                "Intune branding profile(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<OrganizationalBrandingLocalization>(
                tenantId, CacheKeyAzureBrandingLocalizations,
                items => AzureBrandingLocalizations = items,
                () => _azureBrandingLocalizationsLoaded = true,
                "Azure branding localization(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<ConditionalAccessPolicy>(
                tenantId, CacheKeyConditionalAccess,
                items => ConditionalAccessPolicies = items,
                () => _conditionalAccessLoaded = true,
                "conditional access policy(ies)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceAndAppManagementAssignmentFilter>(
                tenantId, CacheKeyAssignmentFilters,
                items => AssignmentFilters = items,
                () => _assignmentFiltersLoaded = true,
                "assignment filter(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<PolicySet>(
                tenantId, CacheKeyPolicySets,
                items => PolicySets = items,
                () => _policySetsLoaded = true,
                "policy set(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<WindowsAutopilotDeploymentProfile>(
                tenantId, CacheKeyAutopilotProfiles,
                items => AutopilotProfiles = items,
                () => _autopilotProfilesLoaded = true,
                "autopilot profile(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceHealthScript>(
                tenantId, CacheKeyDeviceHealthScripts,
                items => DeviceHealthScripts = items,
                () => _deviceHealthScriptsLoaded = true,
                "device health script(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<DeviceCustomAttributeShellScript>(
                tenantId, CacheKeyMacCustomAttributes,
                items => MacCustomAttributes = items,
                () => _macCustomAttributesLoaded = true,
                "mac custom attribute(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<WindowsFeatureUpdateProfile>(
                tenantId, CacheKeyFeatureUpdateProfiles,
                items => FeatureUpdateProfiles = items,
                () => _featureUpdateProfilesLoaded = true,
                "feature update profile(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<NamedLocation>(
                tenantId, CacheKeyNamedLocations,
                items => NamedLocations = items,
                () => _namedLocationsLoaded = true,
                "named location(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<AuthenticationStrengthPolicy>(
                tenantId, CacheKeyAuthenticationStrengths,
                items => AuthenticationStrengthPolicies = items,
                () => _authenticationStrengthPoliciesLoaded = true,
                "authentication strength policy(ies)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<AuthenticationContextClassReference>(
                tenantId, CacheKeyAuthenticationContexts,
                items => AuthenticationContextClassReferences = items,
                () => _authenticationContextClassReferencesLoaded = true,
                "authentication context(s)", ref oldestCacheTime))
                typesLoaded++;

            if (TryLoadCollectionFromCache<Agreement>(
                tenantId, CacheKeyTermsOfUseAgreements,
                items => TermsOfUseAgreements = items,
                () => _termsOfUseAgreementsLoaded = true,
                "terms of use agreement(s)", ref oldestCacheTime))
                typesLoaded++;

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
            SaveCollectionToCache(tenantId, CacheKeyDeviceConfigs, DeviceConfigurations);
            SaveCollectionToCache(tenantId, CacheKeyCompliancePolicies, CompliancePolicies);
            SaveCollectionToCache(tenantId, CacheKeyApplications, Applications);
            SaveCollectionToCache(tenantId, CacheKeySettingsCatalog, SettingsCatalogPolicies);
            SaveCollectionToCache(tenantId, CacheKeyEndpointSecurity, EndpointSecurityIntents);
            SaveCollectionToCache(tenantId, CacheKeyAdministrativeTemplates, AdministrativeTemplates);
            SaveCollectionToCache(tenantId, CacheKeyEnrollmentConfigurations, EnrollmentConfigurations);
            SaveCollectionToCache(tenantId, CacheKeyAppProtectionPolicies, AppProtectionPolicies);
            SaveCollectionToCache(tenantId, CacheKeyManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations);
            SaveCollectionToCache(tenantId, CacheKeyTargetedManagedAppConfigurations, TargetedManagedAppConfigurations);
            SaveCollectionToCache(tenantId, CacheKeyTermsAndConditions, TermsAndConditionsCollection);
            SaveCollectionToCache(tenantId, CacheKeyScopeTags, ScopeTags);
            SaveCollectionToCache(tenantId, CacheKeyRoleDefinitions, RoleDefinitions);
            SaveCollectionToCache(tenantId, CacheKeyIntuneBrandingProfiles, IntuneBrandingProfiles);
            SaveCollectionToCache(tenantId, CacheKeyAzureBrandingLocalizations, AzureBrandingLocalizations);
            SaveCollectionToCache(tenantId, CacheKeyConditionalAccess, ConditionalAccessPolicies);
            SaveCollectionToCache(tenantId, CacheKeyAssignmentFilters, AssignmentFilters);
            SaveCollectionToCache(tenantId, CacheKeyPolicySets, PolicySets);
            SaveCollectionToCache(tenantId, CacheKeyAutopilotProfiles, AutopilotProfiles);
            SaveCollectionToCache(tenantId, CacheKeyDeviceHealthScripts, DeviceHealthScripts);
            SaveCollectionToCache(tenantId, CacheKeyMacCustomAttributes, MacCustomAttributes);
            SaveCollectionToCache(tenantId, CacheKeyFeatureUpdateProfiles, FeatureUpdateProfiles);
            SaveCollectionToCache(tenantId, CacheKeyNamedLocations, NamedLocations);
            SaveCollectionToCache(tenantId, CacheKeyAuthenticationStrengths, AuthenticationStrengthPolicies);
            SaveCollectionToCache(tenantId, CacheKeyAuthenticationContexts, AuthenticationContextClassReferences);
            SaveCollectionToCache(tenantId, CacheKeyTermsOfUseAgreements, TermsOfUseAgreements);

            DebugLog.Log("Cache", "Saved data to disk cache");
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to save to cache: {ex.Message}", ex);
        }
    }
}

