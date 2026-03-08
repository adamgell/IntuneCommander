using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public class SettingsCatalogDefinitionRegistryTests
{
    [Fact]
    public void Definitions_LoadsFromEmbeddedResource_DoesNotThrow()
    {
        var definitions = SettingsCatalogDefinitionRegistry.Definitions;
        Assert.NotNull(definitions);
    }

    [Fact]
    public void Definitions_EmbeddedSnapshotCount_ExceedsSanityThreshold()
    {
        var definitions = SettingsCatalogDefinitionRegistry.Definitions;
        Assert.True(
            definitions.Count >= 100,
            $"Expected at least 100 embedded setting definitions, but found {definitions.Count}. The embedded snapshot may be truncated.");
    }

    [Fact]
    public void Categories_LoadsFromEmbeddedResource_DoesNotThrow()
    {
        var categories = SettingsCatalogDefinitionRegistry.Categories;
        Assert.NotNull(categories);
    }

    [Fact]
    public void ResolveDisplayName_NullInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName(null));

    [Fact]
    public void ResolveDisplayName_EmptyInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName(""));

    [Fact]
    public void ResolveDisplayName_UnknownId_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDisplayName("not_a_real_definition_id"));

    [Fact]
    public void ResolveDescription_NullInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription(null));

    [Fact]
    public void ResolveDescription_EmptyInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription(""));

    [Fact]
    public void ResolveDescription_UnknownId_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDescription("not_a_real_definition_id"));

    [Fact]
    public void ResolveHelpText_NullInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveHelpText(null));

    [Fact]
    public void ResolveHelpText_EmptyInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveHelpText(""));

    [Fact]
    public void ResolveHelpText_UnknownId_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveHelpText("not_a_real_definition_id"));

    [Fact]
    public void ResolveDefinition_NullInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDefinition(null));

    [Fact]
    public void ResolveDefinition_EmptyInput_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDefinition(""));

    [Fact]
    public void ResolveDefinition_UnknownId_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveDefinition("not_a_real_definition_id"));

    [Fact]
    public void ResolveOptionDisplayName_NullDefinition_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveOptionDisplayName(null, "opt-1"));

    [Fact]
    public void ResolveOptionDisplayName_NullOption_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveOptionDisplayName("def-1", null));

    [Fact]
    public void ResolveOptionDisplayName_UnknownDefinition_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveOptionDisplayName("missing", "opt-1"));

    [Fact]
    public void ResolveOptionDescription_UnknownDefinition_ReturnsNull()
        => Assert.Null(SettingsCatalogDefinitionRegistry.ResolveOptionDescription("missing", "opt-1"));

    [Fact]
    public void ResolveCategoryName_NullInput_ReturnsEmpty()
        => Assert.Equal(string.Empty, SettingsCatalogDefinitionRegistry.ResolveCategoryName(null));

    [Fact]
    public void ResolveCategoryName_EmptyInput_ReturnsEmpty()
        => Assert.Equal(string.Empty, SettingsCatalogDefinitionRegistry.ResolveCategoryName(""));

    [Fact]
    public void ResolveCategoryName_UnknownId_ReturnsOriginalId()
    {
        var unknownId = "unknown-category-id";
        Assert.Equal(unknownId, SettingsCatalogDefinitionRegistry.ResolveCategoryName(unknownId));
    }

    [Fact]
    public void HasDefinitions_PropertyAccessible_DoesNotThrow()
    {
        var _ = SettingsCatalogDefinitionRegistry.HasDefinitions;
    }

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
