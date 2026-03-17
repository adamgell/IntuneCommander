using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class AppProtectionPolicyBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKeyPolicies = "AppProtectionPolicies";
    private const string CacheKeyList = "AppProtectionPolicies_List";
    private const string CacheKeyDetail = "AppProtectionPolicies_Detail";

    private IAppProtectionPolicyService? _service;

    public AppProtectionPolicyBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IAppProtectionPolicyService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new AppProtectionPolicyService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        if (tenantId is not null)
        {
            var cachedList = _cache.Get<AppProtectionPolicyListItemDto>(tenantId, CacheKeyList);
            if (cachedList is { Count: > 0 })
                return cachedList.ToArray();
        }

        var service = GetService();
        var policies = await GroupResolutionHelper.GetCachedOrFetchAsync(
            _cache,
            tenantId,
            CacheKeyPolicies,
            () => service.ListAppProtectionPoliciesAsync());

        var items = await MapListAsync(policies);
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyList, items.ToList());

        return items;
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
            var cached = _cache.GetSingle<AppProtectionPolicyDetailDto>(tenantId, $"{CacheKeyDetail}_{id}");
            if (cached is not null)
                return cached;
        }

        var policy = await service.GetAppProtectionPolicyAsync(id)
            ?? throw new InvalidOperationException($"App protection policy {id} not found");

        var assignments = await GetAssignmentsAsync(policy);
        var targets = assignments.Select(assignment => assignment.Target).ToList();
        var groupNames = await GroupResolutionHelper.ResolveGroupNamesAsync(targets, client);

        var detail = new AppProtectionPolicyDetailDto(
            Id: policy.Id ?? "",
            DisplayName: policy.DisplayName ?? "",
            Description: policy.Description,
            PolicyType: ApplicationDataMapper.FormatTypeName(policy.GetType().Name),
            OdataType: policy.OdataType ?? policy.GetType().Name,
            Platform: GetProtectionPlatform(policy),
            Version: ApplicationDataMapper.ExtractVersion(policy),
            CreatedDateTime: policy.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: policy.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (policy.RoleScopeTagIds ?? []).ToArray(),
            MinimumRequiredAppVersion: policy is ManagedAppProtection protection
                ? protection.MinimumRequiredAppVersion ?? ""
                : "",
            MinimumRequiredOsVersion: policy is ManagedAppProtection managedProtection
                ? managedProtection.MinimumRequiredOsVersion ?? ""
                : "",
            Assignments: GroupResolutionHelper.MapAssignments(targets, groupNames));

        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyDetail}_{id}", detail);

        return detail;
    }

    private async Task<AppProtectionPolicyListItemDto[]> MapListAsync(List<ManagedAppPolicy> policies)
    {
        using var semaphore = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();

        var tasks = policies.Where(policy => policy.Id is not null).Select(async policy =>
        {
            await semaphore.WaitAsync();
            try
            {
                var assignments = await GetAssignmentsAsync(policy);
                lock (counts)
                {
                    counts[policy.Id!] = assignments.Count;
                }
            }
            catch
            {
                lock (counts)
                {
                    counts[policy.Id!] = 0;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return policies
            .Select(policy => new AppProtectionPolicyListItemDto(
                Id: policy.Id ?? "",
                DisplayName: policy.DisplayName ?? "",
                Description: policy.Description,
                PolicyType: ApplicationDataMapper.FormatTypeName(policy.GetType().Name),
                Platform: GetProtectionPlatform(policy),
                Version: ApplicationDataMapper.ExtractVersion(policy),
                CreatedDateTime: policy.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: policy.LastModifiedDateTime?.ToString("o") ?? "",
                AssignmentCount: counts.GetValueOrDefault(policy.Id ?? "", 0)))
            .ToArray();
    }

    private async Task<List<TargetedManagedAppPolicyAssignment>> GetAssignmentsAsync(ManagedAppPolicy policy)
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        if (policy.Id is null)
            return [];

        var response = policy switch
        {
            AndroidManagedAppProtection => await client.DeviceAppManagement.AndroidManagedAppProtections[policy.Id]
                .Assignments.GetAsync(),
            IosManagedAppProtection => await client.DeviceAppManagement.IosManagedAppProtections[policy.Id]
                .Assignments.GetAsync(),
            WindowsManagedAppProtection => await client.DeviceAppManagement.WindowsManagedAppProtections[policy.Id]
                .Assignments.GetAsync(),
            MdmWindowsInformationProtectionPolicy => await client.DeviceAppManagement.MdmWindowsInformationProtectionPolicies[policy.Id]
                .Assignments.GetAsync(),
            WindowsInformationProtectionPolicy => await client.DeviceAppManagement.WindowsInformationProtectionPolicies[policy.Id]
                .Assignments.GetAsync(),
            _ => null
        };

        return response?.Value ?? [];
    }

    private static string GetProtectionPlatform(ManagedAppPolicy policy)
    {
        return policy switch
        {
            AndroidManagedAppProtection => "Android",
            IosManagedAppProtection => "iOS",
            WindowsManagedAppProtection or WindowsInformationProtectionPolicy or MdmWindowsInformationProtectionPolicy => "Windows",
            _ => "Unknown"
        };
    }
}
