using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class SimpleIntegerSettingViewModel : SettingViewModelBase
{
    [ObservableProperty]
    private int? _value;

    public long? Minimum { get; init; }

    public long? Maximum { get; init; }

    partial void OnValueChanged(int? value) => IsModified = true;

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        return WrapInstance(new DeviceManagementConfigurationSimpleSettingInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            SimpleSettingValue = new DeviceManagementConfigurationIntegerSettingValue
            {
                Value = Value
            }
        });
    }
}
