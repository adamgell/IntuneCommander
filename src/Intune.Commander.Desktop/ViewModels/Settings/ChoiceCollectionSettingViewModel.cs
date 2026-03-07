using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class ChoiceCollectionSettingViewModel : SettingViewModelBase
{
    public ObservableCollection<ChoiceOption> SelectedOptions { get; } = [];

    public ObservableCollection<ChoiceOption> AvailableOptions { get; } = [];

    public ChoiceCollectionSettingViewModel()
    {
        SelectedOptions.CollectionChanged += OnSelectedOptionsChanged;
    }

    private void OnSelectedOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => IsModified = true;

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
