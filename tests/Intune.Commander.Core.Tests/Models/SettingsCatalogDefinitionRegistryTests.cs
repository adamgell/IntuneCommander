using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public class SettingsCatalogDefinitionRegistryTests
{
    // ── Embedded resource loading ──

    [Fact]
    public void Definitions_LoadsFromEmbeddedResource_DoesNotThrow()
    {
        // Should return an empty dictionary from the placeholder JSON, not throw
        var definitions = SettingsCatalogDefinitionRegistry.Definitions;
        Assert.NotNull(definitions);
    }

    [Fact]
    public void Categories_LoadsFromEmbeddedResource_DoesNotThrow()
    {
        var categories = SettingsCatalogDefinitionRegistry.Categories;
        Assert.NotNull(categories);
    }

    // ── ResolveDisplayName ──

    [Fact]
    public void ResolveDisplayName_NullInput_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName(null));
    }

    [Fact]
    public void ResolveDisplayName_EmptyInput_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName(""));
    }

    [Fact]
    public void ResolveDisplayName_UnknownId_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName("not_a_real_definition_id"));
    }

    // ── ResolveDescription ──

    [Fact]
    public void ResolveDescription_NullInput_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription(null));
    }

    [Fact]
    public void ResolveDescription_EmptyInput_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription(""));
    }

    [Fact]
    public void ResolveDescription_UnknownId_ReturnsNull()
    {
        Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription("not_a_real_definition_id"));
    }

    // ── ResolveCategoryName ──

    [Fact]
    public void ResolveCategoryName_NullInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SettingsCatalogDefinitionRegistry.ResolveCategoryName(null));
    }

    [Fact]
    public void ResolveCategoryName_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SettingsCatalogDefinitionRegistry.ResolveCategoryName(""));
    }

    [Fact]
    public void ResolveCategoryName_UnknownId_ReturnsOriginalId()
    {
        var unknownId = "unknown-category-id";
        Assert.Equal(unknownId, SettingsCatalogDefinitionRegistry.ResolveCategoryName(unknownId));
    }

    // ── HasDefinitions ──

    [Fact]
    public void HasDefinitions_WithPlaceholderJson_ReturnsFalse()
    {
        // The placeholder JSON is an empty array, so no definitions are loaded
        Assert.False(SettingsCatalogDefinitionRegistry.HasDefinitions);
    }

    // ── Model types ──

    [Fact]
    public void SettingDefinitionEntry_PropertiesAreSettable()
    {
        var entry = new SettingDefinitionEntry
        {
            Id = "test_id",
            Name = "TestSetting",
            DisplayName = "Test Setting",
            Description = "A test setting",
            HelpText = "Help text",
            CategoryId = "cat-1",
            BaseUri = "./Device/Vendor/MSFT",
            OffsetUri = "/Policy/Config",
            DefaultOptionId = "opt-1",
            OdataType = "#microsoft.graph.deviceManagementConfigurationChoiceSettingDefinition",
            Keywords = ["test", "setting"],
            Options =
            [
                new SettingDefinitionOption
                {
                    ItemId = "opt-1",
                    Name = "Enabled",
                    DisplayName = "Enabled",
                    Description = "Enable this setting"
                }
            ]
        };

        Assert.Equal("test_id", entry.Id);
        Assert.Equal("Test Setting", entry.DisplayName);
        Assert.Single(entry.Options!);
        Assert.Equal("Enabled", entry.Options![0].DisplayName);
    }

    [Fact]
    public void SettingCategoryEntry_PropertiesAreSettable()
    {
        var entry = new SettingCategoryEntry
        {
            Id = "cat-1",
            Name = "TestCategory",
            DisplayName = "Test Category",
            Description = "A test category",
            Platforms = "windows10",
            Technologies = "mdm",
            ParentCategoryId = "parent-1",
            RootCategoryId = "root-1",
            ChildCategoryIds = ["child-1", "child-2"]
        };

        Assert.Equal("cat-1", entry.Id);
        Assert.Equal("Test Category", entry.DisplayName);
        Assert.Equal(2, entry.ChildCategoryIds!.Count);
    }
}
