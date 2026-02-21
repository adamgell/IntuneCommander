using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Services;

/// <summary>
/// Full .NET port of the IntuneAssignmentChecker PowerShell script.
/// Scans all supported Intune policy types and resolves assignments for
/// users, groups, and devices.
/// </summary>
public class AssignmentCheckerService : IAssignmentCheckerService
{
    private readonly GraphServiceClient _graphClient;
    private const int MaxConcurrency = 5;

    public AssignmentCheckerService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────────────────────

    public async Task<List<AssignmentReportRow>> GetUserAssignmentsAsync(
        string userPrincipalName,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Invoke($"Looking up user {userPrincipalName}...");
        var user = await _graphClient.Users[userPrincipalName]
            .GetAsync(req => req.QueryParameters.Select = ["id", "displayName", "userPrincipalName"],
                cancellationToken);

        if (user?.Id == null)
            throw new InvalidOperationException($"User not found: {userPrincipalName}");

        progress?.Invoke("Fetching group memberships...");
        var groupIds = await GetUserTransitiveMemberGroupIdsAsync(user.Id, cancellationToken);

        return await GetEntityAssignmentsAsync(groupIds, isUser: true, progress, cancellationToken);
    }

    public async Task<List<AssignmentReportRow>> GetGroupAssignmentsAsync(
        string groupId,
        string groupName,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(
            rows, ScanMode.DirectGroup, groupId, groupName, null,
            progress, cancellationToken);
        return rows;
    }

    public async Task<List<AssignmentReportRow>> GetDeviceAssignmentsAsync(
        string deviceName,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Invoke($"Looking up device {deviceName}...");
        var escaped = deviceName.Replace("'", "''");
        var devicesPage = await _graphClient.Devices
            .GetAsync(req =>
            {
                req.QueryParameters.Filter = $"displayName eq '{escaped}'";
                req.QueryParameters.Select = ["id", "displayName", "operatingSystem"];
                req.QueryParameters.Top = 5;
            }, cancellationToken);

        var device = devicesPage?.Value?.FirstOrDefault();
        if (device?.Id == null)
            throw new InvalidOperationException($"Device not found in Azure AD: {deviceName}");

        progress?.Invoke("Fetching device group memberships...");
        var groupIds = await GetDeviceTransitiveMemberGroupIdsAsync(device.Id, cancellationToken);

        return await GetEntityAssignmentsAsync(groupIds, isUser: false, progress, cancellationToken);
    }

