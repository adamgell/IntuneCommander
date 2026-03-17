using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class AssignmentExplorerBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private IAssignmentCheckerService? _checkerService;
    private IGroupService? _groupService;

    public AssignmentExplorerBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private Microsoft.Graph.Beta.GraphServiceClient GetClient() =>
        _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public void Reset()
    {
        _checkerService = null;
        _groupService = null;
    }

    private IAssignmentCheckerService GetCheckerService()
    {
        var client = GetClient();
        var tenantId = GetTenantId();
        _checkerService ??= new AssignmentCheckerService(client, _cache, tenantId);
        return _checkerService;
    }

    private IGroupService GetGroupService()
    {
        var client = GetClient();
        _groupService ??= new GroupService(client);
        return _groupService;
    }

    public async Task<object> SearchGroupsAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("query", out var queryProp))
            throw new ArgumentException("Search query is required");

        var query = queryProp.GetString() ?? throw new ArgumentException("Search query is required");
        var groupService = GetGroupService();
        var groups = await groupService.SearchGroupsAsync(query);

        return groups.Select(g => new GroupSearchResultDto(
            Id: g.Id ?? "",
            DisplayName: g.DisplayName ?? "",
            GroupType: g.GroupTypes?.Contains("DynamicMembership") == true ? "Dynamic" : "Assigned",
            MembershipRule: g.MembershipRule
        )).Take(20).ToArray();
    }

    public async Task<object> RunReportAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("mode", out var modeProp))
            throw new ArgumentException("Report mode is required");

        var mode = modeProp.GetString() ?? throw new ArgumentException("Report mode is required");
        var checker = GetCheckerService();

        List<AssignmentReportRow> rows = mode switch
        {
            "group" => await RunGroupReport(payload.Value, checker),
            "allPolicies" => await checker.GetAllPoliciesWithAssignmentsAsync(),
            "allUsers" => await checker.GetAllUsersAssignmentsAsync(),
            "allDevices" => await checker.GetAllDevicesAssignmentsAsync(),
            "unassigned" => await checker.GetUnassignedPoliciesAsync(),
            "emptyGroups" => await checker.GetEmptyGroupAssignmentsAsync(),
            _ => throw new ArgumentException($"Unknown report mode: {mode}")
        };

        return rows.Select(MapRow).ToArray();
    }

    private static async Task<List<AssignmentReportRow>> RunGroupReport(
        JsonElement payload, IAssignmentCheckerService checker)
    {
        if (!payload.TryGetProperty("groupId", out var groupIdProp))
            throw new ArgumentException("groupId is required for group report");

        var groupId = groupIdProp.GetString() ?? throw new ArgumentException("groupId is required");
        var groupName = payload.TryGetProperty("groupName", out var nameProp)
            ? nameProp.GetString() ?? ""
            : "";

        return await checker.GetGroupAssignmentsAsync(groupId, groupName);
    }

    private static AssignmentReportRowDto MapRow(AssignmentReportRow row)
    {
        return new AssignmentReportRowDto(
            PolicyId: row.PolicyId,
            PolicyName: row.PolicyName,
            PolicyType: row.PolicyType,
            Platform: row.Platform,
            AssignmentSummary: row.AssignmentSummary,
            AssignmentReason: row.AssignmentReason,
            GroupId: row.GroupId,
            GroupName: row.GroupName,
            Group1Status: row.Group1Status,
            Group2Status: row.Group2Status,
            TargetDevice: row.TargetDevice,
            UserPrincipalName: row.UserPrincipalName,
            Status: row.Status,
            LastReported: row.LastReported);
    }
}
