using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ManagedDeviceAppConfigurationBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKeyConfigurations = "ManagedDeviceAppConfigurations";
    private const string CacheKeyList = "ManagedDeviceAppConfigurations_List";
    private const string CacheKeyDetail = "ManagedDeviceAppConfigurations_Detail";

    private IManagedAppConfigurationService? _service;

    public ManagedDeviceAppConfigurationBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IManagedAppConfigurationService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new ManagedAppConfigurationService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        if (tenantId is not null)
        {
            var cachedList = _cache.Get<ManagedDeviceAppConfigurationListItemDto>(tenantId, CacheKeyList);
            if (cachedList is { Count: > 0 })
                return cachedList.ToArray();
        }

        var service = GetService();
        var items = await GroupResolutionHelper.GetCachedOrFetchAsync(
            _cache,
            tenantId,
            CacheKeyConfigurations,
            () => service.ListManagedDeviceAppConfigurationsAsync());

        var mapped = await MapListAsync(items);
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyList, mapped.ToList());

        return mapped;
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var tenantId = GetTenantId();
        var service = GetService();
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");

        if (tenantId is not null)
        {
            var cached = _cache.GetSingle<ManagedDeviceAppConfigurationDetailDto>(tenantId, $"{CacheKeyDetail}_{id}");
            if (cached is not null)
                return cached;
        }

        var configuration = await service.GetManagedDeviceAppConfigurationAsync(id)
            ?? throw new InvalidOperationException($"Managed device app configuration {id} not found");

        var assignmentsResponse = await client.DeviceAppManagement.MobileAppConfigurations[id].Assignments.GetAsync();
        var assignments = assignmentsResponse?.Value ?? [];
        var targets = assignments.Select(assignment => assignment.Target).ToList();
        var groupNames = await GroupResolutionHelper.ResolveGroupNamesAsync(targets, client);

        var detail = new ManagedDeviceAppConfigurationDetailDto(
            Id: configuration.Id ?? "",
            DisplayName: configuration.DisplayName ?? "",
            Description: configuration.Description,
            ConfigurationType: ApplicationDataMapper.FormatTypeName(configuration.GetType().Name),
            OdataType: configuration.OdataType ?? configuration.GetType().Name,
            Version: configuration.Version?.ToString() ?? "",
            CreatedDateTime: configuration.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: configuration.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (configuration.RoleScopeTagIds ?? []).ToArray(),
            TargetedMobileApps: (configuration.TargetedMobileApps ?? []).ToArray(),
            Assignments: GroupResolutionHelper.MapAssignments(targets, groupNames));

        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyDetail}_{id}", detail);

        return detail;
    }

    private async Task<ManagedDeviceAppConfigurationListItemDto[]> MapListAsync(
        List<ManagedDeviceMobileAppConfiguration> configurations)
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        using var semaphore = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();

        var tasks = configurations.Where(configuration => configuration.Id is not null).Select(async configuration =>
        {
            await semaphore.WaitAsync();
            try
            {
                var response = await client.DeviceAppManagement.MobileAppConfigurations[configuration.Id!]
                    .Assignments.GetAsync();
                lock (counts)
                {
                    counts[configuration.Id!] = response?.Value?.Count ?? 0;
                }
            }
            catch
            {
                lock (counts)
                {
                    counts[configuration.Id!] = 0;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return configurations
            .Select(configuration => new ManagedDeviceAppConfigurationListItemDto(
                Id: configuration.Id ?? "",
                DisplayName: configuration.DisplayName ?? "",
                Description: configuration.Description,
                ConfigurationType: ApplicationDataMapper.FormatTypeName(configuration.GetType().Name),
                Version: configuration.Version?.ToString() ?? "",
                CreatedDateTime: configuration.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: configuration.LastModifiedDateTime?.ToString("o") ?? "",
                TargetedMobileAppCount: configuration.TargetedMobileApps?.Count ?? 0,
                AssignmentCount: counts.GetValueOrDefault(configuration.Id ?? "", 0)))
            .ToArray();
    }
}
