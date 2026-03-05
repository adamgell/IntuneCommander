using System.Text.Json;
using System.Text.Json.Serialization;

namespace Intune.Commander.Core.Models;

/// <summary>
/// Provides lazily-loaded, in-memory lookup of Intune Settings Catalog definitions
/// and categories from embedded JSON assets. The JSON is fetched daily by a GitHub
/// Actions workflow and baked into the .exe at build time.
/// <para>
/// To update: run <c>scripts/Fetch-SettingsCatalogDefinitions.ps1</c> or let the
/// <c>update-settings-catalog</c> workflow update the Assets automatically.
/// </para>
/// </summary>
public static class SettingsCatalogDefinitionRegistry
{
    private static readonly Lazy<IReadOnlyDictionary<string, SettingDefinitionEntry>> _definitions = new(LoadDefinitions);
    private static readonly Lazy<IReadOnlyDictionary<string, SettingCategoryEntry>> _categories = new(LoadCategories);

    /// <summary>
    /// All setting definitions keyed by definition ID. Loaded once on first access.
    /// </summary>
    public static IReadOnlyDictionary<string, SettingDefinitionEntry> Definitions => _definitions.Value;

    /// <summary>
    /// All setting categories keyed by category ID. Loaded once on first access.
    /// </summary>
    public static IReadOnlyDictionary<string, SettingCategoryEntry> Categories => _categories.Value;

    /// <summary>
    /// Resolves a setting definition ID to its human-readable display name.
    /// Returns <c>null</c> if not found in the embedded definitions.
    /// </summary>
    public static string? ResolveDisplayName(string? definitionId)
    {
        if (string.IsNullOrEmpty(definitionId)) return null;
        return Definitions.TryGetValue(definitionId, out var def) ? def.DisplayName : null;
    }

    /// <summary>
    /// Resolves a setting definition ID to its description.
    /// Returns <c>null</c> if not found.
    /// </summary>
    public static string? ResolveDescription(string? definitionId)
    {
        if (string.IsNullOrEmpty(definitionId)) return null;
        return Definitions.TryGetValue(definitionId, out var def) ? def.Description : null;
    }

    /// <summary>
    /// Resolves a category ID to its display name.
    /// Returns the original <paramref name="categoryId"/> if not found.
    /// </summary>
    public static string ResolveCategoryName(string? categoryId)
    {
        if (string.IsNullOrEmpty(categoryId)) return string.Empty;
        return Categories.TryGetValue(categoryId, out var cat) ? cat.DisplayName ?? categoryId : categoryId;
    }

    /// <summary>
    /// Returns whether embedded definitions are available (non-empty).
    /// Will be false if the placeholder JSON has not been replaced by the workflow.
    /// </summary>
    public static bool HasDefinitions => Definitions.Count > 0;

    private static IReadOnlyDictionary<string, SettingDefinitionEntry> LoadDefinitions()
    {
        var assembly = typeof(SettingsCatalogDefinitionRegistry).Assembly;
        const string resourceName = "Intune.Commander.Core.Assets.settings-catalog-definitions.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return new Dictionary<string, SettingDefinitionEntry>(StringComparer.OrdinalIgnoreCase);

        var entries = JsonSerializer.Deserialize<List<SettingDefinitionEntry>>(stream, JsonOptions) ?? [];

        var dict = new Dictionary<string, SettingDefinitionEntry>(entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Id))
                dict.TryAdd(entry.Id, entry);
        }
        return dict;
    }

    private static IReadOnlyDictionary<string, SettingCategoryEntry> LoadCategories()
    {
        var assembly = typeof(SettingsCatalogDefinitionRegistry).Assembly;
        const string resourceName = "Intune.Commander.Core.Assets.settings-catalog-categories.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return new Dictionary<string, SettingCategoryEntry>(StringComparer.OrdinalIgnoreCase);

        var entries = JsonSerializer.Deserialize<List<SettingCategoryEntry>>(stream, JsonOptions) ?? [];

        var dict = new Dictionary<string, SettingCategoryEntry>(entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Id))
                dict.TryAdd(entry.Id, entry);
        }
        return dict;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>
/// Represents a single setting definition from the Settings Catalog.
/// Matches the shape returned by GET /beta/deviceManagement/configurationSettings.
/// </summary>
public sealed class SettingDefinitionEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("helpText")]
    public string? HelpText { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("baseUri")]
    public string? BaseUri { get; set; }

    [JsonPropertyName("offsetUri")]
    public string? OffsetUri { get; set; }

    [JsonPropertyName("options")]
    public List<SettingDefinitionOption>? Options { get; set; }

    [JsonPropertyName("defaultOptionId")]
    public string? DefaultOptionId { get; set; }

    /// <summary>The OData type discriminator (e.g. #microsoft.graph.deviceManagementConfigurationChoiceSettingDefinition).</summary>
    [JsonPropertyName("@odata.type")]
    public string? OdataType { get; set; }
}

/// <summary>
/// Represents an allowed option for a choice-type setting definition.
/// </summary>
public sealed class SettingDefinitionOption
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Represents a category in the Settings Catalog category tree.
/// Matches the shape returned by GET /beta/deviceManagement/configurationCategories.
/// </summary>
public sealed class SettingCategoryEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("platforms")]
    public string? Platforms { get; set; }

    [JsonPropertyName("technologies")]
    public string? Technologies { get; set; }

    [JsonPropertyName("parentCategoryId")]
    public string? ParentCategoryId { get; set; }

    [JsonPropertyName("rootCategoryId")]
    public string? RootCategoryId { get; set; }

    [JsonPropertyName("childCategoryIds")]
    public List<string>? ChildCategoryIds { get; set; }
}
