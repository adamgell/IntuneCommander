using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public abstract partial class SettingViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private string? _settingDefinitionId;

    [ObservableProperty]
    private string? _displayName;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isModified;

    public abstract DeviceManagementConfigurationSetting ToGraphSetting();

    protected DeviceManagementConfigurationSetting WrapInstance(
        DeviceManagementConfigurationSettingInstance instance)
    {
        return new DeviceManagementConfigurationSetting { SettingInstance = instance };
    }
}
