using System.Collections.Concurrent;
using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class SettingsCatalogBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private readonly ConcurrentDictionary<string, string> _groupNameCache = new(StringComparer.OrdinalIgnoreCase);

    private const string CacheKeySettingsCatalog = "SettingsCatalog";

    private ISettingsCatalogService? _service;

    public SettingsCatalogBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private ISettingsCatalogService GetService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        // Reuse existing service instance unless the graph client changed
        _service ??= new SettingsCatalogService(client);
        return _service;
    }

    public void Reset()
    {
        _service = null;
        _groupNameCache.Clear();
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        var service = GetService();

        // Try cache first
        if (tenantId is not null)
        {
            var cached = _cache.Get<DeviceManagementConfigurationPolicy>(tenantId, CacheKeySettingsCatalog);
            if (cached is { Count: > 0 })
                return MapPolicies(cached);
        }

        var policies = await service.ListSettingsCatalogPoliciesAsync();

        // Store in cache
        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeySettingsCatalog, policies);

        return MapPolicies(policies);
    }

    private static PolicyListItem[] MapPolicies(List<DeviceManagementConfigurationPolicy> policies)
    {
        return policies.Select(p => new PolicyListItem(
            Id: p.Id ?? "",
            Name: p.Name ?? "",
            Description: p.Description,
            Platform: FormatPlatforms(p.Platforms),
            ProfileType: FormatProfileType(p),
            Technologies: FormatTechnologies(p.Technologies),
            CreatedDateTime: p.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: p.LastModifiedDateTime?.ToString("o") ?? "",
            ScopeTag: FormatScopeTags(p.RoleScopeTagIds),
            IsAssigned: p.IsAssigned ?? false,
            SettingCount: p.SettingCount ?? 0
        )).ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Policy ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Policy ID is required");
        var service = GetService();

        // Parallel fetch: policy + assignments + settings
        var policyTask = service.GetSettingsCatalogPolicyAsync(id);
        var assignmentsTask = service.GetAssignmentsAsync(id);
        var settingsTask = service.GetPolicySettingsAsync(id);

        await Task.WhenAll(policyTask, assignmentsTask, settingsTask);

        var policy = await policyTask ?? throw new InvalidOperationException($"Policy {id} not found");
        var assignments = await assignmentsTask;
        var settings = await settingsTask;

        // Resolve assignment group names
        var assignmentData = await MapAssignmentsAsync(assignments);

        // Parse settings into grouped DTOs
        var settingGroups = ParseSettingGroups(settings);

        return new PolicyDetail(
            Id: policy.Id ?? "",
            Name: policy.Name ?? "",
            Description: policy.Description,
            Platform: FormatPlatforms(policy.Platforms),
            ProfileType: FormatProfileType(policy),
            Technologies: FormatTechnologies(policy.Technologies),
            CreatedDateTime: policy.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: policy.LastModifiedDateTime?.ToString("o") ?? "",
            ScopeTags: (policy.RoleScopeTagIds ?? []).Select(FormatScopeTagId).ToArray(),
            SettingCount: policy.SettingCount ?? 0,
            IsAssigned: policy.IsAssigned ?? false,
            TemplateReference: policy.TemplateReference?.TemplateDisplayName,
            Assignments: assignmentData,
            SettingGroups: settingGroups);
    }

    private async Task<AssignmentData> MapAssignmentsAsync(List<DeviceManagementConfigurationPolicyAssignment> assignments)
    {
        var included = new List<AssignmentEntry>();
        var excluded = new List<AssignmentEntry>();

        foreach (var a in assignments)
        {
            switch (a.Target)
            {
                case AllDevicesAssignmentTarget:
                    included.Add(new AssignmentEntry("All Devices", "Required", null, null));
                    break;
                case AllLicensedUsersAssignmentTarget:
                    included.Add(new AssignmentEntry("All Users", "Required", null, null));
                    break;
                case ExclusionGroupAssignmentTarget excl:
                    var exclName = await ResolveGroupNameAsync(excl.GroupId);
                    excluded.Add(new AssignmentEntry(exclName, "Excluded", null, null));
                    break;
                case GroupAssignmentTarget grp:
                    var grpName = await ResolveGroupNameAsync(grp.GroupId);
                    included.Add(new AssignmentEntry(grpName, "Required", null, null));
                    break;
            }
        }

        return new AssignmentData(included.ToArray(), excluded.ToArray());
    }

    private async Task<string> ResolveGroupNameAsync(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return "Unknown Group";
        if (!Guid.TryParse(groupId, out _)) return groupId;
        if (_groupNameCache.TryGetValue(groupId, out var cached)) return cached;

        try
        {
            var client = _authBridge.GraphClient;
            if (client != null)
            {
                var response = await client.Groups
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Filter = $"id eq '{groupId}'";
                        req.QueryParameters.Select = ["displayName"];
                        req.QueryParameters.Top = 1;
                    });

                var name = response?.Value?.FirstOrDefault()?.DisplayName ?? groupId;
                _groupNameCache[groupId] = name;
                return name;
            }
        }
        catch
        {
            // Fall back to showing the GUID
        }

        _groupNameCache[groupId] = groupId;
        return groupId;
    }

    private static SettingGroupDto[] ParseSettingGroups(List<DeviceManagementConfigurationSetting> settings)
    {
        var items = SettingsCatalogHelper.FlattenSettings(settings);

        return items
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .Select(g => new SettingGroupDto(
                Name: g.Key,
                SettingCount: g.Count(),
                Settings: g.OrderBy(s => s.Label)
                    .Select(s => new SettingEntryDto(s.Label, s.Value))
                    .ToArray()))
            .ToArray();
    }

    private static string FormatPlatforms(DeviceManagementConfigurationPlatforms? platforms)
    {
        if (platforms is null or DeviceManagementConfigurationPlatforms.None)
            return "None";

        var parts = new List<string>();
        if (platforms.Value.HasFlag(DeviceManagementConfigurationPlatforms.Windows10))
            parts.Add("Windows 10 and later");
        if (platforms.Value.HasFlag(DeviceManagementConfigurationPlatforms.MacOS))
            parts.Add("macOS");
        if (platforms.Value.HasFlag(DeviceManagementConfigurationPlatforms.IOS))
            parts.Add("iOS/iPadOS");
        if (platforms.Value.HasFlag(DeviceManagementConfigurationPlatforms.Android))
            parts.Add("Android Enterprise");
        if (platforms.Value.HasFlag(DeviceManagementConfigurationPlatforms.Linux))
            parts.Add("Linux");

        return parts.Count > 0 ? string.Join(", ", parts) : platforms.Value.ToString();
    }

    private static string FormatProfileType(DeviceManagementConfigurationPolicy policy)
    {
        // Prefer template display name, fall back to technologies
        var templateName = policy.TemplateReference?.TemplateDisplayName;
        if (!string.IsNullOrWhiteSpace(templateName))
            return templateName;

        return FormatTechnologies(policy.Technologies);
    }

    private static string FormatTechnologies(DeviceManagementConfigurationTechnologies? technologies)
    {
        if (technologies is null or DeviceManagementConfigurationTechnologies.None)
            return "Custom";

        var parts = new List<string>();
        if (technologies.Value.HasFlag(DeviceManagementConfigurationTechnologies.Mdm))
            parts.Add("MDM");
        if (technologies.Value.HasFlag(DeviceManagementConfigurationTechnologies.ConfigManager))
            parts.Add("Config Manager");
        if (technologies.Value.HasFlag(DeviceManagementConfigurationTechnologies.MicrosoftSense))
            parts.Add("Microsoft Sense");
        if (technologies.Value.HasFlag(DeviceManagementConfigurationTechnologies.Enrollment))
            parts.Add("Enrollment");

        return parts.Count > 0 ? string.Join(", ", parts) : "Settings catalog";
    }

    private static string FormatScopeTags(List<string>? tagIds)
    {
        if (tagIds is null or { Count: 0 })
            return "Default";

        return tagIds.All(t => t == "0") ? "Default" : string.Join(", ", tagIds.Select(FormatScopeTagId));
    }

    private static string FormatScopeTagId(string tagId)
        => tagId == "0" ? "Default" : $"Tag {tagId}";
}
