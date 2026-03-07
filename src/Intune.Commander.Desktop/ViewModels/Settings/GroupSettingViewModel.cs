using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class GroupSettingViewModel : SettingViewModelBase
{
    public ObservableCollection<SettingViewModelBase> Children { get; } = [];

    public bool IsCollection { get; init; }

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        var childInstances = Children
            .Select(c => c.ToGraphSetting().SettingInstance
                ?? throw new InvalidOperationException(
                    $"Child setting '{c.SettingDefinitionId}' produced null SettingInstance"))
            .ToList();

        if (IsCollection)
        {
            return WrapInstance(new DeviceManagementConfigurationGroupSettingCollectionInstance
            {
                SettingDefinitionId = SettingDefinitionId,
                GroupSettingCollectionValue =
                [
                    new DeviceManagementConfigurationGroupSettingValue { Children = childInstances }
                ]
            });
        }

        return WrapInstance(new DeviceManagementConfigurationGroupSettingInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            GroupSettingValue = new DeviceManagementConfigurationGroupSettingValue
            {
                Children = childInstances
            }
        });
    }
}
