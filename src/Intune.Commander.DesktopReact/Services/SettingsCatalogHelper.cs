using System.Text.RegularExpressions;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

/// <summary>
/// Shared static helpers for resolving Settings Catalog definition IDs to
/// human-readable labels and values. Used by both SettingsCatalogBridgeService
/// and PolicyComparisonBridgeService.
/// </summary>
public static class SettingsCatalogHelper
{
    public static void FlattenSettingInstance(
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

    public static void AddItem(
        List<(string Category, string Label, string Value)> items,
        string? definitionId, string value)
    {
        var label = FormatSettingLabel(definitionId);
        var category = ResolveCategoryForSetting(definitionId);
        items.Add((category, label, value));
    }

    public static string NormalizeDisplayValue(string? definitionId, string? value)
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

    public static string FormatSettingLabel(string? id)
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
        if (parts.Length == 0) return "Unknown Setting";

        var labelParts = parts.Length > 1 ? parts[1..] : parts;
        var settingName = labelParts.Length > 0 ? labelParts[^1] : parts[^1];
        var spaced = Regex.Replace(settingName, @"(?<=[a-z0-9])(?=[A-Z])", " ");
        if (string.IsNullOrEmpty(spaced)) return "Unknown Setting";
        return char.ToUpper(spaced[0]) + spaced[1..];
    }

    public static string ResolveCategoryForSetting(string? definitionId)
    {
        var definition = SettingsCatalogDefinitionRegistry.ResolveDefinition(definitionId);
        if (!string.IsNullOrWhiteSpace(definition?.CategoryId))
        {
            var categoryName = SettingsCatalogDefinitionRegistry.ResolveCategoryName(definition.CategoryId);
            if (!string.IsNullOrWhiteSpace(categoryName))
                return categoryName;
        }

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

    public static string HumanizeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var spaced = token.Replace('_', ' ');
        spaced = Regex.Replace(spaced, @"\s+", " ").Trim();
        spaced = FormatPascalCase(spaced);
        return Regex.Replace(spaced, @"\b(\w)", m => m.Value.ToUpperInvariant());
    }

    public static string FormatPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var spaced = Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])", " ");
        return char.ToUpper(spaced[0]) + spaced[1..];
    }

    public static string ExtractSettingInstanceValue(DeviceManagementConfigurationSettingInstance? instance)
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

    public static string ExtractSimpleValue(DeviceManagementConfigurationSimpleSettingValue? v)
    {
        return v switch
        {
            DeviceManagementConfigurationStringSettingValue sv => sv.Value ?? "",
            DeviceManagementConfigurationIntegerSettingValue iv => iv.Value?.ToString() ?? "",
            DeviceManagementConfigurationSecretSettingValue sec => $"[secret: {sec.ValueState}]",
            _ => v?.AdditionalData != null && v.AdditionalData.TryGetValue("value", out var raw) ? raw?.ToString() ?? "" : ""
        };
    }

    /// <summary>
    /// Flatten a list of Settings Catalog settings into (category, label, value) triples.
    /// </summary>
    public static List<(string Category, string Label, string Value)> FlattenSettings(
        List<DeviceManagementConfigurationSetting> settings)
    {
        var items = new List<(string Category, string Label, string Value)>();
        foreach (var setting in settings)
            FlattenSettingInstance(setting.SettingInstance, items, 0);
        return items;
    }
}
