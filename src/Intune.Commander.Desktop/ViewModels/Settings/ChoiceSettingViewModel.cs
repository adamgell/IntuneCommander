using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public partial class ChoiceSettingViewModel : SettingViewModelBase
{
    [ObservableProperty]
    private ChoiceOption? _selectedOption;

    public ObservableCollection<ChoiceOption> Options { get; } = [];

    public ObservableCollection<SettingViewModelBase> Children { get; } = [];

    partial void OnSelectedOptionChanged(ChoiceOption? value) => IsModified = true;

    public override DeviceManagementConfigurationSetting ToGraphSetting()
    {
        var choiceValue = new DeviceManagementConfigurationChoiceSettingValue
        {
            Value = SelectedOption?.ItemId
        };

        if (Children.Count > 0)
        {
            choiceValue.Children = Children
                .Select(c => c.ToGraphSetting().SettingInstance!)
                .ToList();
        }

        return WrapInstance(new DeviceManagementConfigurationChoiceSettingInstance
        {
            SettingDefinitionId = SettingDefinitionId,
            ChoiceSettingValue = choiceValue
        });
    }
}
