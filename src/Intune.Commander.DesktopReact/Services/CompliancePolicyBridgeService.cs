using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class CompliancePolicyBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKey = "CompliancePolicies";
    private const string CacheKeyList = "CompliancePolicies_List";
    private ICompliancePolicyService? _service;

    public CompliancePolicyBridgeService(AuthBridgeService authBridge, ICacheService cache, ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private ICompliancePolicyService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new CompliancePolicyService(client);
        return _service;
    }

    public void Reset() => _service = null;
    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var service = GetService();
        var tenantId = GetTenantId();

        // Return cached mapped list (includes assignment counts) to avoid N+1 on cache hits
        if (tenantId is not null)
        {
            var cachedList = _cache.Get<CompliancePolicyListItemDto>(tenantId, CacheKeyList);
            if (cachedList is { Count: > 0 })
                return cachedList.ToArray();
        }

        var cached = tenantId is not null ? _cache.Get<DeviceCompliancePolicy>(tenantId, CacheKey) : null;
        var policies = cached is { Count: > 0 } ? cached : await service.ListCompliancePoliciesAsync();
        if (cached is not { Count: > 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKey, policies);

        var result = await MapList(policies, service);
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyList, result.ToList());
        return result;
    }

    private async Task<CompliancePolicyListItemDto[]> MapList(List<DeviceCompliancePolicy> policies, ICompliancePolicyService service)
    {
        using var sem = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();
        var tasks = policies.Where(p => p.Id is not null).Select(async p =>
        {
            await sem.WaitAsync();
            try
            {
                var assignments = await service.GetAssignmentsAsync(p.Id!);
                lock (counts) counts[p.Id!] = assignments.Count;
            }
            catch { }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);

        return policies.Select(p => new CompliancePolicyListItemDto(
            Id: p.Id ?? "",
            DisplayName: p.DisplayName ?? "",
            Description: p.Description,
            PolicyType: p.GetType().Name.Replace("DeviceCompliancePolicy", "").Replace("CompliancePolicy", ""),
            CreatedDateTime: p.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: p.LastModifiedDateTime?.ToString("o") ?? "",
            AssignmentCount: counts.GetValueOrDefault(p.Id ?? "", 0)
        )).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var service = GetService();
        var client = _authBridge.GraphClient!;

        var policyTask = service.GetCompliancePolicyAsync(id);
        var assignmentsTask = service.GetAssignmentsAsync(id);
        await Task.WhenAll(policyTask, assignmentsTask);

        var policy = await policyTask ?? throw new InvalidOperationException($"Policy {id} not found");
        var assignments = await assignmentsTask;
        var targets = assignments.Select(a => a.Target).ToList();
        var groupNames = await GroupResolutionHelper.ResolveGroupNamesAsync(targets, client);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return new CompliancePolicyDetailDto(
            Id: policy.Id ?? "",
            DisplayName: policy.DisplayName ?? "",
            Description: policy.Description,
            PolicyType: policy.GetType().Name,
            CreatedDateTime: policy.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: policy.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (policy.RoleScopeTagIds ?? []).ToArray(),
            Assignments: MapAssignments(targets, groupNames),
            RawJson: JsonSerializer.Serialize(policy, jsonOptions));
    }

    private static AssignmentDto[] MapAssignments(List<DeviceAndAppManagementAssignmentTarget?> targets, Dictionary<string, string> groupNames)
    {
        return targets.Where(t => t is not null).Select(t => t switch
        {
            AllDevicesAssignmentTarget => new AssignmentDto("All Devices", "Include"),
            AllLicensedUsersAssignmentTarget => new AssignmentDto("All Users", "Include"),
            ExclusionGroupAssignmentTarget excl => new AssignmentDto(
                groupNames.GetValueOrDefault(excl.GroupId ?? "") ?? excl.GroupId ?? "Unknown", "Exclude"),
            GroupAssignmentTarget grp => new AssignmentDto(
                groupNames.GetValueOrDefault(grp.GroupId ?? "") ?? grp.GroupId ?? "Unknown", "Include"),
            _ => new AssignmentDto("Unknown", "Unknown")
        }).ToArray();
    }
}
