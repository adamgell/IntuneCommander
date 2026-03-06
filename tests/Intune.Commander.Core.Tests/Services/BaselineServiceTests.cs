using System.Text.Json;
using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class BaselineServiceTests
{
    [Fact]
    public void GetAllBaselines_WithPlaceholderAssets_ReturnsEmptyList()
    {
        var sut = new BaselineService();
        var result = sut.GetAllBaselines();
        Assert.Empty(result);
    }

    [Fact]
    public void GetBaselinesByType_ReturnsFilteredList()
    {
        var baselines = CreateTestBaselines();
        var sut = new BaselineService(baselines);

        var sc = sut.GetBaselinesByType(BaselinePolicyType.SettingsCatalog);
        var es = sut.GetBaselinesByType(BaselinePolicyType.EndpointSecurity);

        Assert.Equal(2, sc.Count);
        Assert.Single(es);
        Assert.All(sc, b => Assert.Equal(BaselinePolicyType.SettingsCatalog, b.PolicyType));
    }

    [Fact]
    public void GetCategories_ReturnsDistinctSortedCategories()
    {
        var baselines = CreateTestBaselines();
        var sut = new BaselineService(baselines);

        var categories = sut.GetCategories();

        Assert.Equal(2, categories.Count);
        Assert.Equal("Antivirus", categories[0]);
        Assert.Equal("Browser", categories[1]);
    }

    [Fact]
    public void GetCategories_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        var baselines = CreateTestBaselines();
        var sut = new BaselineService(baselines);

        var categories = sut.GetCategories(BaselinePolicyType.EndpointSecurity);

        Assert.Single(categories);
        Assert.Equal("Antivirus", categories[0]);
    }

    [Fact]
    public void GetBaselinesByCategory_FiltersCorrectly()
    {
        var baselines = CreateTestBaselines();
        var sut = new BaselineService(baselines);

        var browser = sut.GetBaselinesByCategory("Browser");

        Assert.Equal(2, browser.Count);
        Assert.All(browser, b => Assert.Equal("Browser", b.Category));
    }

    [Fact]
    public void GetBaselinesByCategory_WithType_FiltersCorrectly()
    {
        var baselines = CreateTestBaselines();
        var sut = new BaselineService(baselines);

        var result = sut.GetBaselinesByCategory("Browser", BaselinePolicyType.SettingsCatalog);

        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData("Win - OIB - Settings Catalog - Browser - D - Edge.json", "Browser")]
    [InlineData("Win - OIB - Endpoint Security - Antivirus - U - Defender.json", "Antivirus")]
    [InlineData("Win - OIB - Compliance Policies - Device Health - D - TPM.json", "Device Health")]
    [InlineData("SomeOtherFile.json", "General")]
    [InlineData("A - B - C.json", "General")]
    public void ParseCategory_ExtractsCorrectly(string fileName, string expected)
    {
        Assert.Equal(expected, BaselineService.ParseCategory(fileName));
    }

    [Fact]
    public void CompareSettingsCatalog_AllMatching_ReturnsNoMissingOrDrifted()
    {
        var sut = new BaselineService();
        var baseline = CreateBaselineWithSettings(
            ("test_setting_1", "option_a"),
            ("test_setting_2", "option_b"));

        var tenantSettings = new List<DeviceManagementConfigurationSetting>
        {
            CreateChoiceSetting("test_setting_1", "option_a"),
            CreateChoiceSetting("test_setting_2", "option_b")
        };

        var result = sut.CompareSettingsCatalog(baseline, tenantSettings, "policy1", "Test Policy");

        Assert.Equal(2, result.Matching.Count);
        Assert.Empty(result.Missing);
        Assert.Empty(result.Drifted);
        Assert.Empty(result.Extra);
        Assert.Equal("Test Baseline", result.BaselineName);
        Assert.Equal("policy1", result.TenantPolicyId);
        Assert.Equal("Test Policy", result.TenantPolicyName);
    }

    [Fact]
    public void CompareSettingsCatalog_MissingSetting_ClassifiedAsMissing()
    {
        var sut = new BaselineService();
        var baseline = CreateBaselineWithSettings(
            ("test_setting_1", "option_a"),
            ("test_setting_2", "option_b"));

        var tenantSettings = new List<DeviceManagementConfigurationSetting>
        {
            CreateChoiceSetting("test_setting_1", "option_a")
        };

        var result = sut.CompareSettingsCatalog(baseline, tenantSettings);

        Assert.Single(result.Matching);
        Assert.Single(result.Missing);
        Assert.Equal("test_setting_2", result.Missing[0].SettingDefinitionId);
        Assert.Equal("option_b", result.Missing[0].BaselineValue);
        Assert.Null(result.Missing[0].TenantValue);
    }

    [Fact]
    public void CompareSettingsCatalog_DriftedValue_ClassifiedAsDrifted()
    {
        var sut = new BaselineService();
        var baseline = CreateBaselineWithSettings(("test_setting_1", "option_a"));

        var tenantSettings = new List<DeviceManagementConfigurationSetting>
        {
            CreateChoiceSetting("test_setting_1", "option_b")
        };

        var result = sut.CompareSettingsCatalog(baseline, tenantSettings);

        Assert.Empty(result.Matching);
        Assert.Single(result.Drifted);
        Assert.Equal("test_setting_1", result.Drifted[0].SettingDefinitionId);
        Assert.Equal("option_a", result.Drifted[0].BaselineValue);
        Assert.Equal("option_b", result.Drifted[0].TenantValue);
    }

    [Fact]
    public void CompareSettingsCatalog_ExtraSetting_ClassifiedAsExtra()
    {
        var sut = new BaselineService();
        var baseline = CreateBaselineWithSettings(("test_setting_1", "option_a"));

        var tenantSettings = new List<DeviceManagementConfigurationSetting>
        {
            CreateChoiceSetting("test_setting_1", "option_a"),
            CreateChoiceSetting("test_setting_extra", "extra_val")
        };

        var result = sut.CompareSettingsCatalog(baseline, tenantSettings);

        Assert.Single(result.Matching);
        Assert.Empty(result.Missing);
        Assert.Empty(result.Drifted);
        Assert.Single(result.Extra);
        Assert.Equal("test_setting_extra", result.Extra[0].SettingDefinitionId);
    }

    [Fact]
    public void CompareSettingsCatalog_EmptyBaseline_AllTenantSettingsAreExtra()
    {
        var sut = new BaselineService();
        var baseline = new BaselinePolicy
        {
            Name = "Empty",
            RawJson = JsonDocument.Parse("""{"settings":[]}""").RootElement
        };

        var tenantSettings = new List<DeviceManagementConfigurationSetting>
        {
            CreateChoiceSetting("setting_1", "val")
        };

        var result = sut.CompareSettingsCatalog(baseline, tenantSettings);

        Assert.Empty(result.Matching);
        Assert.Empty(result.Missing);
        Assert.Empty(result.Drifted);
        Assert.Single(result.Extra);
    }

    private static List<BaselinePolicy> CreateTestBaselines()
    {
        var emptyJson = JsonDocument.Parse("{}").RootElement;
        return
        [
            new BaselinePolicy
            {
                PolicyType = BaselinePolicyType.SettingsCatalog,
                Name = "Edge",
                Category = "Browser",
                FileName = "Win - OIB - Settings Catalog - Browser - D - Edge.json",
                RawJson = emptyJson
            },
            new BaselinePolicy
            {
                PolicyType = BaselinePolicyType.SettingsCatalog,
                Name = "Chrome",
                Category = "Browser",
                FileName = "Win - OIB - Settings Catalog - Browser - D - Chrome.json",
                RawJson = emptyJson
            },
            new BaselinePolicy
            {
                PolicyType = BaselinePolicyType.EndpointSecurity,
                Name = "Defender",
                Category = "Antivirus",
                FileName = "Win - OIB - Endpoint Security - Antivirus - U - Defender.json",
                RawJson = emptyJson
            }
        ];
    }

    private static BaselinePolicy CreateBaselineWithSettings(params (string defId, string value)[] settings)
    {
        var settingsArray = settings.Select(s => $$"""
        {
            "settingInstance": {
                "@odata.type": "#microsoft.graph.deviceManagementConfigurationChoiceSettingInstance",
                "settingDefinitionId": "{{s.defId}}",
                "choiceSettingValue": {
                    "value": "{{s.value}}",
                    "children": []
                }
            }
        }
        """);

        var json = $$"""{"settings": [{{string.Join(",", settingsArray)}}]}""";
        return new BaselinePolicy
        {
            Name = "Test Baseline",
            PolicyType = BaselinePolicyType.SettingsCatalog,
            FileName = "Win - OIB - Settings Catalog - Test - D - Test.json",
            Category = "Test",
            RawJson = JsonDocument.Parse(json).RootElement
        };
    }

    private static DeviceManagementConfigurationSetting CreateChoiceSetting(string definitionId, string value)
    {
        return new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
            {
                SettingDefinitionId = definitionId,
                ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                {
                    Value = value
                }
            }
        };
    }
}
