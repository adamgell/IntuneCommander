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
                await _cacheService.SetAsync(ActiveProfile.TenantId, cacheKey, items);
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
    private async Task<(bool loaded, DateTime? oldestCacheTime)> TryLoadCollectionFromCacheAsync<T>(
        string tenantId,
        string cacheKey,
        Action<ObservableCollection<T>> setCollection,
        Action? setLoadedFlag,
        string displayName,
        DateTime? oldestCacheTime)
    {
        var items = await _cacheService.GetAsync<T>(tenantId, cacheKey);
        if (items == null) return (false, oldestCacheTime);

        setCollection(new ObservableCollection<T>(items));
        setLoadedFlag?.Invoke();
        DebugLog.Log("Cache", $"Loaded {items.Count} {displayName} from cache");
        oldestCacheTime = await UpdateOldestCacheTimeAsync(oldestCacheTime, tenantId, cacheKey);
        return (true, oldestCacheTime);
    }

    /// <summary>
    /// Saves a single collection to cache if it contains items.
    /// </summary>
    private async Task SaveCollectionToCacheAsync<T>(string tenantId, string cacheKey, ObservableCollection<T> collection)
    {
        if (collection.Count > 0)
            await _cacheService.SetAsync(tenantId, cacheKey, collection.ToList());
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

    private Task LoadQualityUpdateProfilesAsync() =>
        LoadCollectionAsync(
            _qualityUpdateProfileService,
            ct => _qualityUpdateProfileService!.ListQualityUpdateProfilesAsync(ct),
            items => QualityUpdateProfiles = items,
            () => _qualityUpdateProfilesLoaded = true,
            CacheKeyQualityUpdateProfiles,
            "quality update profile(s)");

    private Task LoadDriverUpdateProfilesAsync() =>
        LoadCollectionAsync(
            _driverUpdateProfileService,
            ct => _driverUpdateProfileService!.ListDriverUpdateProfilesAsync(ct),
            items => DriverUpdateProfiles = items,
            () => _driverUpdateProfilesLoaded = true,
            CacheKeyDriverUpdateProfiles,
            "driver update profile(s)");

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

    private Task LoadDeviceManagementScriptsAsync() =>
        LoadCollectionAsync(
            _deviceManagementScriptService,
            ct => _deviceManagementScriptService!.ListDeviceManagementScriptsAsync(ct),
            items => DeviceManagementScripts = items,
            () => _deviceManagementScriptsLoaded = true,
            CacheKeyDeviceManagementScripts,
            "device management script(s)");

    private Task LoadDeviceShellScriptsAsync() =>
        LoadCollectionAsync(
            _deviceShellScriptService,
            ct => _deviceShellScriptService!.ListDeviceShellScriptsAsync(ct),
            items => DeviceShellScripts = items,
            () => _deviceShellScriptsLoaded = true,
            CacheKeyDeviceShellScripts,
            "device shell script(s)");

    private Task LoadComplianceScriptsAsync() =>
        LoadCollectionAsync(
            _complianceScriptService,
            ct => _complianceScriptService!.ListComplianceScriptsAsync(ct),
            items => ComplianceScripts = items,
            () => _complianceScriptsLoaded = true,
            CacheKeyComplianceScripts,
            "compliance script(s)");

    private Task LoadAppleDepSettingsAsync() =>
        LoadCollectionAsync(
            _appleDepService,
            ct => _appleDepService!.ListDepOnboardingSettingsAsync(ct),
            items => AppleDepSettings = items,
            () => _appleDepSettingsLoaded = true,
            CacheKeyAppleDepSettings,
            "Apple DEP onboarding setting(s)");

    private Task LoadDeviceCategoriesAsync() =>
        LoadCollectionAsync(
            _deviceCategoryService,
            ct => _deviceCategoryService!.ListDeviceCategoriesAsync(ct),
            items => DeviceCategories = items,
            () => _deviceCategoriesLoaded = true,
            CacheKeyDeviceCategories,
            "device category(ies)");

    private Task LoadCloudPcProvisioningPoliciesAsync() =>
        LoadCollectionAsync(
            _cloudPcProvisioningService,
            ct => _cloudPcProvisioningService!.ListProvisioningPoliciesAsync(ct),
            items => CloudPcProvisioningPolicies = items,
            () => _cloudPcProvisioningPoliciesLoaded = true,
            CacheKeyCloudPcProvisioningPolicies,
            "Cloud PC provisioning policy(ies)");

    private Task LoadCloudPcUserSettingsAsync() =>
        LoadCollectionAsync(
            _cloudPcUserSettingsService,
            ct => _cloudPcUserSettingsService!.ListUserSettingsAsync(ct),
            items => CloudPcUserSettings = items,
            () => _cloudPcUserSettingsLoaded = true,
            CacheKeyCloudPcUserSettings,
            "Cloud PC user setting(s)");

    private Task LoadVppTokensAsync() =>
        LoadCollectionAsync(
            _vppTokenService,
            ct => _vppTokenService!.ListVppTokensAsync(ct),
            items => VppTokens = items,
            () => _vppTokensLoaded = true,
            CacheKeyVppTokens,
            "VPP token(s)");

    private Task LoadRoleAssignmentsAsync() =>
        LoadCollectionAsync(
            _roleDefinitionService,
            ct => _roleDefinitionService!.GetRoleAssignmentsAsync(ct),
            items => RoleAssignments = items,
            () => _roleAssignmentsLoaded = true,
            CacheKeyRoleAssignments,
            "role assignment(s)");

    private Task LoadAdmxFilesAsync() =>
        LoadCollectionAsync(
            _admxFileService,
            ct => _admxFileService!.ListAdmxFilesAsync(ct),
            items => AdmxFiles = items,
            () => _admxFilesLoaded = true,
            CacheKeyAdmxFiles,
            "ADMX file(s)");

    private Task LoadReusablePolicySettingsAsync() =>
        LoadCollectionAsync(
            _reusablePolicySettingService,
            ct => _reusablePolicySettingService!.ListReusablePolicySettingsAsync(ct),
            items => ReusablePolicySettings = items,
            () => _reusablePolicySettingsLoaded = true,
            CacheKeyReusablePolicySettings,
            "reusable policy setting(s)");

    private Task LoadNotificationTemplatesAsync() =>
        LoadCollectionAsync(
            _notificationTemplateService,
            ct => _notificationTemplateService!.ListNotificationTemplatesAsync(ct),
            items => NotificationTemplates = items,
            () => _notificationTemplatesLoaded = true,
            CacheKeyNotificationTemplates,
            "notification template(s)");

    [RelayCommand]
    private async Task LoadDevicesAndUsersAsync(CancellationToken cancellationToken)
    {
        if (_managedDeviceService == null || _entraUserService == null || ActiveProfile?.TenantId == null)
            return;

        IsBusy = true;
        StatusText = "Loading devices and users...";

        try
        {
            var tenantId = ActiveProfile.TenantId;

            var devices = await _cacheService.GetAsync<ManagedDevice>(tenantId, CacheKeyManagedDevices);
            if (devices == null)
            {
                devices = await _managedDeviceService.ListManagedDevicesAsync(cancellationToken);
                await _cacheService.SetAsync(tenantId, CacheKeyManagedDevices, devices);
            }

            var users = await _cacheService.GetAsync<User>(tenantId, CacheKeyEntraUsers);
            if (users == null)
            {
                users = await _entraUserService.ListUsersAsync(cancellationToken);
                await _cacheService.SetAsync(tenantId, CacheKeyEntraUsers, users);
            }

            DeviceUserEntries = new ObservableCollection<DeviceUserEntry>(BuildDeviceUserEntries(devices, users));
            _deviceUserEntriesLoaded = true;
            ApplyFilter();
            await UpdateDevicesAndUsersCacheStatusAsync(tenantId);
            StatusText = $"Loaded {DeviceUserEntries.Count} correlated device/user row(s)";
        }
        catch (Exception ex)
        {
            SetError($"Failed to load devices and users: {FormatGraphError(ex)}");
            StatusText = "Error loading devices and users";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ForceRefreshDevicesAndUsersAsync(CancellationToken cancellationToken)
    {
        if (ActiveProfile?.TenantId == null)
            return;

        await _cacheService.InvalidateAsync(ActiveProfile.TenantId, CacheKeyManagedDevices);
        await _cacheService.InvalidateAsync(ActiveProfile.TenantId, CacheKeyEntraUsers);
        _deviceUserEntriesLoaded = false;
        await LoadDevicesAndUsersAsync(cancellationToken);
    }

    private static List<DeviceUserEntry> BuildDeviceUserEntries(List<ManagedDevice> devices, List<User> users)
    {
        var userById = users
            .Where(u => !string.IsNullOrEmpty(u.Id))
            .GroupBy(u => u.Id!)
            .ToDictionary(g => g.Key, g => g.First());

        return devices
            .Select(device =>
            {
                userById.TryGetValue(device.UserId ?? "", out var user);
                return DeviceUserEntry.From(device, user);
            })
            .OrderBy(r => r.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task UpdateDevicesAndUsersCacheStatusAsync(string tenantId)
    {
        var deviceMeta = await _cacheService.GetMetadataAsync(tenantId, CacheKeyManagedDevices);
        var userMeta = await _cacheService.GetMetadataAsync(tenantId, CacheKeyEntraUsers);

        if (deviceMeta == null || userMeta == null)
        {
            DevicesAndUsersCacheStatusText = "";
            return;
        }

        var lastRefreshed = deviceMeta.Value.CachedAt < userMeta.Value.CachedAt
            ? deviceMeta.Value.CachedAt
            : userMeta.Value.CachedAt;
        DevicesAndUsersCacheStatusText =
            $"Last refreshed: {FormatCacheAge(lastRefreshed)} ({deviceMeta.Value.ItemCount} devices, {userMeta.Value.ItemCount} users)";
        CacheStatusText = DevicesAndUsersCacheStatusText;
    }

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
        var loadQualityUpdates = IsQualityUpdatesCategory;
        var loadDriverUpdates = IsDriverUpdatesCategory;
        var loadNamedLocations = IsNamedLocationsCategory;
        var loadAuthenticationStrengths = IsAuthenticationStrengthsCategory;
        var loadAuthenticationContexts = IsAuthenticationContextsCategory;
        var loadTermsOfUse = IsTermsOfUseCategory;
        var loadDeviceManagementScripts = IsDeviceManagementScriptsCategory;
        var loadDeviceShellScripts = IsDeviceShellScriptsCategory;
        var loadComplianceScripts = IsComplianceScriptsCategory;
        var loadAppleDep = IsAppleDepCategory;
        var loadDeviceCategories = IsDeviceCategoriesCategory;
        var loadCloudPcProvisioningPolicies = IsCloudPcProvisioningCategory;
        var loadCloudPcUserSettings = IsCloudPcUserSettingsCategory;
        var loadVppTokens = IsVppTokensCategory;
        var loadRoleAssignments = IsRoleAssignmentsCategory;
        var loadAdmxFiles = IsAdmxFilesCategory;
        var loadReusablePolicySettings = IsReusablePolicySettingsCategory;
        var loadNotificationTemplates = IsNotificationTemplatesCategory;

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

            if (_deviceManagementScriptService != null && loadDeviceManagementScripts)
                await RefreshCollectionAsync(
                    ct => _deviceManagementScriptService.ListDeviceManagementScriptsAsync(ct),
                    items => DeviceManagementScripts = items,
                    v => _deviceManagementScriptsLoaded = v,
                    "device management script(s)", "Device Management Scripts",
                    errors, cancellationToken);

            if (_deviceShellScriptService != null && loadDeviceShellScripts)
                await RefreshCollectionAsync(
                    ct => _deviceShellScriptService.ListDeviceShellScriptsAsync(ct),
                    items => DeviceShellScripts = items,
                    v => _deviceShellScriptsLoaded = v,
                    "device shell script(s)", "Device Shell Scripts",
                    errors, cancellationToken);

            if (_complianceScriptService != null && loadComplianceScripts)
                await RefreshCollectionAsync(
                    ct => _complianceScriptService.ListComplianceScriptsAsync(ct),
                    items => ComplianceScripts = items,
                    v => _complianceScriptsLoaded = v,
                    "compliance script(s)", "Compliance Scripts",
                    errors, cancellationToken);

            if (_appleDepService != null && loadAppleDep)
                await RefreshCollectionAsync(
                    ct => _appleDepService.ListDepOnboardingSettingsAsync(ct),
                    items => AppleDepSettings = items,
                    v => _appleDepSettingsLoaded = v,
                    "Apple DEP onboarding setting(s)", "Apple DEP",
                    errors, cancellationToken);

            if (_deviceCategoryService != null && loadDeviceCategories)
                await RefreshCollectionAsync(
                    ct => _deviceCategoryService.ListDeviceCategoriesAsync(ct),
                    items => DeviceCategories = items,
                    v => _deviceCategoriesLoaded = v,
                    "device category(ies)", "Device Categories",
                    errors, cancellationToken);

            if (_cloudPcProvisioningService != null && loadCloudPcProvisioningPolicies)
                await RefreshCollectionAsync(
                    ct => _cloudPcProvisioningService.ListProvisioningPoliciesAsync(ct),
                    items => CloudPcProvisioningPolicies = items,
                    v => _cloudPcProvisioningPoliciesLoaded = v,
                    "Cloud PC provisioning policy(ies)", "Cloud PC Provisioning Policies",
                    errors, cancellationToken);

            if (_cloudPcUserSettingsService != null && loadCloudPcUserSettings)
                await RefreshCollectionAsync(
                    ct => _cloudPcUserSettingsService.ListUserSettingsAsync(ct),
                    items => CloudPcUserSettings = items,
                    v => _cloudPcUserSettingsLoaded = v,
                    "Cloud PC user setting(s)", "Cloud PC User Settings",
                    errors, cancellationToken);

            if (_vppTokenService != null && loadVppTokens)
                await RefreshCollectionAsync(
                    ct => _vppTokenService.ListVppTokensAsync(ct),
                    items => VppTokens = items,
                    v => _vppTokensLoaded = v,
                    "VPP token(s)", "VPP Tokens",
                    errors, cancellationToken);

            if (_roleDefinitionService != null && loadRoleAssignments)
                await RefreshCollectionAsync(
                    ct => _roleDefinitionService.GetRoleAssignmentsAsync(ct),
                    items => RoleAssignments = items,
                    v => _roleAssignmentsLoaded = v,
                    "role assignment(s)", "Role Assignments",
                    errors, cancellationToken);

            if (_qualityUpdateProfileService != null && loadQualityUpdates)
                await RefreshCollectionAsync(
                    ct => _qualityUpdateProfileService.ListQualityUpdateProfilesAsync(ct),
                    items => QualityUpdateProfiles = items,
                    v => _qualityUpdateProfilesLoaded = v,
                    "quality update profile(s)", "Quality Updates",
                    errors, cancellationToken);

            if (_driverUpdateProfileService != null && loadDriverUpdates)
                await RefreshCollectionAsync(
                    ct => _driverUpdateProfileService.ListDriverUpdateProfilesAsync(ct),
                    items => DriverUpdateProfiles = items,
                    v => _driverUpdateProfilesLoaded = v,
                    "driver update profile(s)", "Driver Updates",
                    errors, cancellationToken);

            if (_admxFileService != null && loadAdmxFiles)
                await RefreshCollectionAsync(
                    ct => _admxFileService.ListAdmxFilesAsync(ct),
                    items => AdmxFiles = items,
                    v => _admxFilesLoaded = v,
                    "ADMX file(s)", "ADMX Files",
                    errors, cancellationToken);

            if (_reusablePolicySettingService != null && loadReusablePolicySettings)
                await RefreshCollectionAsync(
                    ct => _reusablePolicySettingService.ListReusablePolicySettingsAsync(ct),
                    items => ReusablePolicySettings = items,
                    v => _reusablePolicySettingsLoaded = v,
                    "reusable policy setting(s)", "Reusable Policy Settings",
                    errors, cancellationToken);

            if (_notificationTemplateService != null && loadNotificationTemplates)
                await RefreshCollectionAsync(
                    ct => _notificationTemplateService.ListNotificationTemplatesAsync(ct),
                    items => NotificationTemplates = items,
                    v => _notificationTemplatesLoaded = v,
                    "notification template(s)", "Notification Templates",
                    errors, cancellationToken);

            // --- Summary ---

            var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count + AutopilotProfiles.Count + DeviceHealthScripts.Count + MacCustomAttributes.Count + FeatureUpdateProfiles.Count + QualityUpdateProfiles.Count + DriverUpdateProfiles.Count + NamedLocations.Count + AuthenticationStrengthPolicies.Count + AuthenticationContextClassReferences.Count + TermsOfUseAgreements.Count + DeviceManagementScripts.Count + DeviceShellScripts.Count + ComplianceScripts.Count + AppleDepSettings.Count + DeviceCategories.Count + CloudPcProvisioningPolicies.Count + CloudPcUserSettings.Count + VppTokens.Count + RoleAssignments.Count + AdmxFiles.Count + ReusablePolicySettings.Count + NotificationTemplates.Count;
            StatusText = $"Loaded {totalItems} item(s) ({DeviceConfigurations.Count} configs, {CompliancePolicies.Count} compliance, {Applications.Count} apps, {SettingsCatalogPolicies.Count} settings catalog, {EndpointSecurityIntents.Count} endpoint security, {AdministrativeTemplates.Count} admin templates, {EnrollmentConfigurations.Count} enrollment configs, {AppProtectionPolicies.Count} app protection, {ManagedDeviceAppConfigurations.Count} managed device app configs, {TargetedManagedAppConfigurations.Count} targeted app configs, {TermsAndConditionsCollection.Count} terms, {ScopeTags.Count} scope tags, {RoleDefinitions.Count} role definitions, {IntuneBrandingProfiles.Count} intune branding, {AzureBrandingLocalizations.Count} azure branding, {ConditionalAccessPolicies.Count} conditional access, {AssignmentFilters.Count} filters, {PolicySets.Count} policy sets, {AutopilotProfiles.Count} autopilot, {DeviceHealthScripts.Count} device health scripts, {MacCustomAttributes.Count} mac custom attributes, {FeatureUpdateProfiles.Count} feature updates, {QualityUpdateProfiles.Count} quality updates, {DriverUpdateProfiles.Count} driver updates, {NamedLocations.Count} named locations, {AuthenticationStrengthPolicies.Count} auth strengths, {AuthenticationContextClassReferences.Count} auth contexts, {TermsOfUseAgreements.Count} terms of use, {DeviceManagementScripts.Count} device mgmt scripts, {DeviceShellScripts.Count} shell scripts, {ComplianceScripts.Count} compliance scripts, {AppleDepSettings.Count} Apple DEP settings, {DeviceCategories.Count} device categories, {CloudPcProvisioningPolicies.Count} Cloud PC provisioning, {CloudPcUserSettings.Count} Cloud PC user settings, {VppTokens.Count} VPP tokens, {RoleAssignments.Count} role assignments, {AdmxFiles.Count} ADMX files, {ReusablePolicySettings.Count} reusable policy settings, {NotificationTemplates.Count} notification templates)";

            if (errors.Count > 0)
                SetError($"Some data failed to load — {string.Join("; ", errors)}");

            // Save successful loads to cache
            if (ActiveProfile?.TenantId != null)
                await SaveToCacheAsync(ActiveProfile.TenantId);

            ApplyFilter();

            // Reset lazy-load state; actual loading is triggered when navigating to those tabs
            _appAssignmentsLoaded = false;
            _dynamicGroupsLoaded = false;
            _assignedGroupsLoaded = false;

            // Invalidate lazy-load caches so they reload from Graph on next tab visit
            if (ActiveProfile?.TenantId != null)
            {
                await _cacheService.InvalidateAsync(ActiveProfile.TenantId, CacheKeyAppAssignments);
                await _cacheService.InvalidateAsync(ActiveProfile.TenantId, CacheKeyDynamicGroups);
                await _cacheService.InvalidateAsync(ActiveProfile.TenantId, CacheKeyAssignedGroups);
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
    private async Task<int> TryLoadFromCacheAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return 0;

        var typesLoaded = 0;
        DateTime? oldestCacheTime = null;

        try
        {
            // Helper to reduce repetition: calls TryLoadCollectionFromCacheAsync and increments counter
            async Task TryLoad<T>(string cacheKey, Action<ObservableCollection<T>> setCollection,
                Action? setLoadedFlag, string displayName)
            {
                var (loaded, updated) = await TryLoadCollectionFromCacheAsync(
                    tenantId, cacheKey, setCollection, setLoadedFlag, displayName, oldestCacheTime);
                if (loaded)
                {
                    typesLoaded++;
                    oldestCacheTime = updated;
                }
            }

            // Core types (no loaded flag)
            await TryLoad<DeviceConfiguration>(CacheKeyDeviceConfigs,
                items => DeviceConfigurations = items, null, "device configuration(s)");

            await TryLoad<DeviceCompliancePolicy>(CacheKeyCompliancePolicies,
                items => CompliancePolicies = items, null, "compliance policy(ies)");

            await TryLoad<MobileApp>(CacheKeyApplications,
                items => Applications = items, null, "application(s)");

            await TryLoad<DeviceManagementConfigurationPolicy>(CacheKeySettingsCatalog,
                items => SettingsCatalogPolicies = items, null, "settings catalog policy(ies)");

            // Lazy types (with loaded flag)
            await TryLoad<DeviceManagementIntent>(CacheKeyEndpointSecurity,
                items => EndpointSecurityIntents = items,
                () => _endpointSecurityLoaded = true, "endpoint security intent(s)");

            await TryLoad<GroupPolicyConfiguration>(CacheKeyAdministrativeTemplates,
                items => AdministrativeTemplates = items,
                () => _administrativeTemplatesLoaded = true, "administrative template(s)");

            await TryLoad<DeviceEnrollmentConfiguration>(CacheKeyEnrollmentConfigurations,
                items => EnrollmentConfigurations = items,
                () => _enrollmentConfigurationsLoaded = true, "enrollment configuration(s)");

            await TryLoad<ManagedAppPolicy>(CacheKeyAppProtectionPolicies,
                items => AppProtectionPolicies = items,
                () => _appProtectionPoliciesLoaded = true, "app protection policy(ies)");

            await TryLoad<ManagedDeviceMobileAppConfiguration>(CacheKeyManagedDeviceAppConfigurations,
                items => ManagedDeviceAppConfigurations = items,
                () => _managedDeviceAppConfigurationsLoaded = true, "managed device app configuration(s)");

            await TryLoad<TargetedManagedAppConfiguration>(CacheKeyTargetedManagedAppConfigurations,
                items => TargetedManagedAppConfigurations = items,
                () => _targetedManagedAppConfigurationsLoaded = true, "targeted managed app configuration(s)");

            await TryLoad<TermsAndConditions>(CacheKeyTermsAndConditions,
                items => TermsAndConditionsCollection = items,
                () => _termsAndConditionsLoaded = true, "terms and conditions item(s)");

            await TryLoad<RoleScopeTag>(CacheKeyScopeTags,
                items => ScopeTags = items,
                () => _scopeTagsLoaded = true, "scope tag(s)");

            await TryLoad<RoleDefinition>(CacheKeyRoleDefinitions,
                items => RoleDefinitions = items,
                () => _roleDefinitionsLoaded = true, "role definition(s)");

            await TryLoad<IntuneBrandingProfile>(CacheKeyIntuneBrandingProfiles,
                items => IntuneBrandingProfiles = items,
                () => _intuneBrandingProfilesLoaded = true, "Intune branding profile(s)");

            await TryLoad<OrganizationalBrandingLocalization>(CacheKeyAzureBrandingLocalizations,
                items => AzureBrandingLocalizations = items,
                () => _azureBrandingLocalizationsLoaded = true, "Azure branding localization(s)");

            await TryLoad<ConditionalAccessPolicy>(CacheKeyConditionalAccess,
                items => ConditionalAccessPolicies = items,
                () => _conditionalAccessLoaded = true, "conditional access policy(ies)");

            await TryLoad<DeviceAndAppManagementAssignmentFilter>(CacheKeyAssignmentFilters,
                items => AssignmentFilters = items,
                () => _assignmentFiltersLoaded = true, "assignment filter(s)");

            await TryLoad<PolicySet>(CacheKeyPolicySets,
                items => PolicySets = items,
                () => _policySetsLoaded = true, "policy set(s)");

            await TryLoad<WindowsAutopilotDeploymentProfile>(CacheKeyAutopilotProfiles,
                items => AutopilotProfiles = items,
                () => _autopilotProfilesLoaded = true, "autopilot profile(s)");

            await TryLoad<DeviceHealthScript>(CacheKeyDeviceHealthScripts,
                items => DeviceHealthScripts = items,
                () => _deviceHealthScriptsLoaded = true, "device health script(s)");

            await TryLoad<DeviceCustomAttributeShellScript>(CacheKeyMacCustomAttributes,
                items => MacCustomAttributes = items,
                () => _macCustomAttributesLoaded = true, "mac custom attribute(s)");

            await TryLoad<WindowsFeatureUpdateProfile>(CacheKeyFeatureUpdateProfiles,
                items => FeatureUpdateProfiles = items,
                () => _featureUpdateProfilesLoaded = true, "feature update profile(s)");

            await TryLoad<NamedLocation>(CacheKeyNamedLocations,
                items => NamedLocations = items,
                () => _namedLocationsLoaded = true, "named location(s)");

            await TryLoad<AuthenticationStrengthPolicy>(CacheKeyAuthenticationStrengths,
                items => AuthenticationStrengthPolicies = items,
                () => _authenticationStrengthPoliciesLoaded = true, "authentication strength policy(ies)");

            await TryLoad<AuthenticationContextClassReference>(CacheKeyAuthenticationContexts,
                items => AuthenticationContextClassReferences = items,
                () => _authenticationContextClassReferencesLoaded = true, "authentication context(s)");

            await TryLoad<Agreement>(CacheKeyTermsOfUseAgreements,
                items => TermsOfUseAgreements = items,
                () => _termsOfUseAgreementsLoaded = true, "terms of use agreement(s)");

            await TryLoad<DeviceManagementScript>(CacheKeyDeviceManagementScripts,
                items => DeviceManagementScripts = items,
                () => _deviceManagementScriptsLoaded = true, "device management script(s)");

            await TryLoad<DeviceShellScript>(CacheKeyDeviceShellScripts,
                items => DeviceShellScripts = items,
                () => _deviceShellScriptsLoaded = true, "device shell script(s)");

            await TryLoad<DeviceComplianceScript>(CacheKeyComplianceScripts,
                items => ComplianceScripts = items,
                () => _complianceScriptsLoaded = true, "compliance script(s)");

            await TryLoad<DepOnboardingSetting>(CacheKeyAppleDepSettings,
                items => AppleDepSettings = items,
                () => _appleDepSettingsLoaded = true, "Apple DEP onboarding setting(s)");

            await TryLoad<DeviceCategory>(CacheKeyDeviceCategories,
                items => DeviceCategories = items,
                () => _deviceCategoriesLoaded = true, "device category(ies)");

            await TryLoad<CloudPcProvisioningPolicy>(CacheKeyCloudPcProvisioningPolicies,
                items => CloudPcProvisioningPolicies = items,
                () => _cloudPcProvisioningPoliciesLoaded = true, "Cloud PC provisioning policy(ies)");

            await TryLoad<CloudPcUserSetting>(CacheKeyCloudPcUserSettings,
                items => CloudPcUserSettings = items,
                () => _cloudPcUserSettingsLoaded = true, "Cloud PC user setting(s)");

            await TryLoad<VppToken>(CacheKeyVppTokens,
                items => VppTokens = items,
                () => _vppTokensLoaded = true, "VPP token(s)");

            await TryLoad<DeviceAndAppManagementRoleAssignment>(CacheKeyRoleAssignments,
                items => RoleAssignments = items,
                () => _roleAssignmentsLoaded = true, "role assignment(s)");

            await TryLoad<WindowsQualityUpdateProfile>(CacheKeyQualityUpdateProfiles,
                items => QualityUpdateProfiles = items,
                () => _qualityUpdateProfilesLoaded = true, "quality update profile(s)");

            await TryLoad<WindowsDriverUpdateProfile>(CacheKeyDriverUpdateProfiles,
                items => DriverUpdateProfiles = items,
                () => _driverUpdateProfilesLoaded = true, "driver update profile(s)");

            await TryLoad<GroupPolicyUploadedDefinitionFile>(CacheKeyAdmxFiles,
                items => AdmxFiles = items,
                () => _admxFilesLoaded = true, "ADMX file(s)");

            await TryLoad<DeviceManagementReusablePolicySetting>(CacheKeyReusablePolicySettings,
                items => ReusablePolicySettings = items,
                () => _reusablePolicySettingsLoaded = true, "reusable policy setting(s)");

            await TryLoad<NotificationMessageTemplate>(CacheKeyNotificationTemplates,
                items => NotificationTemplates = items,
                () => _notificationTemplatesLoaded = true, "notification template(s)");

            if (typesLoaded > 0)
            {
                var totalItems = DeviceConfigurations.Count + CompliancePolicies.Count + Applications.Count + SettingsCatalogPolicies.Count + EndpointSecurityIntents.Count + AdministrativeTemplates.Count + EnrollmentConfigurations.Count + AppProtectionPolicies.Count + ManagedDeviceAppConfigurations.Count + TargetedManagedAppConfigurations.Count + TermsAndConditionsCollection.Count + ScopeTags.Count + RoleDefinitions.Count + IntuneBrandingProfiles.Count + AzureBrandingLocalizations.Count + ConditionalAccessPolicies.Count + AssignmentFilters.Count + PolicySets.Count + AutopilotProfiles.Count + DeviceHealthScripts.Count + MacCustomAttributes.Count + FeatureUpdateProfiles.Count + NamedLocations.Count + AuthenticationStrengthPolicies.Count + AuthenticationContextClassReferences.Count + TermsOfUseAgreements.Count + DeviceManagementScripts.Count + DeviceShellScripts.Count + ComplianceScripts.Count + AppleDepSettings.Count + DeviceCategories.Count + CloudPcProvisioningPolicies.Count + CloudPcUserSettings.Count + VppTokens.Count + RoleAssignments.Count + QualityUpdateProfiles.Count + DriverUpdateProfiles.Count + AdmxFiles.Count + ReusablePolicySettings.Count + NotificationTemplates.Count;
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
                var cachedAssignments = await _cacheService.GetAsync<AppAssignmentRow>(tenantId, CacheKeyAppAssignments);
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
                    (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows,
                    SettingsCatalogPolicies.Count,
                    EndpointSecurityIntents.Count,
                    AdministrativeTemplates.Count,
                    ConditionalAccessPolicies.Count,
                    EnrollmentConfigurations.Count,
                    DeviceManagementScripts.Count + DeviceShellScripts.Count,
                    AppProtectionPolicies.Count);
                DebugLog.Log("Cache", "Updated Overview dashboard from cache");
            }
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to load from cache: {ex.Message}", ex);
        }

        return typesLoaded;
    }

    private async Task<DateTime?> UpdateOldestCacheTimeAsync(DateTime? oldest, string tenantId, string dataType)
    {
        var meta = await _cacheService.GetMetadataAsync(tenantId, dataType);
        if (meta != null)
        {
            if (oldest == null || meta.Value.CachedAt < oldest.Value)
                return meta.Value.CachedAt;
        }
        return oldest;
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
    private async Task SaveToCacheAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId)) return;

        CacheStatusText = $"Cache: {DateTime.Now:MMM dd, h:mm tt}";

        try
        {
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDeviceConfigs, DeviceConfigurations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyCompliancePolicies, CompliancePolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyApplications, Applications);
            await SaveCollectionToCacheAsync(tenantId, CacheKeySettingsCatalog, SettingsCatalogPolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyEndpointSecurity, EndpointSecurityIntents);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAdministrativeTemplates, AdministrativeTemplates);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyEnrollmentConfigurations, EnrollmentConfigurations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAppProtectionPolicies, AppProtectionPolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyManagedDeviceAppConfigurations, ManagedDeviceAppConfigurations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyTargetedManagedAppConfigurations, TargetedManagedAppConfigurations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyTermsAndConditions, TermsAndConditionsCollection);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyScopeTags, ScopeTags);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyRoleDefinitions, RoleDefinitions);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyIntuneBrandingProfiles, IntuneBrandingProfiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAzureBrandingLocalizations, AzureBrandingLocalizations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyConditionalAccess, ConditionalAccessPolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAssignmentFilters, AssignmentFilters);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyPolicySets, PolicySets);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAutopilotProfiles, AutopilotProfiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDeviceHealthScripts, DeviceHealthScripts);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyMacCustomAttributes, MacCustomAttributes);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyFeatureUpdateProfiles, FeatureUpdateProfiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyNamedLocations, NamedLocations);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAuthenticationStrengths, AuthenticationStrengthPolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAuthenticationContexts, AuthenticationContextClassReferences);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyTermsOfUseAgreements, TermsOfUseAgreements);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDeviceManagementScripts, DeviceManagementScripts);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDeviceShellScripts, DeviceShellScripts);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyComplianceScripts, ComplianceScripts);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAppleDepSettings, AppleDepSettings);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDeviceCategories, DeviceCategories);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyCloudPcProvisioningPolicies, CloudPcProvisioningPolicies);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyCloudPcUserSettings, CloudPcUserSettings);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyVppTokens, VppTokens);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyRoleAssignments, RoleAssignments);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyQualityUpdateProfiles, QualityUpdateProfiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyDriverUpdateProfiles, DriverUpdateProfiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyAdmxFiles, AdmxFiles);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyReusablePolicySettings, ReusablePolicySettings);
            await SaveCollectionToCacheAsync(tenantId, CacheKeyNotificationTemplates, NotificationTemplates);

            DebugLog.Log("Cache", "Saved data to disk cache");
        }
        catch (Exception ex)
        {
            DebugLog.LogError($"Failed to save to cache: {ex.Message}", ex);
        }
    }

    // ─── Download All to Cache ─────────────────────────────────────────────

    [RelayCommand]
    private async Task DownloadAllToCacheAsync(CancellationToken cancellationToken)
    {
        if (!IsConnected || IsDownloadingAll) return;

        var tenantId = ActiveProfile?.TenantId;
        if (string.IsNullOrEmpty(tenantId)) return;

        IsDownloadingAll = true;
        DownloadProgress = "Preparing download...";
        DownloadProgressPercent = 0;
        ClearError();

        _downloadAllCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _downloadAllCts.Token;

        var completed = 0;
        var failed = 0;
        var errors = new List<string>();

        // Build the 32-type task list
        var downloadTasks = BuildDownloadTaskList(tenantId, ct);
        var total = downloadTasks.Count;

        DebugLog.Log("DownloadAll", $"Starting download of {total} data types (parallel=5)");

        try
        {
            using var semaphore = new SemaphoreSlim(5, 5);

            var tasks = downloadTasks.Select(async entry =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    await entry.Action();

                    var current = Interlocked.Increment(ref completed);
                    Dispatcher.UIThread.Post(() =>
                    {
                        DownloadProgress = $"Downloading {current + failed} of {total}...";
                        DownloadProgressPercent = ((current + failed) / (double)total) * 100;
                    });
                    DebugLog.Log("DownloadAll", $"Completed: {entry.Name} ({current}/{total})");
                }
                catch (OperationCanceledException)
                {
                    throw; // Let cancellation propagate
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    var detail = FormatGraphError(ex);
                    DebugLog.LogError($"Failed to download {entry.Name}: {detail}", ex);
                    lock (errors) { errors.Add($"{entry.Name}: {detail}"); }

                    var current = Interlocked.Add(ref completed, 0);
                    Dispatcher.UIThread.Post(() =>
                    {
                        DownloadProgress = $"Downloading {current + failed} of {total}...";
                        DownloadProgressPercent = ((current + failed) / (double)total) * 100;
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            ApplyFilter();

            if (errors.Count > 0)
            {
                DownloadProgress = $"Completed with {errors.Count} error(s) — {completed} of {total} succeeded";
                SetError($"Some downloads failed: {string.Join("; ", errors.Take(5))}");
            }
            else
            {
                DownloadProgress = $"Downloaded all {total} data types";
            }
            DownloadProgressPercent = 100;

            DebugLog.Log("DownloadAll", $"Finished: {completed} succeeded, {failed} failed");
        }
        catch (OperationCanceledException)
        {
            DownloadProgress = $"Cancelled — {completed} of {total} completed";
            DebugLog.Log("DownloadAll", $"Cancelled after {completed} of {total}");
        }
        catch (Exception ex)
        {
            DownloadProgress = $"Error — {completed} of {total} completed";
            SetError($"Download failed: {FormatGraphError(ex)}");
            DebugLog.LogError($"Download all failed: {FormatGraphError(ex)}", ex);
        }
        finally
        {
            IsDownloadingAll = false;
            _downloadAllCts?.Dispose();
            _downloadAllCts = null;
        }
    }

    [RelayCommand]
    private void CancelDownloadAll()
    {
        _downloadAllCts?.Cancel();
        DebugLog.Log("DownloadAll", "Cancel requested by user");
    }

    private record struct DownloadTask(string Name, Func<Task> Action);

    private List<DownloadTask> BuildDownloadTaskList(string tenantId, CancellationToken ct)
    {
        var tasks = new List<DownloadTask>();

        // Helper to add a simple service→collection→cache task
        void AddTask<T>(string name, object? service, Func<CancellationToken, Task<List<T>>> fetch,
            Action<ObservableCollection<T>> setCollection, Action? setLoadedFlag, string cacheKey)
        {
            if (service == null) return;
            tasks.Add(new DownloadTask(name, async () =>
            {
                var items = await fetch(ct);
                Dispatcher.UIThread.Post(() =>
                {
                    setCollection(new ObservableCollection<T>(items));
                    setLoadedFlag?.Invoke();
                });
                await _cacheService.SetAsync(tenantId, cacheKey, items);
            }));
        }

        // --- 4 core types ---
        AddTask("Device Configurations", _configProfileService,
            c => _configProfileService!.ListDeviceConfigurationsAsync(c),
            items => DeviceConfigurations = items, null, CacheKeyDeviceConfigs);

        AddTask("Compliance Policies", _compliancePolicyService,
            c => _compliancePolicyService!.ListCompliancePoliciesAsync(c),
            items => CompliancePolicies = items, null, CacheKeyCompliancePolicies);

        AddTask("Applications", _applicationService,
            c => _applicationService!.ListApplicationsAsync(c),
            items => Applications = items, null, CacheKeyApplications);

        AddTask("Settings Catalog", _settingsCatalogService,
            c => _settingsCatalogService!.ListSettingsCatalogPoliciesAsync(c),
            items => SettingsCatalogPolicies = items, null, CacheKeySettingsCatalog);

        // --- 25 lazy types ---
        AddTask("Conditional Access", _conditionalAccessPolicyService,
            c => _conditionalAccessPolicyService!.ListPoliciesAsync(c),
            items => ConditionalAccessPolicies = items,
            () => _conditionalAccessLoaded = true, CacheKeyConditionalAccess);

        AddTask("Assignment Filters", _assignmentFilterService,
            c => _assignmentFilterService!.ListFiltersAsync(c),
            items => AssignmentFilters = items,
            () => _assignmentFiltersLoaded = true, CacheKeyAssignmentFilters);

        AddTask("Policy Sets", _policySetService,
            c => _policySetService!.ListPolicySetsAsync(c),
            items => PolicySets = items,
            () => _policySetsLoaded = true, CacheKeyPolicySets);

        AddTask("Endpoint Security", _endpointSecurityService,
            c => _endpointSecurityService!.ListEndpointSecurityIntentsAsync(c),
            items => EndpointSecurityIntents = items,
            () => _endpointSecurityLoaded = true, CacheKeyEndpointSecurity);

        AddTask("Administrative Templates", _administrativeTemplateService,
            c => _administrativeTemplateService!.ListAdministrativeTemplatesAsync(c),
            items => AdministrativeTemplates = items,
            () => _administrativeTemplatesLoaded = true, CacheKeyAdministrativeTemplates);

        AddTask("Enrollment Configurations", _enrollmentConfigurationService,
            c => _enrollmentConfigurationService!.ListEnrollmentConfigurationsAsync(c),
            items => EnrollmentConfigurations = items,
            () => _enrollmentConfigurationsLoaded = true, CacheKeyEnrollmentConfigurations);

        AddTask("App Protection Policies", _appProtectionPolicyService,
            c => _appProtectionPolicyService!.ListAppProtectionPoliciesAsync(c),
            items => AppProtectionPolicies = items,
            () => _appProtectionPoliciesLoaded = true, CacheKeyAppProtectionPolicies);

        AddTask("Managed Device App Configurations", _managedAppConfigurationService,
            c => _managedAppConfigurationService!.ListManagedDeviceAppConfigurationsAsync(c),
            items => ManagedDeviceAppConfigurations = items,
            () => _managedDeviceAppConfigurationsLoaded = true, CacheKeyManagedDeviceAppConfigurations);

        AddTask("Targeted Managed App Configurations", _managedAppConfigurationService,
            c => _managedAppConfigurationService!.ListTargetedManagedAppConfigurationsAsync(c),
            items => TargetedManagedAppConfigurations = items,
            () => _targetedManagedAppConfigurationsLoaded = true, CacheKeyTargetedManagedAppConfigurations);

        AddTask("Terms and Conditions", _termsAndConditionsService,
            c => _termsAndConditionsService!.ListTermsAndConditionsAsync(c),
            items => TermsAndConditionsCollection = items,
            () => _termsAndConditionsLoaded = true, CacheKeyTermsAndConditions);

        AddTask("Scope Tags", _scopeTagService,
            c => _scopeTagService!.ListScopeTagsAsync(c),
            items => ScopeTags = items,
            () => _scopeTagsLoaded = true, CacheKeyScopeTags);

        AddTask("Role Definitions", _roleDefinitionService,
            c => _roleDefinitionService!.ListRoleDefinitionsAsync(c),
            items => RoleDefinitions = items,
            () => _roleDefinitionsLoaded = true, CacheKeyRoleDefinitions);

        AddTask("Intune Branding Profiles", _intuneBrandingService,
            c => _intuneBrandingService!.ListIntuneBrandingProfilesAsync(c),
            items => IntuneBrandingProfiles = items,
            () => _intuneBrandingProfilesLoaded = true, CacheKeyIntuneBrandingProfiles);

        AddTask("Azure Branding Localizations", _azureBrandingService,
            c => _azureBrandingService!.ListBrandingLocalizationsAsync(c),
            items => AzureBrandingLocalizations = items,
            () => _azureBrandingLocalizationsLoaded = true, CacheKeyAzureBrandingLocalizations);

        AddTask("Autopilot Profiles", _autopilotService,
            c => _autopilotService!.ListAutopilotProfilesAsync(c),
            items => AutopilotProfiles = items,
            () => _autopilotProfilesLoaded = true, CacheKeyAutopilotProfiles);

        AddTask("Device Health Scripts", _deviceHealthScriptService,
            c => _deviceHealthScriptService!.ListDeviceHealthScriptsAsync(c),
            items => DeviceHealthScripts = items,
            () => _deviceHealthScriptsLoaded = true, CacheKeyDeviceHealthScripts);

        AddTask("Mac Custom Attributes", _macCustomAttributeService,
            c => _macCustomAttributeService!.ListMacCustomAttributesAsync(c),
            items => MacCustomAttributes = items,
            () => _macCustomAttributesLoaded = true, CacheKeyMacCustomAttributes);

        AddTask("Feature Update Profiles", _featureUpdateProfileService,
            c => _featureUpdateProfileService!.ListFeatureUpdateProfilesAsync(c),
            items => FeatureUpdateProfiles = items,
            () => _featureUpdateProfilesLoaded = true, CacheKeyFeatureUpdateProfiles);

        AddTask("Named Locations", _namedLocationService,
            c => _namedLocationService!.ListNamedLocationsAsync(c),
            items => NamedLocations = items,
            () => _namedLocationsLoaded = true, CacheKeyNamedLocations);

        AddTask("Authentication Strength Policies", _authenticationStrengthService,
            c => _authenticationStrengthService!.ListAuthenticationStrengthPoliciesAsync(c),
            items => AuthenticationStrengthPolicies = items,
            () => _authenticationStrengthPoliciesLoaded = true, CacheKeyAuthenticationStrengths);

        AddTask("Authentication Contexts", _authenticationContextService,
            c => _authenticationContextService!.ListAuthenticationContextsAsync(c),
            items => AuthenticationContextClassReferences = items,
            () => _authenticationContextClassReferencesLoaded = true, CacheKeyAuthenticationContexts);

        AddTask("Terms of Use Agreements", _termsOfUseService,
            c => _termsOfUseService!.ListTermsOfUseAgreementsAsync(c),
            items => TermsOfUseAgreements = items,
            () => _termsOfUseAgreementsLoaded = true, CacheKeyTermsOfUseAgreements);

        AddTask("Device Management Scripts", _deviceManagementScriptService,
            c => _deviceManagementScriptService!.ListDeviceManagementScriptsAsync(c),
            items => DeviceManagementScripts = items,
            () => _deviceManagementScriptsLoaded = true, CacheKeyDeviceManagementScripts);

        AddTask("Device Shell Scripts", _deviceShellScriptService,
            c => _deviceShellScriptService!.ListDeviceShellScriptsAsync(c),
            items => DeviceShellScripts = items,
            () => _deviceShellScriptsLoaded = true, CacheKeyDeviceShellScripts);

        AddTask("Compliance Scripts", _complianceScriptService,
            c => _complianceScriptService!.ListComplianceScriptsAsync(c),
            items => ComplianceScripts = items,
            () => _complianceScriptsLoaded = true, CacheKeyComplianceScripts);

        AddTask("Apple DEP", _appleDepService,
            c => _appleDepService!.ListDepOnboardingSettingsAsync(c),
            items => AppleDepSettings = items,
            () => _appleDepSettingsLoaded = true, CacheKeyAppleDepSettings);

        AddTask("Device Categories", _deviceCategoryService,
            c => _deviceCategoryService!.ListDeviceCategoriesAsync(c),
            items => DeviceCategories = items,
            () => _deviceCategoriesLoaded = true, CacheKeyDeviceCategories);

        AddTask("Cloud PC Provisioning Policies", _cloudPcProvisioningService,
            c => _cloudPcProvisioningService!.ListProvisioningPoliciesAsync(c),
            items => CloudPcProvisioningPolicies = items,
            () => _cloudPcProvisioningPoliciesLoaded = true, CacheKeyCloudPcProvisioningPolicies);

        AddTask("Cloud PC User Settings", _cloudPcUserSettingsService,
            c => _cloudPcUserSettingsService!.ListUserSettingsAsync(c),
            items => CloudPcUserSettings = items,
            () => _cloudPcUserSettingsLoaded = true, CacheKeyCloudPcUserSettings);

        AddTask("VPP Tokens", _vppTokenService,
            c => _vppTokenService!.ListVppTokensAsync(c),
            items => VppTokens = items,
            () => _vppTokensLoaded = true, CacheKeyVppTokens);

        AddTask("Role Assignments", _roleDefinitionService,
            c => _roleDefinitionService!.GetRoleAssignmentsAsync(c),
            items => RoleAssignments = items,
            () => _roleAssignmentsLoaded = true, CacheKeyRoleAssignments);

        AddTask("Quality Update Profiles", _qualityUpdateProfileService,
            c => _qualityUpdateProfileService!.ListQualityUpdateProfilesAsync(c),
            items => QualityUpdateProfiles = items,
            () => _qualityUpdateProfilesLoaded = true, CacheKeyQualityUpdateProfiles);

        AddTask("Driver Update Profiles", _driverUpdateProfileService,
            c => _driverUpdateProfileService!.ListDriverUpdateProfilesAsync(c),
            items => DriverUpdateProfiles = items,
            () => _driverUpdateProfilesLoaded = true, CacheKeyDriverUpdateProfiles);
        AddTask("ADMX Files", _admxFileService,
            c => _admxFileService!.ListAdmxFilesAsync(c),
            items => AdmxFiles = items,
            () => _admxFilesLoaded = true, CacheKeyAdmxFiles);

        AddTask("Reusable Policy Settings", _reusablePolicySettingService,
            c => _reusablePolicySettingService!.ListReusablePolicySettingsAsync(c),
            items => ReusablePolicySettings = items,
            () => _reusablePolicySettingsLoaded = true, CacheKeyReusablePolicySettings);

        AddTask("Notification Templates", _notificationTemplateService,
            c => _notificationTemplateService!.ListNotificationTemplatesAsync(c),
            items => NotificationTemplates = items,
            () => _notificationTemplatesLoaded = true, CacheKeyNotificationTemplates);

        // --- 2 group types (special: require member-count enrichment) ---
        if (_groupService != null)
        {
            tasks.Add(new DownloadTask("Dynamic Groups", async () =>
            {
                var groups = await _groupService.ListDynamicGroupsAsync(ct);
                var rows = await EnrichGroupRowsAsync(_groupService, groups, ct);
                Dispatcher.UIThread.Post(() =>
                {
                    DynamicGroupRows = new ObservableCollection<GroupRow>(rows);
                    _dynamicGroupsLoaded = true;
                });
                await _cacheService.SetAsync(tenantId, CacheKeyDynamicGroups, rows);
            }));

            tasks.Add(new DownloadTask("Assigned Groups", async () =>
            {
                var groups = await _groupService.ListAssignedGroupsAsync(ct);
                var rows = await EnrichGroupRowsAsync(_groupService, groups, ct);
                Dispatcher.UIThread.Post(() =>
                {
                    AssignedGroupRows = new ObservableCollection<GroupRow>(rows);
                    _assignedGroupsLoaded = true;
                });
                await _cacheService.SetAsync(tenantId, CacheKeyAssignedGroups, rows);
            }));
        }

        // --- 1 user type (cache-only, no UI tab) ---
        if (_userService != null)
        {
            tasks.Add(new DownloadTask("Users", async () =>
            {
                var users = await _userService.ListUsersAsync(ct);
                await _cacheService.SetAsync(tenantId, CacheKeyUsers, users);
            }));
        }

        // --- Devices & Users (managed devices + Entra users → correlated view) ---
        if (_managedDeviceService != null && _entraUserService != null)
        {
            tasks.Add(new DownloadTask("Managed Devices & Entra Users", async () =>
            {
                var devices = await _managedDeviceService.ListManagedDevicesAsync(ct);
                await _cacheService.SetAsync(tenantId, CacheKeyManagedDevices, devices);

                var entraUsers = await _entraUserService.ListUsersAsync(ct);
                await _cacheService.SetAsync(tenantId, CacheKeyEntraUsers, entraUsers);

                var entries = BuildDeviceUserEntries(devices, entraUsers);
                Dispatcher.UIThread.Post(() =>
                {
                    DeviceUserEntries = new ObservableCollection<DeviceUserEntry>(entries);
                    _deviceUserEntriesLoaded = true;
                    ApplyFilter();
                });
            }));
        }

        return tasks;
    }

    /// <summary>
    /// Enriches a list of groups with member counts to produce GroupRow objects.
    /// Uses SemaphoreSlim(5) internally for parallel member-count lookups.
    /// </summary>
    private static async Task<List<GroupRow>> EnrichGroupRowsAsync(
        IGroupService groupService,
        List<Microsoft.Graph.Beta.Models.Group> groups,
        CancellationToken ct)
    {
        var rows = new List<GroupRow>();
        using var semaphore = new SemaphoreSlim(5, 5);

        var tasks = groups.Select(async group =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var counts = group.Id != null
                    ? await groupService.GetMemberCountsAsync(group.Id, ct)
                    : new GroupMemberCounts(0, 0, 0, 0);
                var row = BuildGroupRow(group, counts);
                lock (rows) { rows.Add(row); }
            }
            finally { semaphore.Release(); }
        }).ToList();

        await Task.WhenAll(tasks);
        rows.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase));
        return rows;
    }
}
