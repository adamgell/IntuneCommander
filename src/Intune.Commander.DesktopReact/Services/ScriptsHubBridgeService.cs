using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ScriptsHubBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyPowerShell = "DeviceManagementScripts";
    private const string CacheKeyShell = "DeviceShellScripts";
    private const string CacheKeyCompliance = "ComplianceScripts";
    private const string CacheKeyHealth = "DeviceHealthScripts";

    private IDeviceManagementScriptService? _psService;
    private IDeviceShellScriptService? _shellService;
    private IComplianceScriptService? _complianceService;
    private IDeviceHealthScriptService? _healthService;

    public ScriptsHubBridgeService(
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
        _psService = null;
        _shellService = null;
        _complianceService = null;
        _healthService = null;
    }

    public async Task<object> ListAllAsync()
    {
        var client = GetClient();
        var tenantId = GetTenantId();

        _psService ??= new DeviceManagementScriptService(client);
        _shellService ??= new DeviceShellScriptService(client);
        _complianceService ??= new ComplianceScriptService(client);
        _healthService ??= new DeviceHealthScriptService(client);

        // Fetch all script types in parallel with caching
        var psTask = GetCachedOrFetch<DeviceManagementScript>(tenantId, CacheKeyPowerShell,
            () => _psService.ListDeviceManagementScriptsAsync());
        var shellTask = GetCachedOrFetch<DeviceShellScript>(tenantId, CacheKeyShell,
            () => _shellService.ListDeviceShellScriptsAsync());
        var complianceTask = GetCachedOrFetch<DeviceComplianceScript>(tenantId, CacheKeyCompliance,
            () => _complianceService.ListComplianceScriptsAsync());
        var healthTask = GetCachedOrFetch<DeviceHealthScript>(tenantId, CacheKeyHealth,
            () => _healthService.ListDeviceHealthScriptsAsync());

        await Task.WhenAll(psTask, shellTask, complianceTask, healthTask);

        var items = new List<ScriptListItemDto>();

        foreach (var s in await psTask)
        {
            items.Add(new ScriptListItemDto(
                Id: s.Id ?? "",
                DisplayName: s.DisplayName ?? "",
                Description: s.Description,
                ScriptType: "powershell",
                Platform: "Windows",
                CreatedDateTime: s.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: s.LastModifiedDateTime?.ToString("o") ?? "",
                RunAsAccount: s.RunAsAccount?.ToString() ?? "System",
                RunAs32Bit: s.RunAs32Bit,
                EnforceSignatureCheck: s.EnforceSignatureCheck,
                HasRemediation: null, Status: null,
                NoIssueDetectedCount: null, IssueDetectedCount: null, IssueRemediatedCount: null));
        }

        foreach (var s in await shellTask)
        {
            items.Add(new ScriptListItemDto(
                Id: s.Id ?? "",
                DisplayName: s.DisplayName ?? "",
                Description: s.Description,
                ScriptType: "shell",
                Platform: "macOS/Linux",
                CreatedDateTime: s.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: s.LastModifiedDateTime?.ToString("o") ?? "",
                RunAsAccount: s.RunAsAccount?.ToString() ?? "System",
                RunAs32Bit: null,
                EnforceSignatureCheck: null,
                HasRemediation: null, Status: null,
                NoIssueDetectedCount: null, IssueDetectedCount: null, IssueRemediatedCount: null));
        }

        foreach (var s in await complianceTask)
        {
            items.Add(new ScriptListItemDto(
                Id: s.Id ?? "",
                DisplayName: s.DisplayName ?? "",
                Description: s.Description,
                ScriptType: "compliance",
                Platform: "Windows",
                CreatedDateTime: s.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: s.LastModifiedDateTime?.ToString("o") ?? "",
                RunAsAccount: s.RunAsAccount?.ToString() ?? "System",
                RunAs32Bit: s.RunAs32Bit,
                EnforceSignatureCheck: s.EnforceSignatureCheck,
                HasRemediation: null, Status: null,
                NoIssueDetectedCount: null, IssueDetectedCount: null, IssueRemediatedCount: null));
        }

        foreach (var s in await healthTask)
        {
            items.Add(new ScriptListItemDto(
                Id: s.Id ?? "",
                DisplayName: s.DisplayName ?? "",
                Description: s.Description,
                ScriptType: "health",
                Platform: "Windows",
                CreatedDateTime: s.CreatedDateTime?.ToString("o") ?? "",
                LastModifiedDateTime: s.LastModifiedDateTime?.ToString("o") ?? "",
                RunAsAccount: s.RunAsAccount?.ToString() ?? "System",
                RunAs32Bit: s.RunAs32Bit,
                EnforceSignatureCheck: s.EnforceSignatureCheck,
                HasRemediation: s.RemediationScriptContent is { Length: > 0 },
                Status: null,
                NoIssueDetectedCount: null, IssueDetectedCount: null, IssueRemediatedCount: null));
        }

        return items.ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Payload is required");

        var p = payload.Value;
        var id = p.GetProperty("id").GetString()
            ?? throw new ArgumentException("Script ID is required");
        var scriptType = p.GetProperty("scriptType").GetString()
            ?? throw new ArgumentException("Script type is required");

        var client = GetClient();

        return scriptType switch
        {
            "powershell" => await GetPowerShellDetail(id, client),
            "shell" => await GetShellDetail(id, client),
            "compliance" => await GetComplianceDetail(id, client),
            "health" => await GetHealthDetail(id, client),
            _ => throw new ArgumentException($"Unknown script type: {scriptType}")
        };
    }

    private async Task<ScriptDetailDto> GetPowerShellDetail(string id, Microsoft.Graph.Beta.GraphServiceClient client)
    {
        _psService ??= new DeviceManagementScriptService(client);
        var script = await _psService.GetDeviceManagementScriptAsync(id)
            ?? throw new InvalidOperationException($"Script {id} not found");
        var assignments = await _psService.GetAssignmentsAsync(id);
        var groupNames = await ResolveGroupNames(assignments.Select(a => a.Target).ToList(), client);

        return new ScriptDetailDto(
            Id: script.Id ?? "",
            DisplayName: script.DisplayName ?? "",
            Description: script.Description,
            ScriptType: "powershell",
            Platform: "Windows",
            CreatedDateTime: script.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: script.LastModifiedDateTime?.ToString("o") ?? "",
            RunAsAccount: script.RunAsAccount?.ToString() ?? "System",
            RunAs32Bit: script.RunAs32Bit,
            EnforceSignatureCheck: script.EnforceSignatureCheck,
            ScriptContent: DecodeScript(script.ScriptContent),
            RemediationScriptContent: null,
            Language: "powershell",
            Assignments: MapAssignments(assignments.Select(a => a.Target).ToList(), groupNames));
    }

    private async Task<ScriptDetailDto> GetShellDetail(string id, Microsoft.Graph.Beta.GraphServiceClient client)
    {
        _shellService ??= new DeviceShellScriptService(client);
        var script = await _shellService.GetDeviceShellScriptAsync(id)
            ?? throw new InvalidOperationException($"Script {id} not found");
        var assignments = await _shellService.GetAssignmentsAsync(id);
        var groupNames = await ResolveGroupNames(assignments.Select(a => a.Target).ToList(), client);

        return new ScriptDetailDto(
            Id: script.Id ?? "",
            DisplayName: script.DisplayName ?? "",
            Description: script.Description,
            ScriptType: "shell",
            Platform: "macOS/Linux",
            CreatedDateTime: script.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: script.LastModifiedDateTime?.ToString("o") ?? "",
            RunAsAccount: script.RunAsAccount?.ToString() ?? "System",
            RunAs32Bit: null,
            EnforceSignatureCheck: null,
            ScriptContent: DecodeScript(script.ScriptContent),
            RemediationScriptContent: null,
            Language: "shell",
            Assignments: MapAssignments(assignments.Select(a => a.Target).ToList(), groupNames));
    }

    private async Task<ScriptDetailDto> GetComplianceDetail(string id, Microsoft.Graph.Beta.GraphServiceClient client)
    {
        _complianceService ??= new ComplianceScriptService(client);
        var script = await _complianceService.GetComplianceScriptAsync(id)
            ?? throw new InvalidOperationException($"Script {id} not found");

        return new ScriptDetailDto(
            Id: script.Id ?? "",
            DisplayName: script.DisplayName ?? "",
            Description: script.Description,
            ScriptType: "compliance",
            Platform: "Windows",
            CreatedDateTime: script.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: script.LastModifiedDateTime?.ToString("o") ?? "",
            RunAsAccount: script.RunAsAccount?.ToString() ?? "System",
            RunAs32Bit: script.RunAs32Bit,
            EnforceSignatureCheck: script.EnforceSignatureCheck,
            ScriptContent: DecodeScript(script.DetectionScriptContent),
            RemediationScriptContent: null,
            Language: "powershell",
            Assignments: []);
    }

    private async Task<ScriptDetailDto> GetHealthDetail(string id, Microsoft.Graph.Beta.GraphServiceClient client)
    {
        _healthService ??= new DeviceHealthScriptService(client);
        var script = await _healthService.GetDeviceHealthScriptAsync(id)
            ?? throw new InvalidOperationException($"Script {id} not found");
        var assignments = await _healthService.GetAssignmentsAsync(id);

        // Resolve group names for health script assignments
        var targets = assignments.Select(a => a.Target as DeviceAndAppManagementAssignmentTarget).ToList();
        var groupNames = await ResolveGroupNames(targets!, client);

        return new ScriptDetailDto(
            Id: script.Id ?? "",
            DisplayName: script.DisplayName ?? "",
            Description: script.Description,
            ScriptType: "health",
            Platform: "Windows",
            CreatedDateTime: script.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: script.LastModifiedDateTime?.ToString("o") ?? "",
            RunAsAccount: script.RunAsAccount?.ToString() ?? "System",
            RunAs32Bit: script.RunAs32Bit,
            EnforceSignatureCheck: script.EnforceSignatureCheck,
            ScriptContent: DecodeScript(script.DetectionScriptContent),
            RemediationScriptContent: DecodeScript(script.RemediationScriptContent),
            Language: "powershell",
            Assignments: MapAssignments(targets!, groupNames));
    }

    private async Task<List<T>> GetCachedOrFetch<T>(string? tenantId, string cacheKey, Func<Task<List<T>>> fetcher) where T : class
    {
        if (tenantId is not null)
        {
            var cached = _cache.Get<T>(tenantId, cacheKey);
            if (cached is { Count: > 0 })
                return cached;
        }

        var data = await fetcher();

        if (tenantId is not null)
            _cache.Set(tenantId, cacheKey, data);

        return data;
    }

    private static string DecodeScript(byte[]? content)
    {
        if (content is null or { Length: 0 }) return "";
        try { return System.Text.Encoding.UTF8.GetString(content); }
        catch { return "[Unable to decode script content]"; }
    }

    private static async Task<Dictionary<string, string>> ResolveGroupNames(
        List<DeviceAndAppManagementAssignmentTarget> targets,
        Microsoft.Graph.Beta.GraphServiceClient client)
    {
        var groupIds = targets
            .OfType<GroupAssignmentTarget>().Select(g => g.GroupId)
            .Concat(targets.OfType<ExclusionGroupAssignmentTarget>().Select(g => g.GroupId))
            .Where(id => id is not null)
            .Distinct()
            .ToList();

        var names = new Dictionary<string, string>();
        var sem = new SemaphoreSlim(5);

        var tasks = groupIds.Select(async gid =>
        {
            await sem.WaitAsync();
            try
            {
                var g = await client.Groups[gid].GetAsync(r => r.QueryParameters.Select = ["displayName"]);
                if (g?.DisplayName is not null)
                    lock (names) names[gid!] = g.DisplayName;
            }
            catch { /* fallback to GUID */ }
            finally { sem.Release(); }
        });

        await Task.WhenAll(tasks);
        return names;
    }

    private static ScriptAssignmentDto[] MapAssignments(
        List<DeviceAndAppManagementAssignmentTarget> targets,
        Dictionary<string, string> groupNames)
    {
        return targets.Select(t => t switch
        {
            AllDevicesAssignmentTarget => new ScriptAssignmentDto("All Devices", "Include"),
            AllLicensedUsersAssignmentTarget => new ScriptAssignmentDto("All Users", "Include"),
            ExclusionGroupAssignmentTarget excl => new ScriptAssignmentDto(
                groupNames.GetValueOrDefault(excl.GroupId ?? "") ?? excl.GroupId ?? "Unknown", "Exclude"),
            GroupAssignmentTarget grp => new ScriptAssignmentDto(
                groupNames.GetValueOrDefault(grp.GroupId ?? "") ?? grp.GroupId ?? "Unknown", "Include"),
            _ => new ScriptAssignmentDto("Unknown", "Unknown")
        }).ToArray();
    }
}
