using System.Linq;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels.Settings;

public static class SettingViewModelFactory
{
    public static SettingViewModelBase Create(DeviceManagementConfigurationSetting setting)
    {
        if (setting.SettingInstance is null)
            return new UnknownSettingViewModel(setting);

        return CreateFromInstance(setting.SettingInstance);
    }

    private static SettingViewModelBase CreateFromInstance(
        DeviceManagementConfigurationSettingInstance instance)
    {
        var defId = instance.SettingDefinitionId;
        var displayName = SettingsCatalogDefinitionRegistry.ResolveDisplayName(defId) ?? defId;
        var description = SettingsCatalogDefinitionRegistry.ResolveDescription(defId);

        SettingViewModelBase vm = instance switch
        {
            DeviceManagementConfigurationChoiceSettingInstance choice =>
                BuildChoiceVm(choice, defId),

            DeviceManagementConfigurationChoiceSettingCollectionInstance choiceCollection =>
                BuildChoiceCollectionVm(choiceCollection, defId),

            DeviceManagementConfigurationSimpleSettingInstance simple =>
                BuildSimpleVm(simple),

            DeviceManagementConfigurationSimpleSettingCollectionInstance simpleCollection =>
                BuildSimpleCollectionVm(simpleCollection),

            DeviceManagementConfigurationGroupSettingInstance group =>
                BuildGroupVm(group, isCollection: false),

            DeviceManagementConfigurationGroupSettingCollectionInstance groupCollection =>
                BuildGroupCollectionVm(groupCollection),

            _ => new UnknownSettingViewModel(new DeviceManagementConfigurationSetting
            {
                SettingInstance = instance
            })
        };

        vm.SettingDefinitionId = defId;
        vm.DisplayName = displayName;
        vm.Description = description;
        vm.IsModified = false;

        return vm;
    }

    private static ChoiceSettingViewModel BuildChoiceVm(
        DeviceManagementConfigurationChoiceSettingInstance choice,
        string? defId)
    {
        var vm = new ChoiceSettingViewModel();
        PopulateChoiceOptions(vm.Options, defId);

        var selectedId = choice.ChoiceSettingValue?.Value;
        vm.SelectedOption = vm.Options.FirstOrDefault(o => o.ItemId == selectedId);
        if (vm.SelectedOption is null && selectedId is not null)
        {
            var fallback = new ChoiceOption(selectedId, selectedId, null);
            vm.Options.Add(fallback);
            vm.SelectedOption = fallback;
        }

        if (choice.ChoiceSettingValue?.Children is { } children)
        {
            foreach (var child in children)
                vm.Children.Add(CreateFromInstance(child));
        }

        return vm;
    }

    private static ChoiceCollectionSettingViewModel BuildChoiceCollectionVm(
        DeviceManagementConfigurationChoiceSettingCollectionInstance choiceCollection,
        string? defId)
    {
        var vm = new ChoiceCollectionSettingViewModel();
        PopulateChoiceOptions(vm.AvailableOptions, defId);

        if (choiceCollection.ChoiceSettingCollectionValue is { } values)
        {
            foreach (var val in values)
            {
                var match = vm.AvailableOptions.FirstOrDefault(o => o.ItemId == val.Value);
                if (match is not null)
                    vm.SelectedOptions.Add(match);
                else if (val.Value is not null)
                    vm.SelectedOptions.Add(new ChoiceOption(val.Value, val.Value, null));
            }
        }

        return vm;
    }

    private static SettingViewModelBase BuildSimpleVm(
        DeviceManagementConfigurationSimpleSettingInstance simple)
    {
        return simple.SimpleSettingValue switch
        {
            DeviceManagementConfigurationIntegerSettingValue intVal =>
                new SimpleIntegerSettingViewModel { Value = (int?)intVal.Value },

            DeviceManagementConfigurationStringSettingValue strVal =>
                new SimpleStringSettingViewModel { Value = strVal.Value },

            _ => new UnknownSettingViewModel(new DeviceManagementConfigurationSetting
            {
                SettingInstance = simple
            })
        };
    }

    private static SimpleCollectionSettingViewModel BuildSimpleCollectionVm(
        DeviceManagementConfigurationSimpleSettingCollectionInstance simpleCollection)
    {
        var vm = new SimpleCollectionSettingViewModel();

        if (simpleCollection.SimpleSettingCollectionValue is { } values)
        {
            foreach (var val in values)
            {
                if (val is DeviceManagementConfigurationStringSettingValue strVal)
                    vm.Values.Add(strVal.Value ?? string.Empty);
            }
        }

        return vm;
    }

    private static GroupSettingViewModel BuildGroupVm(
        DeviceManagementConfigurationGroupSettingInstance group,
        bool isCollection)
    {
        var vm = new GroupSettingViewModel { IsCollection = isCollection };

        if (group.GroupSettingValue?.Children is { } children)
        {
            foreach (var child in children)
                vm.Children.Add(CreateFromInstance(child));
        }

        return vm;
    }

    private static SettingViewModelBase BuildGroupCollectionVm(
        DeviceManagementConfigurationGroupSettingCollectionInstance groupCollection)
    {
        // Multi-value group collections can't be round-tripped safely; fall back to read-only
        if (groupCollection.GroupSettingCollectionValue is { Count: > 1 })
        {
            return new UnknownSettingViewModel(new DeviceManagementConfigurationSetting
            {
                SettingInstance = groupCollection
            });
        }

        var vm = new GroupSettingViewModel { IsCollection = true };

        if (groupCollection.GroupSettingCollectionValue is { Count: > 0 } values)
        {
            var first = values[0];
            if (first.Children is { } children)
            {
                foreach (var child in children)
                    vm.Children.Add(CreateFromInstance(child));
            }
        }

        return vm;
    }

    private static void PopulateChoiceOptions(
        System.Collections.ObjectModel.ObservableCollection<ChoiceOption> target,
        string? defId)
    {
        if (string.IsNullOrEmpty(defId)) return;

        if (SettingsCatalogDefinitionRegistry.Definitions.TryGetValue(defId, out var def)
            && def.Options is { } options)
        {
            foreach (var opt in options)
            {
                if (opt.ItemId is not null)
                    target.Add(new ChoiceOption(opt.ItemId, opt.DisplayName, opt.Description));
            }
        }
    }
}
