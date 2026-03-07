using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class SimpleStringSettingViewModel : SettingViewModelBase
{
    [ObservableProperty]
    private string? _value;

    partial void OnValueChanged(string? value) => IsModified = true;

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        return WrapInstance(new DeviceManagementConfigurationSimpleSettingInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
            {
                Value = Value
            }
        });
    }
}
