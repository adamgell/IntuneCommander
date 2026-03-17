using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class TargetedManagedAppConfigurationBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKeyConfigurations = "TargetedManagedAppConfigurations";
    private const string CacheKeyList = "TargetedManagedAppConfigurations_List";
    private const string CacheKeyDetail = "TargetedManagedAppConfigurations_Detail";

    private IManagedAppConfigurationService? _service;

    public TargetedManagedAppConfigurationBridgeService(
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
            var cachedList = _cache.Get<TargetedManagedAppConfigurationListItemDto>(tenantId, CacheKeyList);
            if (cachedList is { Count: > 0 })
                return cachedList.ToArray();
        }

        var service = GetService();
        var items = await GroupResolutionHelper.GetCachedOrFetchAsync(
            _cache,
            tenantId,
            CacheKeyConfigurations,
            () => service.ListTargetedManagedAppConfigurationsAsync());

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
            var cached = _cache.GetSingle<TargetedManagedAppConfigurationDetailDto>(tenantId, $"{CacheKeyDetail}_{id}");
            if (cached is not null)
                return cached;
        }

        var configuration = await service.GetTargetedManagedAppConfigurationAsync(id)
            ?? throw new InvalidOperationException($"Targeted managed app configuration {id} not found");

        var assignmentsResponse = await client.DeviceAppManagement.TargetedManagedAppConfigurations[id].Assignments.GetAsync();
        var assignments = assignmentsResponse?.Value ?? [];
        var targets = assignments.Select(assignment => assignment.Target).ToList();
        var groupNames = await GroupResolutionHelper.ResolveGroupNamesAsync(targets, client);

        var detail = new TargetedManagedAppConfigurationDetailDto(
            Id: configuration.Id ?? "",
            DisplayName: configuration.DisplayName ?? "",
            Description: configuration.Description,
            ConfigurationType: ApplicationDataMapper.FormatTypeName(configuration.GetType().Name),
            OdataType: configuration.OdataType ?? configuration.GetType().Name,
            Version: configuration.Version?.ToString() ?? "",
            AppGroupType: configuration.AppGroupType?.ToString() ?? "",
            IsAssigned: configuration.IsAssigned ?? false,
            DeployedAppCount: configuration.DeployedAppCount ?? 0,
            CreatedDateTime: configuration.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: configuration.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (configuration.RoleScopeTagIds ?? []).ToArray(),
            Assignments: GroupResolutionHelper.MapAssignments(targets, groupNames));

        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyDetail}_{id}", detail);

        return detail;
    }

    private async Task<TargetedManagedAppConfigurationListItemDto[]> MapListAsync(
        List<TargetedManagedAppConfiguration> configurations)
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        using var semaphore = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();

        var tasks = configurations.Where(configuration => configuration.Id is not null).Select(async configuration =>
        {
            await semaphore.WaitAsync();
            try
            {
                var response = await client.DeviceAppManagement.TargetedManagedAppConfigurations[configuration.Id!]
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
            .Select(configuration => new TargetedManagedAppConfigurationListItemDto(
                Id: configuration.Id ?? "",
                DisplayName: configuration.DisplayName ?? "",
                Description: configuration.Description,
                ConfigurationType: ApplicationDataMapper.FormatTypeName(configuration.GetType().Name),
                Version: configuration.Version?.ToString() ?? "",
                AppGroupType: configuration.AppGroupType?.ToString() ?? "",
                IsAssigned: configuration.IsAssigned ?? false,
                DeployedAppCount: configuration.DeployedAppCount ?? 0,
                CreatedDateTime: configuration.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: configuration.LastModifiedDateTime?.ToString("o") ?? "",
                AssignmentCount: counts.GetValueOrDefault(configuration.Id ?? "", 0)))
            .ToArray();
    }
}
