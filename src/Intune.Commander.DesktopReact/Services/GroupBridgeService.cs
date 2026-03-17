using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class GroupBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyDynamicGroups = "DynamicGroups";
    private const string CacheKeyAssignedGroups = "AssignedGroups";
    private const string CacheKeyGroupDetail = "GroupDetail";

    private IGroupService? _groupService;
    private IConfigurationProfileService? _configService;
    private ICompliancePolicyService? _complianceService;
    private IApplicationService? _appService;

    public GroupBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IGroupService GetGroupService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");
        _groupService ??= new GroupService(client);
        return _groupService;
    }

    private IConfigurationProfileService GetConfigService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected");
        _configService ??= new ConfigurationProfileService(client);
        return _configService;
    }

    private ICompliancePolicyService GetComplianceService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected");
        _complianceService ??= new CompliancePolicyService(client);
        return _complianceService;
    }

    private IApplicationService GetAppService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected");
        _appService ??= new ApplicationService(client);
        return _appService;
    }

    public void Reset()
    {
        _groupService = null;
        _configService = null;
        _complianceService = null;
        _appService = null;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        var service = GetGroupService();

        // Try cache for both types
        List<Group>? dynamicGroups = null;
        List<Group>? assignedGroups = null;

        if (tenantId is not null)
        {
            dynamicGroups = _cache.Get<Group>(tenantId, CacheKeyDynamicGroups);
            assignedGroups = _cache.Get<Group>(tenantId, CacheKeyAssignedGroups);
        }

        if (dynamicGroups is null || dynamicGroups.Count == 0)
        {
            dynamicGroups = await service.ListDynamicGroupsAsync();
            if (tenantId is not null)
                _cache.Set(tenantId, CacheKeyDynamicGroups, dynamicGroups);
        }

        if (assignedGroups is null || assignedGroups.Count == 0)
        {
            assignedGroups = await service.ListAssignedGroupsAsync();
            if (tenantId is not null)
                _cache.Set(tenantId, CacheKeyAssignedGroups, assignedGroups);
        }

        var all = new List<Group>(dynamicGroups.Count + assignedGroups.Count);
        all.AddRange(dynamicGroups);
        all.AddRange(assignedGroups);

        return MapGroups(all, dynamicGroups);
    }

    public async Task<object> SearchAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("query", out var queryProp))
            throw new ArgumentException("Search query is required");

        var query = queryProp.GetString() ?? throw new ArgumentException("Search query is required");
        var service = GetGroupService();

        var results = await service.SearchGroupsAsync(query);
        return MapGroups(results, []);
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Group ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Group ID is required");
        var tenantId = GetTenantId();

        // Check detail cache first (1-hour TTL)
        if (tenantId is not null)
        {
            var cachedDetail = _cache.GetSingle<GroupDetailDto>(tenantId, $"{CacheKeyGroupDetail}_{id}");
            if (cachedDetail is not null)
                return cachedDetail;
        }

        var service = GetGroupService();

        // Fetch member counts, members, and assignments in parallel
        var countsTask = service.GetMemberCountsAsync(id);
        var membersTask = service.ListGroupMembersAsync(id);
        var assignmentsTask = service.GetGroupAssignmentsAsync(
            id,
            GetConfigService(),
            GetComplianceService(),
            GetAppService());

        await Task.WhenAll(countsTask, membersTask, assignmentsTask);

        var counts = await countsTask;
        var members = await membersTask;
        var assignments = await assignmentsTask;

        // Find the group metadata in the list cache
        Group? group = FindGroupInCache(id, tenantId);

        var isDynamic = group?.GroupTypes?.Contains("DynamicMembership") ?? false;
        var groupType = isDynamic ? ClassifyDynamicGroup(group!) : "Assigned";

        var detail = new GroupDetailDto(
            Id: id,
            DisplayName: group?.DisplayName ?? id,
            Description: group?.Description,
            GroupType: groupType,
            MembershipRule: group?.MembershipRule,
            MembershipRuleProcessingState: group?.MembershipRuleProcessingState,
            MailEnabled: group?.MailEnabled ?? false,
            SecurityEnabled: group?.SecurityEnabled ?? false,
            Mail: group?.Mail,
            CreatedDateTime: group?.CreatedDateTime?.ToString("o") ?? "",
            MemberCounts: new GroupMemberCountsDto(counts.Users, counts.Devices, counts.NestedGroups, counts.Total),
            Members: members.Select(m => new GroupMemberInfoDto(
                m.MemberType, m.DisplayName, m.SecondaryInfo, m.TertiaryInfo, m.Status, m.Id)).ToArray(),
            Assignments: assignments.Select(a => new GroupAssignedObjectDto(
                a.ObjectId, a.DisplayName, a.ObjectType, a.Category, a.Platform, a.AssignmentIntent, a.IsExclusion)).ToArray());

        // Cache the assembled detail (shorter 1-hour TTL since members change more often)
        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyGroupDetail}_{id}", detail, TimeSpan.FromHours(1));

        return detail;
    }

    private Group? FindGroupInCache(string id, string? tenantId)
    {
        if (tenantId is null) return null;

        var cached = _cache.Get<Group>(tenantId, CacheKeyDynamicGroups);
        var group = cached?.FirstOrDefault(g => g.Id == id);
        if (group is not null) return group;

        cached = _cache.Get<Group>(tenantId, CacheKeyAssignedGroups);
        return cached?.FirstOrDefault(g => g.Id == id);
    }

    private static GroupListItemDto[] MapGroups(List<Group> groups, List<Group> dynamicGroups)
    {
        var dynamicIds = new HashSet<string>(dynamicGroups.Where(g => g.Id is not null).Select(g => g.Id!));

        return groups
            .Where(g => g.Id is not null)
            .Select(g =>
            {
                var isDynamic = g.GroupTypes?.Contains("DynamicMembership") ?? false;
                var groupType = isDynamic ? ClassifyDynamicGroup(g) : "Assigned";

                return new GroupListItemDto(
                    Id: g.Id!,
                    DisplayName: g.DisplayName ?? "",
                    Description: g.Description,
                    GroupType: groupType,
                    MembershipRule: g.MembershipRule,
                    MemberCount: 0, // Counts loaded on detail selection to avoid N+1
                    Mail: g.Mail,
                    CreatedDateTime: g.CreatedDateTime?.ToString("o") ?? "");
            })
            .OrderBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ClassifyDynamicGroup(Group group)
    {
        var rule = group.MembershipRule ?? "";
        if (rule.Contains("device.", StringComparison.OrdinalIgnoreCase))
            return "Dynamic Device";
        if (rule.Contains("user.", StringComparison.OrdinalIgnoreCase))
            return "Dynamic User";
        return "Dynamic";
    }
}
