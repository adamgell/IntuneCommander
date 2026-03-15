using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class EndpointSecurityBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private const string CacheKey = "EndpointSecurityIntents";
    private const string CacheKeyList = "EndpointSecurityIntents_List";
    private IEndpointSecurityService? _service;

    public EndpointSecurityBridgeService(AuthBridgeService authBridge, ICacheService cache, ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IEndpointSecurityService GetService()
    {
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected");
        _service ??= new EndpointSecurityService(client);
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
            var cachedList = _cache.Get<EndpointSecurityListItemDto>(tenantId, CacheKeyList);
            if (cachedList is { Count: > 0 })
                return cachedList.ToArray();
        }

        var cached = tenantId is not null ? _cache.Get<DeviceManagementIntent>(tenantId, CacheKey) : null;
        var intents = cached is { Count: > 0 } ? cached : await service.ListEndpointSecurityIntentsAsync();
        if (cached is not { Count: > 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKey, intents);

        var result = await MapList(intents, service);
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyList, result.ToList());
        return result;
    }

    private async Task<EndpointSecurityListItemDto[]> MapList(List<DeviceManagementIntent> intents, IEndpointSecurityService service)
    {
        using var sem = new SemaphoreSlim(5);
        var counts = new Dictionary<string, int>();
        var tasks = intents.Where(i => i.Id is not null).Select(async i =>
        {
            await sem.WaitAsync();
            try
            {
                var assignments = await service.GetAssignmentsAsync(i.Id!);
                lock (counts) counts[i.Id!] = assignments.Count;
            }
            catch { }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);

        return intents.Select(i => new EndpointSecurityListItemDto(
            Id: i.Id ?? "",
            DisplayName: i.DisplayName ?? "",
            Description: i.Description,
            IntentType: i.TemplateId ?? "Unknown",
            IsAssigned: i.IsAssigned ?? false,
            CreatedDateTime: i.LastModifiedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: i.LastModifiedDateTime?.ToString("o") ?? "",
            AssignmentCount: counts.GetValueOrDefault(i.Id ?? "", 0)
        )).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("ID is required");
        var service = GetService();
        var client = _authBridge.GraphClient!;

        var intentTask = service.GetEndpointSecurityIntentAsync(id);
        var assignmentsTask = service.GetAssignmentsAsync(id);
        await Task.WhenAll(intentTask, assignmentsTask);

        var intent = await intentTask ?? throw new InvalidOperationException($"Intent {id} not found");
        var assignments = await assignmentsTask;
        var targets = assignments.Select(a => a.Target).ToList();
        var groupNames = await GroupResolutionHelper.ResolveGroupNamesAsync(targets, client);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        return new EndpointSecurityDetailDto(
            Id: intent.Id ?? "",
            DisplayName: intent.DisplayName ?? "",
            Description: intent.Description,
            IntentType: intent.TemplateId ?? "Unknown",
            IsAssigned: intent.IsAssigned ?? false,
            CreatedDateTime: intent.LastModifiedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: intent.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (intent.RoleScopeTagIds ?? []).ToArray(),
            Assignments: MapAssignments(targets, groupNames),
            RawJson: JsonSerializer.Serialize(intent, jsonOptions));
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
