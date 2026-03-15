using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Intune.Commander.Core.Models;
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
            LastModified: p.LastModifiedDateTime?.ToString("o") ?? "",
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
        var items = new List<(string Category, string Label, string Value)>();

        foreach (var setting in settings)
        {
            FlattenSettingInstance(setting.SettingInstance, items, 0);
        }

        // Group by category
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

    private static void FlattenSettingInstance(
        DeviceManagementConfigurationSettingInstance? instance,
        List<(string Category, string Label, string Value)> items,
        int depth)
    {
        if (instance is null || depth > 5) return;

        var definitionId = instance.SettingDefinitionId;

        switch (instance)
        {
            case DeviceManagementConfigurationChoiceSettingInstance choice:
            {
                var value = NormalizeDisplayValue(definitionId, choice.ChoiceSettingValue?.Value);
                AddItem(items, definitionId, value);
                if (choice.ChoiceSettingValue?.Children is { Count: > 0 } children)
                {
                    foreach (var child in children)
                        FlattenSettingInstance(child, items, depth + 1);
                }
                break;
            }

            case DeviceManagementConfigurationChoiceSettingCollectionInstance choiceColl:
            {
                if (choiceColl.ChoiceSettingCollectionValue is { Count: > 0 } vals)
                {
                    var formatted = vals.Select(v => NormalizeDisplayValue(definitionId, v.Value));
                    AddItem(items, definitionId, string.Join(", ", formatted));
                }
                else
                {
                    AddItem(items, definitionId, "(empty)");
                }
                break;
            }

            case DeviceManagementConfigurationSimpleSettingInstance simple:
            {
                var value = ExtractSimpleValue(simple.SimpleSettingValue);
                AddItem(items, definitionId, NormalizeDisplayValue(definitionId, value));
                break;
            }

            case DeviceManagementConfigurationSimpleSettingCollectionInstance simpleColl:
            {
                if (simpleColl.SimpleSettingCollectionValue is { Count: > 0 } vals)
                {
                    var formatted = vals.Select(ExtractSimpleValue);
                    AddItem(items, definitionId, string.Join(", ", formatted));
                }
                else
                {
                    AddItem(items, definitionId, "(empty)");
                }
                break;
            }

            case DeviceManagementConfigurationGroupSettingInstance group:
            {
                if (group.GroupSettingValue?.Children is { Count: > 0 } children)
                {
                    foreach (var child in children)
                        FlattenSettingInstance(child, items, depth + 1);
                }
                break;
            }

            case DeviceManagementConfigurationGroupSettingCollectionInstance groupColl:
            {
                if (groupColl.GroupSettingCollectionValue is { Count: > 0 } groups)
                {
                    foreach (var g in groups)
                    {
                        if (g.Children is { Count: > 0 } children)
                        {
                            foreach (var child in children)
                                FlattenSettingInstance(child, items, depth + 1);
                        }
                    }
                }
                break;
            }

            default:
            {
                var value = ExtractSettingInstanceValue(instance);
                AddItem(items, definitionId, value);
                break;
            }
        }
    }

    private static void AddItem(
        List<(string Category, string Label, string Value)> items,
        string? definitionId, string value)
    {
        var label = FormatSettingLabel(definitionId);
        var category = ResolveCategoryForSetting(definitionId);
        items.Add((category, label, value));
    }

    private static string NormalizeDisplayValue(string? definitionId, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Not Configured";

        var optionDisplayName = SettingsCatalogDefinitionRegistry.ResolveOptionDisplayName(definitionId, value);
        if (!string.IsNullOrWhiteSpace(optionDisplayName)) return optionDisplayName;

        if (value.Contains("_vendor_msft_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("_config_", StringComparison.OrdinalIgnoreCase) ||
            value.Contains('_'))
        {
            var lastSegment = value.Split('_', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? value;
            return HumanizeToken(lastSegment);
        }

        if (!value.Contains(' ') && Regex.IsMatch(value, "[a-z][A-Z]"))
            return FormatPascalCase(value);

        return value;
    }

    private static string FormatSettingLabel(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "Unknown Setting";

        var displayName = SettingsCatalogDefinitionRegistry.ResolveDisplayName(id);
        if (!string.IsNullOrWhiteSpace(displayName)) return displayName;

        // Strip common prefixes
        foreach (var prefix in new[]
        {
            "device_vendor_msft_policy_config_",
            "user_vendor_msft_policy_config_",
            "device_vendor_msft_",
            "user_vendor_msft_"
        })
        {
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                id = id[prefix.Length..];
                break;
            }
        }

        var parts = id.Split('_', StringSplitOptions.RemoveEmptyEntries);

        // Skip the first segment (category) since it's already shown as the group name
        var labelParts = parts.Length > 1 ? parts[1..] : parts;

        // Show the last meaningful segment as the label (the actual setting name)
        var settingName = labelParts.Length > 0 ? labelParts[^1] : parts[^1];

        // Insert spaces before uppercase letters in camelCase/PascalCase names
        var spaced = Regex.Replace(settingName, @"(?<=[a-z0-9])(?=[A-Z])", " ");

        // Title-case the first letter
        return char.ToUpper(spaced[0]) + spaced[1..];
    }

    private static string ResolveCategoryForSetting(string? definitionId)
    {
        var definition = SettingsCatalogDefinitionRegistry.ResolveDefinition(definitionId);
        if (!string.IsNullOrWhiteSpace(definition?.CategoryId))
        {
            var categoryName = SettingsCatalogDefinitionRegistry.ResolveCategoryName(definition.CategoryId);
            if (!string.IsNullOrWhiteSpace(categoryName))
                return categoryName;
        }

        // Fallback: extract category from definition ID path
        if (string.IsNullOrEmpty(definitionId)) return "General";

        foreach (var prefix in new[]
        {
            "device_vendor_msft_policy_config_",
            "user_vendor_msft_policy_config_",
            "device_vendor_msft_",
            "user_vendor_msft_"
        })
        {
            if (definitionId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var remaining = definitionId[prefix.Length..];
                var categoryToken = remaining.Split('_', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(categoryToken))
                    return HumanizeToken(categoryToken);
            }
        }

        return "General";
    }

    private static string HumanizeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var spaced = token.Replace('_', ' ');
        spaced = Regex.Replace(spaced, @"\s+", " ").Trim();
        spaced = FormatPascalCase(spaced);

        // Title-case each word for readability (handles all-lowercase tokens like "allowencryptionoracle")
        return Regex.Replace(spaced, @"\b(\w)", m => m.Value.ToUpperInvariant());
    }

    private static string FormatPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var spaced = Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])", " ");
        return char.ToUpper(spaced[0]) + spaced[1..];
    }

    private static string ExtractSettingInstanceValue(DeviceManagementConfigurationSettingInstance? instance)
    {
        return instance switch
        {
            DeviceManagementConfigurationSimpleSettingInstance s =>
                ExtractSimpleValue(s.SimpleSettingValue),

            DeviceManagementConfigurationSimpleSettingCollectionInstance sc =>
                sc.SimpleSettingCollectionValue is { Count: > 0 } vals
                    ? string.Join(", ", vals.Select(ExtractSimpleValue))
                    : "(empty)",

            DeviceManagementConfigurationChoiceSettingInstance c =>
                c.ChoiceSettingValue?.Value?.Split('_').LastOrDefault() ?? "",

            DeviceManagementConfigurationChoiceSettingCollectionInstance cc =>
                cc.ChoiceSettingCollectionValue is { Count: > 0 } cvals
                    ? string.Join(", ", cvals.Select(v => v.Value?.Split('_').LastOrDefault() ?? ""))
                    : "(empty)",

            DeviceManagementConfigurationGroupSettingInstance g =>
                $"[{g.GroupSettingValue?.Children?.Count ?? 0} child setting(s)]",

            DeviceManagementConfigurationGroupSettingCollectionInstance gc =>
                $"[{gc.GroupSettingCollectionValue?.Count ?? 0} group(s)]",

            _ => instance?.OdataType?.Split('.').LastOrDefault() ?? ""
        };
    }

    private static string ExtractSimpleValue(DeviceManagementConfigurationSimpleSettingValue? v)
    {
        return v switch
        {
            DeviceManagementConfigurationStringSettingValue sv => sv.Value ?? "",
            DeviceManagementConfigurationIntegerSettingValue iv => iv.Value?.ToString() ?? "",
            DeviceManagementConfigurationSecretSettingValue sec => $"[secret: {sec.ValueState}]",
            _ => v?.AdditionalData != null && v.AdditionalData.TryGetValue("value", out var raw) ? raw?.ToString() ?? "" : ""
        };
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