    public async Task<List<AssignmentReportRow>> GetAllPoliciesWithAssignmentsAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(rows, ScanMode.AllPolicies, null, null, null, progress, cancellationToken);
        return rows;
    }

    public async Task<List<AssignmentReportRow>> GetAllUsersAssignmentsAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(rows, ScanMode.AllUsers, null, null, null, progress, cancellationToken);
        return rows;
    }

    public async Task<List<AssignmentReportRow>> GetAllDevicesAssignmentsAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(rows, ScanMode.AllDevices, null, null, null, progress, cancellationToken);
        return rows;
    }

    public async Task<List<AssignmentReportRow>> GetUnassignedPoliciesAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(rows, ScanMode.Unassigned, null, null, null, progress, cancellationToken);
        return rows;
    }

    public async Task<List<AssignmentReportRow>> GetEmptyGroupAssignmentsAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();

        // First pass: collect all group IDs referenced in assignments
        progress?.Invoke("Collecting group IDs from assignments...");
        var seenGroupIds = await CollectAllAssignedGroupIdsAsync(cancellationToken);

        if (seenGroupIds.Count == 0) return rows;

        // Second pass: check which groups are empty
        progress?.Invoke($"Checking {seenGroupIds.Count} groups for empty membership...");
        var emptyGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var sem = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var tasks = seenGroupIds.Select(async gid =>
        {
            await sem.WaitAsync(cancellationToken);
            try
            {
                var group = await _graphClient.Groups[gid]
                    .GetAsync(req => req.QueryParameters.Select = ["id", "displayName"], cancellationToken);
                if (group == null) return;
                lock (groupNames) groupNames[gid] = group.DisplayName ?? gid;

                var members = await _graphClient.Groups[gid].Members
                    .GetAsync(req => req.QueryParameters.Select = ["id"], cancellationToken);
                if ((members?.Value?.Count ?? 0) == 0)
                    lock (emptyGroupIds) emptyGroupIds.Add(gid);
            }
            catch { /* skip inaccessible groups */ }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);

        if (emptyGroupIds.Count == 0) return rows;

        // Third pass: find policies assigned to empty groups
        progress?.Invoke($"Finding policies assigned to {emptyGroupIds.Count} empty group(s)...");
        await ScanAllPoliciesAsync(rows, ScanMode.EmptyGroups, null, null,
            new EmptyGroupContext(emptyGroupIds, groupNames), progress, cancellationToken);

        return rows;
    }

    public async Task<List<AssignmentReportRow>> CompareGroupAssignmentsAsync(
        string groupId1, string groupName1,
        string groupId2, string groupName2,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Invoke($"Scanning assignments for {groupName1}...");
        var g1 = await GetGroupAssignmentsAsync(groupId1, groupName1, null, cancellationToken);

        progress?.Invoke($"Scanning assignments for {groupName2}...");
        var g2 = await GetGroupAssignmentsAsync(groupId2, groupName2, null, cancellationToken);

        var g1Map = g1.ToDictionary(r => r.PolicyId + "|" + r.PolicyType);
        var g2Map = g2.ToDictionary(r => r.PolicyId + "|" + r.PolicyType);

        var allKeys = g1Map.Keys.Union(g2Map.Keys).ToList();
        var result = new List<AssignmentReportRow>(allKeys.Count);

        foreach (var key in allKeys)
        {
            g1Map.TryGetValue(key, out var r1);
            g2Map.TryGetValue(key, out var r2);
            var template = r1 ?? r2!;
            result.Add(new AssignmentReportRow
            {
                PolicyId = template.PolicyId,
                PolicyName = template.PolicyName,
                PolicyType = template.PolicyType,
                Platform = template.Platform,
                Group1Status = r1 != null ? r1.AssignmentReason : "",
                Group2Status = r2 != null ? r2.AssignmentReason : ""
            });
        }

        result.Sort(PolicyTypeNameComparer);
        return result;
    }

    public async Task<List<AssignmentReportRow>> GetFailedAssignmentsAsync(
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<AssignmentReportRow>();
        var failedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "error", "conflict", "notApplicable" };

        // Device Configuration Failures
        progress?.Invoke("Checking device configuration failures...");
        try
        {
            var configs = await FetchDeviceConfigurationsAsync(cancellationToken);
            using var sem = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
            var tasks = configs.Select(async config =>
            {
                await sem.WaitAsync(cancellationToken);
                try
                {
                    if (config.Id == null) return;
                    var statusResp = await _graphClient.DeviceManagement
                        .DeviceConfigurations[config.Id].DeviceStatuses
                        .GetAsync(req =>
                            req.QueryParameters.Select = ["id", "deviceDisplayName", "status",
                                "userPrincipalName", "lastReportedDateTime"],
                            cancellationToken);
                    var statuses = statusResp?.Value ?? [];
                    foreach (var s in statuses)
                    {
                        var statusStr = s.Status?.ToString().ToLowerInvariant() ?? "";
                        if (!failedStatuses.Contains(statusStr)) continue;
                        lock (rows)
                            rows.Add(new AssignmentReportRow
                            {
                                PolicyId = config.Id,
                                PolicyName = config.DisplayName ?? config.Id,
                                PolicyType = "Device Configuration",
                                TargetDevice = s.DeviceDisplayName ?? "",
                                Status = s.Status?.ToString() ?? "",
                                UserPrincipalName = s.UserPrincipalName ?? "",
                                LastReported = s.LastReportedDateTime?.ToString("g") ?? ""
                            });
                    }
                }
                finally { sem.Release(); }
            });
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            rows.Add(new AssignmentReportRow { PolicyType = "Device Configuration", PolicyName = $"Error: {ex.Message}" });
        }

        // Compliance Policy Failures
        progress?.Invoke("Checking compliance policy failures...");
        var complianceFailed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "error", "conflict", "notApplicable", "nonCompliant" };
        try
        {
            var policies = await FetchCompliancePoliciesAsync(cancellationToken);
            using var sem2 = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
            var tasks2 = policies.Select(async policy =>
            {
                await sem2.WaitAsync(cancellationToken);
                try
                {
                    if (policy.Id == null) return;
                    var statusResp = await _graphClient.DeviceManagement
                        .DeviceCompliancePolicies[policy.Id].DeviceStatuses
                        .GetAsync(req =>
                            req.QueryParameters.Select = ["id", "deviceDisplayName", "status",
                                "userPrincipalName", "lastReportedDateTime"],
                            cancellationToken);
                    var statuses = statusResp?.Value ?? [];
                    foreach (var s in statuses)
                    {
                        var statusStr = s.Status?.ToString().ToLowerInvariant() ?? "";
                        if (!complianceFailed.Contains(statusStr)) continue;
                        lock (rows)
                            rows.Add(new AssignmentReportRow
                            {
                                PolicyId = policy.Id,
                                PolicyName = policy.DisplayName ?? policy.Id,
                                PolicyType = "Compliance Policy",
                                TargetDevice = s.DeviceDisplayName ?? "",
                                Status = s.Status?.ToString() ?? "",
                                UserPrincipalName = s.UserPrincipalName ?? "",
                                LastReported = s.LastReportedDateTime?.ToString("g") ?? ""
                            });
                    }
                }
                finally { sem2.Release(); }
            });
            await Task.WhenAll(tasks2);
        }
        catch (Exception ex)
        {
            rows.Add(new AssignmentReportRow { PolicyType = "Compliance Policy", PolicyName = $"Error: {ex.Message}" });
        }

        rows.Sort(PolicyTypeNameComparer);
        return rows;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Internal scan modes
    // ──────────────────────────────────────────────────────────────────────────────

    private enum ScanMode
    {
        EntityMatch,   // user or device — group IDs + allUsers/allDevices
        DirectGroup,   // single specific group ID
        AllPolicies,   // all policies with any assignments
        AllUsers,      // policies assigned to All Users
        AllDevices,    // policies assigned to All Devices
        Unassigned,    // policies with no assignments
        EmptyGroups    // policies assigned to groups with 0 members
    }

    private sealed record EmptyGroupContext(
        HashSet<string> EmptyGroupIds,
        Dictionary<string, string> GroupNames);

    private async Task<List<AssignmentReportRow>> GetEntityAssignmentsAsync(
        HashSet<string> groupIds, bool isUser,
        Action<string>? progress, CancellationToken ct)
    {
        var rows = new List<AssignmentReportRow>();
        await ScanAllPoliciesAsync(rows, ScanMode.EntityMatch, null, null,
            new EntityContext(groupIds, isUser), progress, ct);
        return rows;
    }

    private sealed record EntityContext(HashSet<string> GroupIds, bool IsUser);

    /// <summary>
    /// Master scan that iterates all policy types and applies the given mode filter.
    /// </summary>
    private async Task ScanAllPoliciesAsync(
        List<AssignmentReportRow> rows,
        ScanMode mode,
        string? directGroupId,
        string? directGroupName,
        object? context,
        Action<string>? progress,
        CancellationToken ct)
    {
        var entityCtx = context as EntityContext;
        var emptyCtx = context as EmptyGroupContext;

        // ── Device Configurations ──
        progress?.Invoke("Scanning device configurations...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Device Configuration",
            () => FetchDeviceConfigurationsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            p => PlatformFromOData(p.OdataType),
            async id =>
            {
                var r = await _graphClient.DeviceManagement.DeviceConfigurations[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Settings Catalog ──
        progress?.Invoke("Scanning settings catalog policies...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Settings Catalog",
            () => FetchSettingsCatalogAsync(ct),
            p => p.Id!, p => p.Name ?? p.Id!,
            p => p.Platforms?.ToString() ?? "",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.ConfigurationPolicies[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Administrative Templates ──
        progress?.Invoke("Scanning administrative templates...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Administrative Template",
            () => FetchAdminTemplatesAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            _ => "Windows",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.GroupPolicyConfigurations[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Compliance Policies ──
        progress?.Invoke("Scanning compliance policies...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Compliance Policy",
            () => FetchCompliancePoliciesAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            p => PlatformFromOData(p.OdataType),
            async id =>
            {
                var r = await _graphClient.DeviceManagement.DeviceCompliancePolicies[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── App Protection Policies ──
        progress?.Invoke("Scanning app protection policies...");
        var appProtectionPolicies = await FetchAppProtectionPoliciesAsync(ct);
        using (var appProtSem = new SemaphoreSlim(MaxConcurrency, MaxConcurrency))
        {
            var appProtTasks = appProtectionPolicies.Select(async policy =>
            {
                await appProtSem.WaitAsync(ct);
                try
                {
                    if (policy.Id == null) return;
                    var flat = await FetchAppProtectionAssignmentsAsync(policy, ct);
                    var row = BuildRow("App Protection Policy", policy.Id, policy.DisplayName ?? policy.Id,
                        PlatformFromOData(policy.OdataType), flat, mode, directGroupId, entityCtx, emptyCtx);
                    if (row != null) lock (rows) rows.Add(row);
                }
                finally { appProtSem.Release(); }
            });
            await Task.WhenAll(appProtTasks);
        }

        // ── App Configuration Policies ──
        progress?.Invoke("Scanning app configuration policies...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "App Configuration Policy",
            () => FetchMobileAppConfigurationsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            p => PlatformFromOData(p.OdataType),
            async id =>
            {
                var r = await _graphClient.DeviceAppManagement.MobileAppConfigurations[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Applications ──
        progress?.Invoke("Scanning applications...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Application",
            () => FetchApplicationsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            p => PlatformFromOData(p.OdataType),
            async id =>
            {
                var r = await _graphClient.DeviceAppManagement.MobileApps[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Platform Scripts ──
        progress?.Invoke("Scanning platform scripts...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Platform Script",
            () => FetchPlatformScriptsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            _ => "Windows",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.DeviceManagementScripts[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Health Scripts ──
        progress?.Invoke("Scanning health scripts...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Health Script",
            () => FetchHealthScriptsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            _ => "Windows",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.DeviceHealthScripts[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Endpoint Security (Settings Catalog families) ──
        progress?.Invoke("Scanning endpoint security policies...");
        var endpointFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "endpointSecurityAntivirus", "endpointSecurityDiskEncryption",
            "endpointSecurityFirewall", "endpointSecurityEndpointDetectionAndResponse",
            "endpointSecurityAttackSurfaceReduction", "endpointSecurityAccountProtection"
        };
        var allConfigPolicies = await FetchSettingsCatalogAsync(ct);
        var esPolicies = allConfigPolicies
            .Where(p => p.TemplateReference?.TemplateFamily != null &&
                        endpointFamilies.Contains(p.TemplateReference.TemplateFamily.ToString()!))
            .ToList();
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Endpoint Security",
            () => Task.FromResult(esPolicies),
            p => p.Id!, p => p.Name ?? p.Id!,
            p => p.Platforms?.ToString() ?? "",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.ConfigurationPolicies[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Endpoint Security Intents (legacy) ──
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Endpoint Security (Legacy)",
            () => FetchEndpointSecurityIntentsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            _ => "",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.Intents[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        // ── Enrollment Configurations ──
        progress?.Invoke("Scanning enrollment configurations...");
        await ScanPolicyTypeAsync(rows, mode, directGroupId, entityCtx, emptyCtx, ct,
            "Enrollment Configuration",
            () => FetchEnrollmentConfigurationsAsync(ct),
            p => p.Id!, p => p.DisplayName ?? p.Id!,
            _ => "",
            async id =>
            {
                var r = await _graphClient.DeviceManagement.DeviceEnrollmentConfigurations[id]
                    .Assignments.GetAsync(cancellationToken: ct);
                return Flatten(r?.Value);
            });

        rows.Sort(PolicyTypeNameComparer);
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Generic type scanner
    // ──────────────────────────────────────────────────────────────────────────────

    private async Task ScanPolicyTypeAsync<T>(
        List<AssignmentReportRow> rows,
        ScanMode mode,
        string? directGroupId,
        EntityContext? entityCtx,
        EmptyGroupContext? emptyCtx,
        CancellationToken ct,
        string policyType,
        Func<Task<List<T>>> fetchPolicies,
        Func<T, string> getId,
        Func<T, string> getName,
        Func<T, string> getPlatform,
        Func<string, Task<List<FlatAssignment>>> getAssignments)
    {
        var items = await fetchPolicies();
        using var sem = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        var tasks = items.Select(async item =>
        {
            await sem.WaitAsync(ct);
            try
            {
                var id = getId(item);
                if (string.IsNullOrEmpty(id)) return;
                List<FlatAssignment> flat;
                try { flat = await getAssignments(id); }
                catch { return; } // skip 403s / 404s
                var row = BuildRow(policyType, id, getName(item), getPlatform(item),
                    flat, mode, directGroupId, entityCtx, emptyCtx);
                if (row != null) lock (rows) rows.Add(row);
            }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Row builder — applies the scan mode filter
    // ──────────────────────────────────────────────────────────────────────────────

    private static AssignmentReportRow? BuildRow(
        string policyType, string id, string name, string platform,
        List<FlatAssignment> assignments,
        ScanMode mode, string? directGroupId,
        EntityContext? entityCtx, EmptyGroupContext? emptyCtx)
    {
        switch (mode)
        {
            case ScanMode.Unassigned:
                return assignments.Count == 0
                    ? new AssignmentReportRow { PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform }
                    : null;

            case ScanMode.AllPolicies:
                return assignments.Count > 0
                    ? new AssignmentReportRow
                    {
                        PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                        AssignmentSummary = SummariseAssignments(assignments)
                    }
                    : null;

            case ScanMode.AllUsers:
                return assignments.Any(a => a.IsAllUsers)
                    ? new AssignmentReportRow { PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                        AssignmentSummary = "All Users" }
                    : null;

            case ScanMode.AllDevices:
                return assignments.Any(a => a.IsAllDevices)
                    ? new AssignmentReportRow { PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                        AssignmentSummary = "All Devices" }
                    : null;

            case ScanMode.DirectGroup when directGroupId != null:
            {
                var matched = assignments
                    .Where(a => string.Equals(a.GroupId, directGroupId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matched.Count == 0) return null;
                var reason = matched.Any(a => a.IsExclusion) ? "Excluded" : "Direct Assignment";
                return new AssignmentReportRow
                {
                    PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                    AssignmentReason = reason,
                    AssignmentSummary = reason
                };
            }

            case ScanMode.EntityMatch when entityCtx != null:
            {
                // Exclusion wins — if any exclusion targets one of the entity's groups, skip
                if (assignments.Any(a => a.IsExclusion && a.GroupId != null && entityCtx.GroupIds.Contains(a.GroupId)))
                    return null;

                string? reason = null;
                foreach (var a in assignments.Where(x => !x.IsExclusion))
                {
                    if (entityCtx.IsUser && a.IsAllUsers) { reason = "All Users"; break; }
                    if (!entityCtx.IsUser && a.IsAllDevices) { reason = "All Devices"; break; }
                    if (a.GroupId != null && entityCtx.GroupIds.Contains(a.GroupId)) { reason = "Group Assignment"; break; }
                }
                return reason == null ? null
                    : new AssignmentReportRow
                    {
                        PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                        AssignmentReason = reason, AssignmentSummary = reason
                    };
            }

            case ScanMode.EmptyGroups when emptyCtx != null:
            {
                var emptyMatches = assignments
                    .Where(a => a.GroupId != null && emptyCtx.EmptyGroupIds.Contains(a.GroupId))
                    .ToList();
                if (emptyMatches.Count == 0) return null;
                var first = emptyMatches[0];
                emptyCtx.GroupNames.TryGetValue(first.GroupId!, out var gName);
                return new AssignmentReportRow
                {
                    PolicyId = id, PolicyName = name, PolicyType = policyType, Platform = platform,
                    GroupId = first.GroupId!,
                    GroupName = gName ?? first.GroupId!,
                    AssignmentReason = first.IsExclusion ? "Excluded" : "Include"
                };
            }

            default:
                return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Assignment flattening
    // ──────────────────────────────────────────────────────────────────────────────

    private sealed record FlatAssignment(
        string? GroupId,
        bool IsExclusion,
        bool IsAllUsers,
        bool IsAllDevices);

    private static List<FlatAssignment> Flatten<T>(IList<T>? assignments) where T : class
    {
        if (assignments == null) return [];
        var result = new List<FlatAssignment>(assignments.Count);
        foreach (var a in assignments)
        {
            var target = a.GetType().GetProperty("Target")?.GetValue(a)
                as DeviceAndAppManagementAssignmentTarget;
            AppendTarget(result, target);
        }
        return result;
    }

    private static void AppendTarget(List<FlatAssignment> result,
        DeviceAndAppManagementAssignmentTarget? target)
    {
        switch (target)
        {
            case ExclusionGroupAssignmentTarget ex:
                result.Add(new FlatAssignment(ex.GroupId, true, false, false));
                break;
            case GroupAssignmentTarget g:
                result.Add(new FlatAssignment(g.GroupId, false, false, false));
                break;
            case AllLicensedUsersAssignmentTarget:
                result.Add(new FlatAssignment(null, false, true, false));
                break;
            case AllDevicesAssignmentTarget:
                result.Add(new FlatAssignment(null, false, false, true));
                break;
        }
    }

    private async Task<List<FlatAssignment>> FetchAppProtectionAssignmentsAsync(
        ManagedAppPolicy policy, CancellationToken ct)
    {
        var odataType = policy.OdataType ?? "";
        try
        {
            if (odataType.Contains("android", StringComparison.OrdinalIgnoreCase))
            {
                var r = await _graphClient.DeviceAppManagement
                    .AndroidManagedAppProtections[policy.Id!].Assignments.GetAsync(cancellationToken: ct);
                return FlattenTargetedManagedApp(r?.Value);
            }
            if (odataType.Contains("ios", StringComparison.OrdinalIgnoreCase))
            {
                var r = await _graphClient.DeviceAppManagement
                    .IosManagedAppProtections[policy.Id!].Assignments.GetAsync(cancellationToken: ct);
                return FlattenTargetedManagedApp(r?.Value);
            }
            if (odataType.Contains("windows", StringComparison.OrdinalIgnoreCase))
            {
                var r = await _graphClient.DeviceAppManagement
                    .WindowsManagedAppProtections[policy.Id!].Assignments.GetAsync(cancellationToken: ct);
                return FlattenTargetedManagedApp(r?.Value);
            }
        }
        catch { /* ignore per-policy errors */ }
        return [];
    }

    private static List<FlatAssignment> FlattenTargetedManagedApp(
        IList<TargetedManagedAppPolicyAssignment>? assignments)
    {
        if (assignments == null) return [];
        var result = new List<FlatAssignment>(assignments.Count);
        foreach (var a in assignments)
            AppendTarget(result, a.Target);
        return result;
    }

    private static string SummariseAssignments(List<FlatAssignment> assignments)
    {
        var parts = new List<string>();
        foreach (var a in assignments)
        {
            if (a.IsAllUsers) parts.Add("All Users");
            else if (a.IsAllDevices) parts.Add("All Devices");
            else if (a.IsExclusion) parts.Add($"[Excluded] {a.GroupId}");
            else parts.Add($"Group: {a.GroupId}");
        }
        return parts.Count > 0 ? string.Join(", ", parts.Distinct()) : "Not Assigned";
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Group ID collector (for empty-group mode first pass)
    // ──────────────────────────────────────────────────────────────────────────────

    private async Task<HashSet<string>> CollectAllAssignedGroupIdsAsync(CancellationToken ct)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        async Task CollectFromAsync<T>(List<T> items, Func<T, string> getId,
            Func<string, Task<List<FlatAssignment>>> getAssignments)
        {
            using var sem = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
            var tasks = items.Select(async item =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var id = getId(item);
                    if (string.IsNullOrEmpty(id)) return;
                    var flat = await getAssignments(id);
                    foreach (var fa in flat.Where(f => f.GroupId != null))
                        lock (ids) ids.Add(fa.GroupId!);
                }
                catch { }
                finally { sem.Release(); }
            });
            await Task.WhenAll(tasks);
        }

        var configs = await FetchDeviceConfigurationsAsync(ct);
        await CollectFromAsync(configs, p => p.Id!,
            async id => Flatten((await _graphClient.DeviceManagement.DeviceConfigurations[id]
                .Assignments.GetAsync(cancellationToken: ct))?.Value));

        var scPolicies = await FetchSettingsCatalogAsync(ct);
        await CollectFromAsync(scPolicies, p => p.Id!,
            async id => Flatten((await _graphClient.DeviceManagement.ConfigurationPolicies[id]
                .Assignments.GetAsync(cancellationToken: ct))?.Value));

        var cpPolicies = await FetchCompliancePoliciesAsync(ct);
        await CollectFromAsync(cpPolicies, p => p.Id!,
            async id => Flatten((await _graphClient.DeviceManagement.DeviceCompliancePolicies[id]
                .Assignments.GetAsync(cancellationToken: ct))?.Value));

        var apps = await FetchApplicationsAsync(ct);
        await CollectFromAsync(apps, p => p.Id!,
            async id => Flatten((await _graphClient.DeviceAppManagement.MobileApps[id]
                .Assignments.GetAsync(cancellationToken: ct))?.Value));

        return ids;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Transitive group membership helpers
    // ──────────────────────────────────────────────────────────────────────────────

    private async Task<HashSet<string>> GetUserTransitiveMemberGroupIdsAsync(
        string userId, CancellationToken ct)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resp = await _graphClient.Users[userId].TransitiveMemberOf
            .GetAsync(req => req.QueryParameters.Select = ["id"], ct);

        while (resp?.Value != null)
        {
            foreach (var obj in resp.Value)
                if (!string.IsNullOrEmpty(obj.Id)) ids.Add(obj.Id!);

            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.Users[userId].TransitiveMemberOf
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return ids;
    }

    private async Task<HashSet<string>> GetDeviceTransitiveMemberGroupIdsAsync(
        string deviceId, CancellationToken ct)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resp = await _graphClient.Devices[deviceId].TransitiveMemberOf
            .GetAsync(req => req.QueryParameters.Select = ["id"], ct);

        while (resp?.Value != null)
        {
            foreach (var obj in resp.Value)
                if (!string.IsNullOrEmpty(obj.Id)) ids.Add(obj.Id!);

            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.Devices[deviceId].TransitiveMemberOf
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return ids;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Policy fetchers (with pagination)
    // ──────────────────────────────────────────────────────────────────────────────

    private async Task<List<DeviceConfiguration>> FetchDeviceConfigurationsAsync(CancellationToken ct)
    {
        var result = new List<DeviceConfiguration>();
        var resp = await _graphClient.DeviceManagement.DeviceConfigurations.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.DeviceConfigurations
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceManagementConfigurationPolicy>> FetchSettingsCatalogAsync(CancellationToken ct)
    {
        var result = new List<DeviceManagementConfigurationPolicy>();
        var resp = await _graphClient.DeviceManagement.ConfigurationPolicies.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "name", "platforms", "technologies", "templateReference"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.ConfigurationPolicies
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<GroupPolicyConfiguration>> FetchAdminTemplatesAsync(CancellationToken ct)
    {
        var result = new List<GroupPolicyConfiguration>();
        var resp = await _graphClient.DeviceManagement.GroupPolicyConfigurations.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.GroupPolicyConfigurations
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceCompliancePolicy>> FetchCompliancePoliciesAsync(CancellationToken ct)
    {
        var result = new List<DeviceCompliancePolicy>();
        var resp = await _graphClient.DeviceManagement.DeviceCompliancePolicies.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.DeviceCompliancePolicies
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<ManagedAppPolicy>> FetchAppProtectionPoliciesAsync(CancellationToken ct)
    {
        var result = new List<ManagedAppPolicy>();
        var resp = await _graphClient.DeviceAppManagement.ManagedAppPolicies.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceAppManagement.ManagedAppPolicies
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<ManagedDeviceMobileAppConfiguration>> FetchMobileAppConfigurationsAsync(CancellationToken ct)
    {
        var result = new List<ManagedDeviceMobileAppConfiguration>();
        var resp = await _graphClient.DeviceAppManagement.MobileAppConfigurations.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceAppManagement.MobileAppConfigurations
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<MobileApp>> FetchApplicationsAsync(CancellationToken ct)
    {
        var result = new List<MobileApp>();
        var resp = await _graphClient.DeviceAppManagement.MobileApps.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type", "isFeatured", "publisher"];
                req.QueryParameters.Filter = "isAssigned eq true";
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null)
                result.AddRange(resp.Value.Where(a => a.IsFeatured != true));
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceAppManagement.MobileApps
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceManagementScript>> FetchPlatformScriptsAsync(CancellationToken ct)
    {
        var result = new List<DeviceManagementScript>();
        var resp = await _graphClient.DeviceManagement.DeviceManagementScripts.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.DeviceManagementScripts
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceHealthScript>> FetchHealthScriptsAsync(CancellationToken ct)
    {
        var result = new List<DeviceHealthScript>();
        var resp = await _graphClient.DeviceManagement.DeviceHealthScripts.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.DeviceHealthScripts
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceManagementIntent>> FetchEndpointSecurityIntentsAsync(CancellationToken ct)
    {
        var result = new List<DeviceManagementIntent>();
        var resp = await _graphClient.DeviceManagement.Intents.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "templateId"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.Intents
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    private async Task<List<DeviceEnrollmentConfiguration>> FetchEnrollmentConfigurationsAsync(CancellationToken ct)
    {
        var result = new List<DeviceEnrollmentConfiguration>();
        var resp = await _graphClient.DeviceManagement.DeviceEnrollmentConfigurations.GetAsync(
            req =>
            {
                req.QueryParameters.Select = ["id", "displayName", "@odata.type"];
                req.QueryParameters.Top = 999;
            }, ct);
        while (resp != null)
        {
            if (resp.Value != null) result.AddRange(resp.Value);
            if (!string.IsNullOrEmpty(resp.OdataNextLink))
                resp = await _graphClient.DeviceManagement.DeviceEnrollmentConfigurations
                    .WithUrl(resp.OdataNextLink).GetAsync(cancellationToken: ct);
            else break;
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    //  Utility
    // ──────────────────────────────────────────────────────────────────────────────

    private static string PlatformFromOData(string? odataType)
    {
        if (string.IsNullOrEmpty(odataType)) return "";
        var lower = odataType.ToLowerInvariant();
        if (lower.Contains("windows") || lower.Contains("win32") || lower.Contains("msi")) return "Windows";
        if (lower.Contains("ios") || lower.Contains("iphone")) return "iOS";
        if (lower.Contains("macos") || lower.Contains("mac")) return "macOS";
        if (lower.Contains("android")) return "Android";
        if (lower.Contains("web")) return "Web";
        return "";
    }

    private static readonly Comparison<AssignmentReportRow> PolicyTypeNameComparer = (a, b) =>
    {
        var t = string.Compare(a.PolicyType, b.PolicyType, StringComparison.OrdinalIgnoreCase);
        return t != 0 ? t : string.Compare(a.PolicyName, b.PolicyName, StringComparison.OrdinalIgnoreCase);
    };
}
