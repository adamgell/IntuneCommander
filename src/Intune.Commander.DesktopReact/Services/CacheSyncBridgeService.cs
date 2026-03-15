using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Bridge;
using Microsoft.Graph.Beta;

namespace Intune.Commander.DesktopReact.Services;

public class CacheSyncBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private IBridgeService? _bridge;

    /// <summary>
    /// Registry of syncable data types: (CacheKey, Label, FetchFunc factory).
    /// The factory receives a GraphServiceClient and returns a Func that fetches + caches the data.
    /// </summary>
    private static readonly (string CacheKey, string Label, Func<GraphServiceClient, string, ICacheService, Func<Task>> Factory)[] SyncableTypes =
    [
        ("SettingsCatalog", "Settings Catalog", (client, tenantId, cache) => async () =>
        {
            var svc = new SettingsCatalogService(client);
            var items = await svc.ListSettingsCatalogPoliciesAsync(CancellationToken.None);
            cache.Set(tenantId, "SettingsCatalog", items);
        }),
        ("DeviceConfigurations", "Device Configurations", (client, tenantId, cache) => async () =>
        {
            var svc = new ConfigurationProfileService(client);
            var items = await svc.ListDeviceConfigurationsAsync(CancellationToken.None);
            cache.Set(tenantId, "DeviceConfigurations", items);
        }),
        ("CompliancePolicies", "Compliance Policies", (client, tenantId, cache) => async () =>
        {
            var svc = new CompliancePolicyService(client);
            var items = await svc.ListCompliancePoliciesAsync(CancellationToken.None);
            cache.Set(tenantId, "CompliancePolicies", items);
        }),
        ("Applications", "Applications", (client, tenantId, cache) => async () =>
        {
            var svc = new ApplicationService(client);
            var items = await svc.ListApplicationsAsync(CancellationToken.None);
            cache.Set(tenantId, "Applications", items);
        }),
        ("ConditionalAccessPolicies", "Conditional Access", (client, tenantId, cache) => async () =>
        {
            var svc = new ConditionalAccessPolicyService(client);
            var items = await svc.ListPoliciesAsync(CancellationToken.None);
            cache.Set(tenantId, "ConditionalAccessPolicies", items);
        }),
        ("AssignmentFilters", "Assignment Filters", (client, tenantId, cache) => async () =>
        {
            var svc = new AssignmentFilterService(client);
            var items = await svc.ListFiltersAsync(CancellationToken.None);
            cache.Set(tenantId, "AssignmentFilters", items);
        }),
        ("EndpointSecurityIntents", "Endpoint Security", (client, tenantId, cache) => async () =>
        {
            var svc = new EndpointSecurityService(client);
            var items = await svc.ListEndpointSecurityIntentsAsync(CancellationToken.None);
            cache.Set(tenantId, "EndpointSecurityIntents", items);
        }),
        ("AdministrativeTemplates", "Administrative Templates", (client, tenantId, cache) => async () =>
        {
            var svc = new AdministrativeTemplateService(client);
            var items = await svc.ListAdministrativeTemplatesAsync(CancellationToken.None);
            cache.Set(tenantId, "AdministrativeTemplates", items);
        }),
        ("EnrollmentConfigurations", "Enrollment Configurations", (client, tenantId, cache) => async () =>
        {
            var svc = new EnrollmentConfigurationService(client);
            var items = await svc.ListEnrollmentConfigurationsAsync(CancellationToken.None);
            cache.Set(tenantId, "EnrollmentConfigurations", items);
        }),
        ("AppProtectionPolicies", "App Protection Policies", (client, tenantId, cache) => async () =>
        {
            var svc = new AppProtectionPolicyService(client);
            var items = await svc.ListAppProtectionPoliciesAsync(CancellationToken.None);
            cache.Set(tenantId, "AppProtectionPolicies", items);
        }),
        ("AutopilotProfiles", "Autopilot Profiles", (client, tenantId, cache) => async () =>
        {
            var svc = new AutopilotService(client);
            var items = await svc.ListAutopilotProfilesAsync(CancellationToken.None);
            cache.Set(tenantId, "AutopilotProfiles", items);
        }),
        ("DeviceHealthScripts", "Device Health Scripts", (client, tenantId, cache) => async () =>
        {
            var svc = new DeviceHealthScriptService(client);
            var items = await svc.ListDeviceHealthScriptsAsync(CancellationToken.None);
            cache.Set(tenantId, "DeviceHealthScripts", items);
        }),
        ("DeviceManagementScripts", "Device Scripts", (client, tenantId, cache) => async () =>
        {
            var svc = new DeviceManagementScriptService(client);
            var items = await svc.ListDeviceManagementScriptsAsync(CancellationToken.None);
            cache.Set(tenantId, "DeviceManagementScripts", items);
        }),
        ("DeviceShellScripts", "Shell Scripts", (client, tenantId, cache) => async () =>
        {
            var svc = new DeviceShellScriptService(client);
            var items = await svc.ListDeviceShellScriptsAsync(CancellationToken.None);
            cache.Set(tenantId, "DeviceShellScripts", items);
        }),
        ("ComplianceScripts", "Compliance Scripts", (client, tenantId, cache) => async () =>
        {
            var svc = new ComplianceScriptService(client);
            var items = await svc.ListComplianceScriptsAsync(CancellationToken.None);
            cache.Set(tenantId, "ComplianceScripts", items);
        }),
        ("FeatureUpdateProfiles", "Feature Updates", (client, tenantId, cache) => async () =>
        {
            var svc = new FeatureUpdateProfileService(client);
            var items = await svc.ListFeatureUpdateProfilesAsync(CancellationToken.None);
            cache.Set(tenantId, "FeatureUpdateProfiles", items);
        }),
        ("QualityUpdateProfiles", "Quality Updates", (client, tenantId, cache) => async () =>
        {
            var svc = new QualityUpdateProfileService(client);
            var items = await svc.ListQualityUpdateProfilesAsync(CancellationToken.None);
            cache.Set(tenantId, "QualityUpdateProfiles", items);
        }),
        ("NamedLocations", "Named Locations", (client, tenantId, cache) => async () =>
        {
            var svc = new NamedLocationService(client);
            var items = await svc.ListNamedLocationsAsync(CancellationToken.None);
            cache.Set(tenantId, "NamedLocations", items);
        }),
        ("ScopeTags", "Scope Tags", (client, tenantId, cache) => async () =>
        {
            var svc = new ScopeTagService(client);
            var items = await svc.ListScopeTagsAsync(CancellationToken.None);
            cache.Set(tenantId, "ScopeTags", items);
        }),
        ("RoleDefinitions", "Role Definitions", (client, tenantId, cache) => async () =>
        {
            var svc = new RoleDefinitionService(client);
            var items = await svc.ListRoleDefinitionsAsync(CancellationToken.None);
            cache.Set(tenantId, "RoleDefinitions", items);
        }),
        ("TermsAndConditions", "Terms and Conditions", (client, tenantId, cache) => async () =>
        {
            var svc = new TermsAndConditionsService(client);
            var items = await svc.ListTermsAndConditionsAsync(CancellationToken.None);
            cache.Set(tenantId, "TermsAndConditions", items);
        }),
        ("NotificationTemplates", "Notification Templates", (client, tenantId, cache) => async () =>
        {
            var svc = new NotificationTemplateService(client);
            var items = await svc.ListNotificationTemplatesAsync(CancellationToken.None);
            cache.Set(tenantId, "NotificationTemplates", items);
        }),
        ("DeviceCategories", "Device Categories", (client, tenantId, cache) => async () =>
        {
            var svc = new DeviceCategoryService(client);
            var items = await svc.ListDeviceCategoriesAsync(CancellationToken.None);
            cache.Set(tenantId, "DeviceCategories", items);
        }),
        ("ManagedDevices", "Managed Devices", (client, tenantId, cache) => async () =>
        {
            var svc = new DeviceService(client);
            var items = await svc.ListAllDevicesAsync(CancellationToken.None);
            cache.Set(tenantId, "ManagedDevices", items);
        }),
    ];

    public CacheSyncBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    public void SetBridge(IBridgeService bridge) => _bridge = bridge;

    /// <summary>
    /// Fetches all Intune data types from Graph API in parallel batches and stores them in cache.
    /// Sends cache.syncProgress events via the bridge as each type completes.
    /// </summary>
    public async Task<object> SyncAllAsync(JsonElement? payload = null)
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        var tenantId = _shellState.ActiveProfile?.TenantId
            ?? throw new InvalidOperationException("No active tenant — connect first");

        var total = SyncableTypes.Length;
        var successCount = 0;
        var errorCount = 0;
        var errors = new List<object>();
        var completed = 0;

        // Process in batches of 5 to avoid Graph API throttling
        const int batchSize = 5;

        for (var batchStart = 0; batchStart < total; batchStart += batchSize)
        {
            var batch = SyncableTypes
                .Skip(batchStart)
                .Take(batchSize)
                .ToArray();

            var tasks = batch.Select(async entry =>
            {
                var (cacheKey, label, factory) = entry;

                if (_bridge is not null) await _bridge.SendEventAsync("cache.syncProgress", new
                {
                    current = Interlocked.Increment(ref completed),
                    total,
                    label,
                    status = "loading"
                });

                try
                {
                    var fetchAction = factory(client, tenantId, _cache);
                    await fetchAction();

                    Interlocked.Increment(ref successCount);

                    if (_bridge is not null) await _bridge.SendEventAsync("cache.syncProgress", new
                    {
                        current = completed,
                        total,
                        label,
                        status = "done"
                    });
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);

                    lock (errors)
                    {
                        errors.Add(new { cacheKey, label, error = ex.Message });
                    }

                    if (_bridge is not null) await _bridge.SendEventAsync("cache.syncProgress", new
                    {
                        current = completed,
                        total,
                        label,
                        status = "error"
                    });
                }
            });

            await Task.WhenAll(tasks);
        }

        if (_bridge is not null) await _bridge.SendEventAsync("cache.syncProgress", new
        {
            current = total,
            total,
            label = "Sync complete",
            status = "complete"
        });

        return new
        {
            totalTypes = total,
            successCount,
            errorCount,
            errors = errors.ToArray()
        };
    }

    /// <summary>
    /// Returns cache metadata for all syncable data types for the current tenant.
    /// </summary>
    public Task<object> GetStatusAsync(JsonElement? payload = null)
    {
        var tenantId = _shellState.ActiveProfile?.TenantId;
        if (tenantId is null)
            return Task.FromResult<object>(Array.Empty<object>());

        var status = SyncableTypes.Select(entry =>
        {
            var meta = _cache.GetMetadata(tenantId, entry.CacheKey);
            return new
            {
                cacheKey = entry.CacheKey,
                label = entry.Label,
                isCached = meta is not null,
                cachedAt = meta?.CachedAt.ToString("o"),
                itemCount = meta?.ItemCount ?? 0
            };
        }).ToArray();

        return Task.FromResult<object>(status);
    }

    /// <summary>
    /// Invalidates all cache entries for the current tenant.
    /// </summary>
    public Task<object> InvalidateAsync(JsonElement? payload = null)
    {
        var tenantId = _shellState.ActiveProfile?.TenantId
            ?? throw new InvalidOperationException("No active tenant — connect first");

        _cache.Invalidate(tenantId);

        return Task.FromResult<object>(new { success = true, tenantId });
    }
}
