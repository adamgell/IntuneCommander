using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class ChoiceCollectionSettingViewModel : SettingViewModelBase
{
    public ObservableCollection<ChoiceOption> SelectedOptions { get; } = [];

    public ObservableCollection<ChoiceOption> AvailableOptions { get; } = [];

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        var values = SelectedOptions.Select(opt =>
            new DeviceManagementConfigurationChoiceSettingValue { Value = opt.ItemId }).ToList();

        return WrapInstance(new DeviceManagementConfigurationChoiceSettingCollectionInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            ChoiceSettingCollectionValue = values
        });
    }
}
