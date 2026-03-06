using Intune.Commander.Desktop.ViewModels.Settings;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.ViewModels.Settings;

public class SettingViewModelFactoryTests
{
    [Fact]
    public void Create_ChoiceSettingInstance_ReturnsChoiceSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
            {
                SettingDefinitionId = "test_choice",
                ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                {
                    Value = "option_1"
                }
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<ChoiceSettingViewModel>(result);
        Assert.Equal("test_choice", vm.SettingDefinitionId);
    }

    [Fact]
    public void Create_ChoiceSettingCollectionInstance_ReturnsChoiceCollectionSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingCollectionInstance
            {
                SettingDefinitionId = "test_choice_collection",
                ChoiceSettingCollectionValue =
                [
                    new DeviceManagementConfigurationChoiceSettingValue { Value = "opt_a" },
                    new DeviceManagementConfigurationChoiceSettingValue { Value = "opt_b" }
                ]
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<ChoiceCollectionSettingViewModel>(result);
        Assert.Equal("test_choice_collection", vm.SettingDefinitionId);
        Assert.Equal(2, vm.SelectedOptions.Count);
    }

    [Fact]
    public void Create_SimpleStringSettingInstance_ReturnsSimpleStringSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
            {
                SettingDefinitionId = "test_string",
                SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
                {
                    Value = "hello"
                }
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<SimpleStringSettingViewModel>(result);
        Assert.Equal("hello", vm.Value);
        Assert.Equal("test_string", vm.SettingDefinitionId);
    }

    [Fact]
    public void Create_SimpleIntegerSettingInstance_ReturnsSimpleIntegerSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
            {
                SettingDefinitionId = "test_int",
                SimpleSettingValue = new DeviceManagementConfigurationIntegerSettingValue
                {
                    Value = 42
                }
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<SimpleIntegerSettingViewModel>(result);
        Assert.Equal(42, vm.Value);
    }

    [Fact]
    public void Create_SimpleSettingCollectionInstance_ReturnsSimpleCollectionSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationSimpleSettingCollectionInstance
            {
                SettingDefinitionId = "test_collection",
                SimpleSettingCollectionValue =
                [
                    new DeviceManagementConfigurationStringSettingValue { Value = "a" },
                    new DeviceManagementConfigurationStringSettingValue { Value = "b" }
                ]
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<SimpleCollectionSettingViewModel>(result);
        Assert.Equal(["a", "b"], vm.Values);
    }

    [Fact]
    public void Create_GroupSettingInstance_ReturnsGroupSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationGroupSettingInstance
            {
                SettingDefinitionId = "test_group",
                GroupSettingValue = new DeviceManagementConfigurationGroupSettingValue
                {
                    Children =
                    [
                        new DeviceManagementConfigurationSimpleSettingInstance
                        {
                            SettingDefinitionId = "child_string",
                            SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
                            {
                                Value = "nested"
                            }
                        }
                    ]
                }
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<GroupSettingViewModel>(result);
        Assert.False(vm.IsCollection);
        Assert.Single(vm.Children);
        var child = Assert.IsType<SimpleStringSettingViewModel>(vm.Children[0]);
        Assert.Equal("nested", child.Value);
    }

    [Fact]
    public void Create_GroupSettingCollectionInstance_ReturnsGroupSettingViewModelWithIsCollection()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationGroupSettingCollectionInstance
            {
                SettingDefinitionId = "test_group_coll",
                GroupSettingCollectionValue =
                [
                    new DeviceManagementConfigurationGroupSettingValue
                    {
                        Children =
                        [
                            new DeviceManagementConfigurationSimpleSettingInstance
                            {
                                SettingDefinitionId = "child_int",
                                SimpleSettingValue =
                                    new DeviceManagementConfigurationIntegerSettingValue { Value = 7 }
                            }
                        ]
                    }
                ]
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<GroupSettingViewModel>(result);
        Assert.True(vm.IsCollection);
        Assert.Single(vm.Children);
    }

    [Fact]
    public void Create_NullSettingInstance_ReturnsUnknownSettingViewModel()
    {
        var setting = new DeviceManagementConfigurationSetting { SettingInstance = null };

        var result = SettingViewModelFactory.Create(setting);

        Assert.IsType<UnknownSettingViewModel>(result);
    }

    [Fact]
    public void Create_ChoiceWithChildren_RecursesChildren()
    {
        var setting = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
            {
                SettingDefinitionId = "parent_choice",
                ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                {
                    Value = "enabled",
                    Children =
                    [
                        new DeviceManagementConfigurationSimpleSettingInstance
                        {
                            SettingDefinitionId = "child_setting",
                            SimpleSettingValue =
                                new DeviceManagementConfigurationStringSettingValue { Value = "child_val" }
                        },
                        new DeviceManagementConfigurationChoiceSettingInstance
                        {
                            SettingDefinitionId = "nested_choice",
                            ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                            {
                                Value = "sub_option"
                            }
                        }
                    ]
                }
            }
        };

        var result = SettingViewModelFactory.Create(setting);

