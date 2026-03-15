using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta.Models.ODataErrors;

namespace Intune.Commander.DesktopReact.Services;

public class DeviceHealthScriptBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyHealthScripts = "DeviceHealthScripts";

    private IDeviceHealthScriptService? _service;

    private const string CacheKeyDevices = "ManagedDevices";

    public DeviceHealthScriptBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IDeviceHealthScriptService GetService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _service ??= new DeviceHealthScriptService(client);
        return _service;
    }

    public void Reset()
    {
        _service = null;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        var service = GetService();

        // Try cache first
        if (tenantId is not null)
        {
            var cached = _cache.Get<DeviceHealthScript>(tenantId, CacheKeyHealthScripts);
            if (cached is { Count: > 0 })
                return await EnrichWithRunSummaries(cached, service);
        }

        var scripts = await service.ListDeviceHealthScriptsAsync();

        // Store in cache
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyHealthScripts, scripts);

        return await EnrichWithRunSummaries(scripts, service);
    }

    private async Task<HealthScriptListItem[]> EnrichWithRunSummaries(
        List<DeviceHealthScript> scripts, IDeviceHealthScriptService service)
    {
        var client = _authBridge.GraphClient;

        // Fetch run summaries + script details in parallel (max 5 concurrent)
        var summaries = new Dictionary<string, DeviceHealthScriptRunSummary>();
        var hasRemediation = new Dictionary<string, bool>();
        var semaphore = new SemaphoreSlim(5);

        var tasks = scripts.Where(s => s.Id is not null).Select(async s =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Fetch run summary
                var summary = await service.GetRunSummaryAsync(s.Id!);
                if (summary is not null)
                    lock (summaries) summaries[s.Id!] = summary;

                // Fetch script to check for remediation content
                if (client is not null)
                {
                    var detail = await client.DeviceManagement.DeviceHealthScripts[s.Id!]
                        .GetAsync(req => req.QueryParameters.Select =
                            ["id", "remediationScriptContent"]);
                    lock (hasRemediation)
                        hasRemediation[s.Id!] = detail?.RemediationScriptContent is { Length: > 0 };
                }
            }
            catch
            {
                // Silently skip
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return MapScripts(scripts, summaries, hasRemediation);
    }

    private static HealthScriptListItem[] MapScripts(
        List<DeviceHealthScript> scripts,
        Dictionary<string, DeviceHealthScriptRunSummary>? summaries = null,
        Dictionary<string, bool>? remediationFlags = null)
    {
        return scripts.Select(s =>
        {
            DeviceHealthScriptRunSummary? rs = null;
            summaries?.TryGetValue(s.Id ?? "", out rs);
            var hasRemediation = remediationFlags?.GetValueOrDefault(s.Id ?? "", false)
                ?? s.RemediationScriptContent is { Length: > 0 };
            var noIssue = rs?.NoIssueDetectedDeviceCount ?? 0;
            var issueDetected = rs?.IssueDetectedDeviceCount ?? 0;
            var issueRemediated = rs?.IssueRemediatedDeviceCount ?? 0;
            var issueReoccurred = rs?.IssueReoccurredDeviceCount ?? 0;
            var totalDevices = noIssue + issueDetected + issueRemediated + issueReoccurred;

            var status = totalDevices > 0 ? "Active" : "Not deployed";

            return new HealthScriptListItem(
                Id: s.Id ?? "",
                DisplayName: s.DisplayName ?? "",
                Description: s.Description,
                Publisher: s.Publisher ?? "",
                Version: s.Version ?? "",
                RunAsAccount: s.RunAsAccount?.ToString() ?? "System",
                RunAs32Bit: s.RunAs32Bit ?? false,
                EnforceSignatureCheck: s.EnforceSignatureCheck ?? false,
                IsGlobal: s.IsGlobalScript ?? false,
                CreatedDateTime: s.CreatedDateTime?.ToString("o") ?? "",
                LastModified: s.LastModifiedDateTime?.ToString("o") ?? "",
                DeviceHealthScriptType: (int)(s.DeviceHealthScriptType ?? 0),
                HasRemediation: hasRemediation,
                Status: status,
                NoIssueDetectedCount: noIssue,
                IssueDetectedCount: issueDetected,
                IssueRemediatedCount: issueRemediated,
                IssueReoccurredCount: issueReoccurred,
                TotalRemediatedCount: issueRemediated + issueReoccurred
            );
        }).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Script ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Script ID is required");
        var service = GetService();

        // Parallel fetch: script + assignments + run summary + device states
        var scriptTask = service.GetDeviceHealthScriptAsync(id);
        var assignmentsTask = service.GetAssignmentsAsync(id);
        var runSummaryTask = service.GetRunSummaryAsync(id);
        var deviceStatesTask = service.GetDeviceRunStatesAsync(id);

        await Task.WhenAll(scriptTask, assignmentsTask, runSummaryTask, deviceStatesTask);

        var script = await scriptTask ?? throw new InvalidOperationException($"Script {id} not found");
        var assignments = await assignmentsTask;
        var runSummary = await runSummaryTask;
        var deviceStates = await deviceStatesTask;

        // Resolve group GUIDs to display names
        var resolvedAssignments = await ResolveAssignmentGroupNamesAsync(assignments);

        return new HealthScriptDetail(
            Id: script.Id ?? "",
            DisplayName: script.DisplayName ?? "",
            Description: script.Description,
            Publisher: script.Publisher ?? "",
            Version: script.Version ?? "",
            RunAsAccount: script.RunAsAccount?.ToString() ?? "System",
            RunAs32Bit: script.RunAs32Bit ?? false,
            EnforceSignatureCheck: script.EnforceSignatureCheck ?? false,
            IsGlobal: script.IsGlobalScript ?? false,
            CreatedDateTime: script.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: script.LastModifiedDateTime?.ToString("o") ?? "",
            RoleScopeTagIds: (script.RoleScopeTagIds ?? []).ToArray(),
            DetectionScript: DecodeScript(script.DetectionScriptContent),
            RemediationScript: DecodeScript(script.RemediationScriptContent),
            RunSummary: runSummary is not null ? new RunSummaryDto(
                NoIssueDetectedCount: runSummary.NoIssueDetectedDeviceCount ?? 0,
                IssueDetectedCount: runSummary.IssueDetectedDeviceCount ?? 0,
                IssueRemediatedCount: runSummary.IssueRemediatedDeviceCount ?? 0,
                IssueReoccurredCount: runSummary.IssueReoccurredDeviceCount ?? 0,
                ErrorDeviceCount: runSummary.DetectionScriptErrorDeviceCount ?? 0,
                LastScriptRunDateTime: runSummary.LastScriptRunDateTime?.ToString("o")
            ) : null,
            DeviceRunStates: deviceStates.Select(ds => new DeviceRunStateDto(
                DeviceName: ds.ManagedDevice?.DeviceName ?? ds.Id ?? "Unknown",
                DetectionState: ds.DetectionState?.ToString() ?? "Unknown",
                RemediationState: ds.RemediationState?.ToString() ?? "Unknown",
                LastStateUpdateDateTime: ds.LastStateUpdateDateTime?.ToString("o") ?? "",
                UserPrincipalName: ds.ManagedDevice?.UserPrincipalName,
                PreRemediationDetectionScriptOutput: ds.PreRemediationDetectionScriptOutput,
                PreRemediationDetectionScriptError: ds.PreRemediationDetectionScriptError,
                PostRemediationDetectionScriptOutput: ds.PostRemediationDetectionScriptOutput,
                PostRemediationDetectionScriptError: ds.PostRemediationDetectionScriptError,
                RemediationScriptError: ds.RemediationScriptError,
                ExpectedStateUpdateDateTime: ds.ExpectedStateUpdateDateTime?.ToString("o"),
                LastSyncDateTime: ds.LastSyncDateTime?.ToString("o")
            )).ToArray(),
            Assignments: resolvedAssignments);
    }

    public async Task<object> UpdateAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Update payload is required");

        var p = payload.Value;
        var id = p.GetProperty("id").GetString()
            ?? throw new ArgumentException("Script ID is required");

        var service = GetService();
        var script = new DeviceHealthScript
        {
            Id = id
        };

        const int maxScriptLength = 200_000; // ~200 KB per script

        // Only patch fields that were sent
        if (p.TryGetProperty("displayName", out var dn))
        {
            var name = dn.GetString();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Display name cannot be empty");
            script.DisplayName = name;
        }
        if (p.TryGetProperty("description", out var desc))
            script.Description = desc.GetString();
        if (p.TryGetProperty("detectionScript", out var det))
        {
            var content = det.GetString() ?? "";
            if (content.Length > maxScriptLength)
                throw new ArgumentException($"Detection script exceeds maximum length of {maxScriptLength} characters");
            script.DetectionScriptContent = System.Text.Encoding.UTF8.GetBytes(content);
        }
        if (p.TryGetProperty("remediationScript", out var rem))
        {
            var content = rem.GetString() ?? "";
            if (content.Length > maxScriptLength)
                throw new ArgumentException($"Remediation script exceeds maximum length of {maxScriptLength} characters");
            script.RemediationScriptContent = System.Text.Encoding.UTF8.GetBytes(content);
        }

        var updated = await service.UpdateDeviceHealthScriptAsync(script);

        // Invalidate cache so next list fetch is fresh
        var tenantId = GetTenantId();
        if (tenantId is not null)
            _cache.Invalidate(tenantId, CacheKeyHealthScripts);

        return new { success = true, id = updated.Id };
    }

    public async Task<object> DeployAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Deploy payload is required");

        var p = payload.Value;
        var scriptId = p.GetProperty("scriptId").GetString()
            ?? throw new ArgumentException("Script ID is required");

        var deviceIds = new List<(string id, string name)>();
        if (p.TryGetProperty("devices", out var devicesArr) && devicesArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in devicesArr.EnumerateArray())
            {
                var id = d.GetProperty("id").GetString() ?? "";
                var name = d.TryGetProperty("deviceName", out var n) ? n.GetString() ?? "" : "";
                deviceIds.Add((id, name));
            }
        }

        if (deviceIds.Count == 0)
            throw new ArgumentException("At least one device is required");

        var service = GetService();
        var records = new List<DeploymentRecordDto>();
        var now = DateTimeOffset.UtcNow.ToString("o");

        // Deploy to each device (sequentially to avoid throttling)
        foreach (var (deviceId, deviceName) in deviceIds)
        {
            try
            {
                await service.InitiateOnDemandRemediationAsync(deviceId, scriptId);
                records.Add(new DeploymentRecordDto(deviceId, deviceName, true, null, now));
            }
            catch (Exception ex)
            {
                var msg = ex is ODataError odata ? odata.Error?.Message ?? ex.Message : ex.Message;
                records.Add(new DeploymentRecordDto(deviceId, deviceName, false, msg, now));
            }
        }

        return records.ToArray();
    }

    public async Task<object> RefreshRunStatesAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Script ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Script ID is required");
        var service = GetService();
        var deviceStates = await service.GetDeviceRunStatesAsync(id);

        return deviceStates.Select(ds => new DeviceRunStateDto(
            DeviceName: ds.ManagedDevice?.DeviceName ?? ds.Id ?? "Unknown",
            DetectionState: ds.DetectionState?.ToString() ?? "Unknown",
            RemediationState: ds.RemediationState?.ToString() ?? "Unknown",
            LastStateUpdateDateTime: ds.LastStateUpdateDateTime?.ToString("o") ?? "",
            UserPrincipalName: ds.ManagedDevice?.UserPrincipalName,
            PreRemediationDetectionScriptOutput: ds.PreRemediationDetectionScriptOutput,
            PreRemediationDetectionScriptError: ds.PreRemediationDetectionScriptError,
            PostRemediationDetectionScriptOutput: ds.PostRemediationDetectionScriptOutput,
            PostRemediationDetectionScriptError: ds.PostRemediationDetectionScriptError,
            RemediationScriptError: ds.RemediationScriptError,
            ExpectedStateUpdateDateTime: ds.ExpectedStateUpdateDateTime?.ToString("o"),
            LastSyncDateTime: ds.LastSyncDateTime?.ToString("o")
        )).ToArray();
    }

    private static string DecodeScript(byte[]? content)
    {
        if (content is null or { Length: 0 }) return "";
        try
        {
            return System.Text.Encoding.UTF8.GetString(content);
        }
        catch
        {
            return "[Unable to decode script content]";
        }
    }

    private async Task<HealthScriptAssignmentDto[]> ResolveAssignmentGroupNamesAsync(
        List<DeviceHealthScriptAssignment> assignments)
    {
        var client = _authBridge.GraphClient;
        if (client is null)
            return MapAssignments(assignments, new Dictionary<string, string>());

        // Collect unique group IDs
        var groupIds = assignments
            .Select(a => a.Target)
            .OfType<GroupAssignmentTarget>()
            .Select(g => g.GroupId)
            .Concat(assignments
                .Select(a => a.Target)
                .OfType<ExclusionGroupAssignmentTarget>()
                .Select(g => g.GroupId))
            .Where(id => id is not null)
            .Distinct()
            .ToList();

        var nameMap = new Dictionary<string, string>();

        // Resolve in parallel, max 5 concurrent
        var semaphore = new SemaphoreSlim(5);
        var tasks = groupIds.Select(async groupId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var group = await client.Groups[groupId].GetAsync(req =>
                    req.QueryParameters.Select = ["displayName"]);
                if (group?.DisplayName is not null)
                    lock (nameMap) nameMap[groupId!] = group.DisplayName;
            }
            catch
            {
                // If resolution fails, we'll fall back to showing the GUID
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return MapAssignments(assignments, nameMap);
    }

    private static HealthScriptAssignmentDto[] MapAssignments(
        List<DeviceHealthScriptAssignment> assignments,
        Dictionary<string, string>? groupNames = null)
    {
        return assignments.Select(a =>
        {
            var (target, kind) = a.Target switch
            {
                AllDevicesAssignmentTarget => ("All Devices", "Include"),
                AllLicensedUsersAssignmentTarget => ("All Users", "Include"),
                ExclusionGroupAssignmentTarget excl => (
                    groupNames?.GetValueOrDefault(excl.GroupId ?? "") ?? excl.GroupId ?? "Unknown",
                    "Exclude"),
                GroupAssignmentTarget grp => (
                    groupNames?.GetValueOrDefault(grp.GroupId ?? "") ?? grp.GroupId ?? "Unknown",
                    "Include"),
                _ => ("Unknown", "Unknown")
            };

            var schedule = FormatSchedule(a.RunSchedule);
            var runRemediation = a.RunRemediationScript ?? false;

            return new HealthScriptAssignmentDto(target, kind, schedule, runRemediation);
        }).ToArray();
    }

    private static string FormatSchedule(DeviceHealthScriptRunSchedule? schedule)
    {
        return schedule switch
        {
            DeviceHealthScriptHourlySchedule hourly =>
                $"Every {hourly.Interval ?? 1} hour(s)",
            DeviceHealthScriptDailySchedule daily =>
                $"Daily at {daily.Time?.ToString() ?? "midnight"}",
            DeviceHealthScriptRunOnceSchedule once =>
                $"Once at {once.Date?.ToString() ?? "unknown"} {once.Time?.ToString() ?? ""}",
            _ => "Not configured"
        };
    }
}
