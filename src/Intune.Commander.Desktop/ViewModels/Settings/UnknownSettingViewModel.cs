using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class UnknownSettingViewModel : SettingViewModelBase
{
    private readonly DeviceManagementConfigurationSetting _original;

    public UnknownSettingViewModel(DeviceManagementConfigurationSetting original)
    {
        _original = original;
    }

    public override DeviceManagementConfigurationSetting ToGraphSetting() => _original;
}