        var vm = Assert.IsType<ChoiceSettingViewModel>(result);
        Assert.Equal(2, vm.Children.Count);
        Assert.IsType<SimpleStringSettingViewModel>(vm.Children[0]);
        Assert.IsType<ChoiceSettingViewModel>(vm.Children[1]);
    }

    [Fact]
    public void ToGraphSetting_Roundtrip_ChoiceSetting()
    {
        var original = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
            {
                SettingDefinitionId = "roundtrip_choice",
                ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                {
                    Value = "option_x"
                }
            }
        };

        var vm = SettingViewModelFactory.Create(original);
        var roundtripped = vm.ToGraphSetting();

        var instance = Assert.IsType<DeviceManagementConfigurationChoiceSettingInstance>(
            roundtripped.SettingInstance);
        Assert.Equal("roundtrip_choice", instance.SettingDefinitionId);
        Assert.Equal("option_x", instance.ChoiceSettingValue?.Value);
    }

    [Fact]
    public void ToGraphSetting_Roundtrip_SimpleStringSetting()
    {
        var original = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
            {
                SettingDefinitionId = "roundtrip_string",
                SimpleSettingValue = new DeviceManagementConfigurationStringSettingValue
                {
                    Value = "test_value"
                }
            }
        };

        var vm = SettingViewModelFactory.Create(original);
        var roundtripped = vm.ToGraphSetting();

        var instance = Assert.IsType<DeviceManagementConfigurationSimpleSettingInstance>(
            roundtripped.SettingInstance);
        Assert.Equal("roundtrip_string", instance.SettingDefinitionId);
        var strVal = Assert.IsType<DeviceManagementConfigurationStringSettingValue>(
            instance.SimpleSettingValue);
        Assert.Equal("test_value", strVal.Value);
    }

    [Fact]
    public void ToGraphSetting_Roundtrip_SimpleIntegerSetting()
    {
        var original = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationSimpleSettingInstance
            {
                SettingDefinitionId = "roundtrip_int",
                SimpleSettingValue = new DeviceManagementConfigurationIntegerSettingValue
                {
                    Value = 99
                }
            }
        };

        var vm = SettingViewModelFactory.Create(original);
        var roundtripped = vm.ToGraphSetting();

        var instance = Assert.IsType<DeviceManagementConfigurationSimpleSettingInstance>(
            roundtripped.SettingInstance);
        var intVal = Assert.IsType<DeviceManagementConfigurationIntegerSettingValue>(
            instance.SimpleSettingValue);
        Assert.Equal(99, intVal.Value);
    }

    [Fact]
    public void ToGraphSetting_Roundtrip_GroupWithNestedChildren()
    {
        var original = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationGroupSettingInstance
            {
                SettingDefinitionId = "roundtrip_group",
                GroupSettingValue = new DeviceManagementConfigurationGroupSettingValue
                {
                    Children =
                    [
                        new DeviceManagementConfigurationSimpleSettingInstance
                        {
                            SettingDefinitionId = "child_1",
                            SimpleSettingValue =
                                new DeviceManagementConfigurationStringSettingValue { Value = "v1" }
                        }
                    ]
                }
            }
        };

        var vm = SettingViewModelFactory.Create(original);
        var roundtripped = vm.ToGraphSetting();

        var instance = Assert.IsType<DeviceManagementConfigurationGroupSettingInstance>(
            roundtripped.SettingInstance);
        Assert.Equal("roundtrip_group", instance.SettingDefinitionId);
        Assert.NotNull(instance.GroupSettingValue?.Children);
        Assert.Single(instance.GroupSettingValue!.Children!);
        var childInstance = Assert.IsType<DeviceManagementConfigurationSimpleSettingInstance>(
            instance.GroupSettingValue.Children![0]);
        Assert.Equal("child_1", childInstance.SettingDefinitionId);
    }

    [Fact]
    public void ToGraphSetting_Roundtrip_ChoiceWithChildren()
    {
        var original = new DeviceManagementConfigurationSetting
        {
            SettingInstance = new DeviceManagementConfigurationChoiceSettingInstance
            {
                SettingDefinitionId = "parent",
                ChoiceSettingValue = new DeviceManagementConfigurationChoiceSettingValue
                {
                    Value = "enabled",
                    Children =
                    [
                        new DeviceManagementConfigurationSimpleSettingInstance
                        {
                            SettingDefinitionId = "sub",
                            SimpleSettingValue =
                                new DeviceManagementConfigurationStringSettingValue { Value = "inner" }
                        }
                    ]
                }
            }
        };

        var vm = SettingViewModelFactory.Create(original);
        var roundtripped = vm.ToGraphSetting();

        var instance = Assert.IsType<DeviceManagementConfigurationChoiceSettingInstance>(
            roundtripped.SettingInstance);
        Assert.Equal("enabled", instance.ChoiceSettingValue?.Value);
        Assert.NotNull(instance.ChoiceSettingValue?.Children);
        Assert.Single(instance.ChoiceSettingValue!.Children!);
        var child = Assert.IsType<DeviceManagementConfigurationSimpleSettingInstance>(
            instance.ChoiceSettingValue.Children![0]);
        Assert.Equal("sub", child.SettingDefinitionId);
    }
}
