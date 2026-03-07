using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.ViewModels;
using Intune.Commander.Desktop.ViewModels.Settings;
using Microsoft.Graph.Beta.Models;
using NSubstitute;

namespace Intune.Commander.Desktop.Tests.ViewModels;

public class SettingsPolicyEditorViewModelTests
{
    private readonly ISettingsCatalogService _scService = Substitute.For<ISettingsCatalogService>();

    private SettingsPolicyEditorViewModel CreateVm(string policyId = "p1")
    {
        var policy = new DeviceManagementConfigurationPolicy
        {
            Id = policyId,
            Name = "Test Policy",
            Description = "Test Description"
        };
        return new SettingsPolicyEditorViewModel(_scService, policy);
    }

    [Fact]
    public void Constructor_SetsPolicyNameAndDescription()
    {
        var vm = CreateVm();

        Assert.Equal("Test Policy", vm.PolicyName);
        Assert.Equal("Test Description", vm.PolicyDescription);
        Assert.Equal("p1", vm.PolicyId);
    }

    [Fact]
    public async Task LoadSettingsAsync_PopulatesCategoryTree()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<DeviceManagementConfigurationSetting>
            {
                new()
                {
                    SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
                    {
                        SettingDefinitionId = "test_setting",
                        ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                        {
                            Value = "option_1"
                        }
                    }
                }
            });

        var vm = CreateVm();
        await vm.LoadSettingsAsync();

        Assert.NotEmpty(vm.CategoryTree);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task LoadSettingsAsync_EmptyPolicyId_DoesNotFetch()
    {
        var policy = new DeviceManagementConfigurationPolicy { Id = null, Name = "No Id" };
        var vm = new SettingsPolicyEditorViewModel(_scService, policy);

        await vm.LoadSettingsAsync();

        await _scService.DidNotReceive()
            .GetPolicySettingsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadSettingsAsync_SetsErrorOnFailure()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns<List<DeviceManagementConfigurationSetting>>(
                x => throw new Exception("Graph error"));

        var vm = CreateVm();
        await vm.LoadSettingsAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Graph error", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveMetadataAsync_UpdatesPolicy()
    {
        var vm = CreateVm();
        vm.PolicyName = "Updated Name";
        vm.PolicyDescription = "Updated Desc";

        await vm.SaveMetadataCommand.ExecuteAsync(null);

        await _scService.Received(1).UpdateSettingsCatalogPolicyMetadataAsync(
            "p1",
            Arg.Is<DeviceManagementConfigurationPolicy>(p =>
                p.Name == "Updated Name" && p.Description == "Updated Desc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveSettingsAsync_PostsAllSettings()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<DeviceManagementConfigurationSetting>
            {
                new()
                {
                    SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
                    {
                        SettingDefinitionId = "s1",
                        SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
                        {
                            Value = "value1"
                        }
                    }
                }
            });

        var vm = CreateVm();
        await vm.LoadSettingsAsync();

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        await _scService.Received(1).UpdatePolicySettingsAsync(
            "p1",
            Arg.Any<List<DeviceManagementConfigurationSetting>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveSettingsAsync_ResetsHasUnsavedChanges()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<DeviceManagementConfigurationSetting>
            {
                new()
                {
                    SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
                    {
                        SettingDefinitionId = "s1",
                        SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
                        {
                            Value = "v"
                        }
                    }
                }
            });

        var vm = CreateVm();
        await vm.LoadSettingsAsync();

        // Simulate a modification
        var firstCategory = vm.CategoryTree.First();
        var settingVm = firstCategory.Settings.OfType<SimpleStringSettingViewModel>().First();
        settingVm.Value = "changed";
        Assert.True(vm.HasUnsavedChanges);

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        Assert.False(vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task DiscardChangesAsync_ReloadsFromGraph()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<DeviceManagementConfigurationSetting>());

        var vm = CreateVm();
        await vm.DiscardChangesCommand.ExecuteAsync(null);

        await _scService.Received(1)
            .GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UnsubscribeFromSettings_CanBeCalledSafely()
    {
        var vm = CreateVm();

        // Should not throw when no settings are loaded
        vm.UnsubscribeFromSettings();
    }

    [Fact]
    public async Task LoadSettingsAsync_IsModifiedFalseAfterLoad()
    {
        _scService.GetPolicySettingsAsync("p1", Arg.Any<CancellationToken>())
            .Returns(new List<DeviceManagementConfigurationSetting>
            {
                new()
                {
                    SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
                    {
                        SettingDefinitionId = "choice1",
                        ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                        {
                            Value = "opt_a"
                        }
                    }
                }
            });

        var vm = CreateVm();
        await vm.LoadSettingsAsync();

        // No settings should be marked as modified after initial load
        var allSettings = vm.CategoryTree.SelectMany(c => c.Settings);
        Assert.All(allSettings, s => Assert.False(s.IsModified));
        Assert.False(vm.HasUnsavedChanges);
    }
}
