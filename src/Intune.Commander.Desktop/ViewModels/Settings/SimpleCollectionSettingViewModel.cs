using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class SimpleCollectionSettingViewModel : SettingViewModelBase
{
    public ObservableCollection<string> Values { get; } = [];

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        var collectionValues = Values.Select(v =>
            (DeviceManagementConfigurationSimpleSettingValue)
            new DeviceManagementConfigurationStringSettingValue { Value = v }).ToList();

        return WrapInstance(new DeviceManagementConfigurationSimpleSettingCollectionInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            SimpleSettingCollectionValue = collectionValues
        });
    }
}
