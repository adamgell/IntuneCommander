using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.Models;
using Intune.Commander.Desktop.ViewModels;
using Microsoft.Graph.Beta.Models;
using NSubstitute;

namespace Intune.Commander.Desktop.Tests.ViewModels;

public class GroupPickerViewModelTests
{
    private readonly IGroupService _groupService = Substitute.For<IGroupService>();

    [Fact]
    public async Task SearchGroupsCommand_PopulatesGroups()
    {
        _groupService.SearchGroupsAsync("test", Arg.Any<CancellationToken>())
            .Returns(new List<Group>
            {
                new() { Id = "g1", DisplayName = "Test Group 1" },
                new() { Id = "g2", DisplayName = "Test Group 2" }
            });

        var vm = new GroupPickerViewModel(_groupService) { SearchText = "test" };

        await vm.SearchGroupsCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Groups.Count);
        Assert.Equal("g1", vm.Groups[0].GroupId);
        Assert.Equal("Test Group 1", vm.Groups[0].DisplayName);
    }

    [Fact]
    public async Task SearchGroupsCommand_SkipsGroupsWithNullId()
    {
        _groupService.SearchGroupsAsync("test", Arg.Any<CancellationToken>())
            .Returns(new List<Group>
            {
                new() { Id = null, DisplayName = "No Id Group" },
                new() { Id = "g1", DisplayName = "Valid Group" }
            });

        var vm = new GroupPickerViewModel(_groupService) { SearchText = "test" };

        await vm.SearchGroupsCommand.ExecuteAsync(null);

        Assert.Single(vm.Groups);
        Assert.Equal("g1", vm.Groups[0].GroupId);
    }

    [Fact]
    public async Task SearchGroupsCommand_EmptySearchText_DoesNotSearch()
    {
        var vm = new GroupPickerViewModel(_groupService) { SearchText = "" };

        await vm.SearchGroupsCommand.ExecuteAsync(null);

        await _groupService.DidNotReceive()
            .SearchGroupsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddGroupCommand_AddsToSelectedGroups()
    {
        var vm = new GroupPickerViewModel(_groupService);
        var item = new GroupSelectionItem("g1", "Group 1", "Security");

        vm.AddGroupCommand.Execute(item);

        Assert.Single(vm.SelectedGroups);
        Assert.Equal("g1", vm.SelectedGroups[0].GroupId);
    }

    [Fact]
    public void AddGroupCommand_PreventsDuplicates()
    {
        var vm = new GroupPickerViewModel(_groupService);
        var item = new GroupSelectionItem("g1", "Group 1", "Security");

        vm.AddGroupCommand.Execute(item);
        vm.AddGroupCommand.Execute(item);

        Assert.Single(vm.SelectedGroups);
    }

    [Fact]
    public void RemoveGroupCommand_RemovesFromSelectedGroups()
    {
        var vm = new GroupPickerViewModel(_groupService);
        var item = new GroupSelectionItem("g1", "Group 1", "Security");
        vm.SelectedGroups.Add(item);

        vm.RemoveGroupCommand.Execute(item);

        Assert.Empty(vm.SelectedGroups);
    }

    [Fact]
    public void BuildAssignments_IncludesAllDevices()
    {
        var vm = new GroupPickerViewModel(_groupService);
        vm.IncludeAllDevices = true;

        var result = vm.BuildAssignments<DeviceManagementConfigurationPolicyAssignment>();

        Assert.Single(result);
        Assert.IsType<AllDevicesAssignmentTarget>(result[0].Target);
    }

    [Fact]
    public void BuildAssignments_IncludesGroupTargets()
    {
        var vm = new GroupPickerViewModel(_groupService);
        vm.SelectedGroups.Add(new GroupSelectionItem("g1", "Group 1", "Security"));

        var result = vm.BuildAssignments<DeviceManagementConfigurationPolicyAssignment>();

        Assert.Single(result);
        var target = Assert.IsType<GroupAssignmentTarget>(result[0].Target);
        Assert.Equal("g1", target.GroupId);
    }

    [Fact]
    public void BuildAssignments_ExclusionGroup_UsesExclusionTarget()
    {
        var vm = new GroupPickerViewModel(_groupService);
        var item = new GroupSelectionItem("g1", "Excluded", "Security") { IsExclusion = true };
        vm.SelectedGroups.Add(item);

        var result = vm.BuildAssignments<DeviceManagementConfigurationPolicyAssignment>();

        Assert.Single(result);
        var target = Assert.IsType<ExclusionGroupAssignmentTarget>(result[0].Target);
        Assert.Equal("g1", target.GroupId);
    }

    [Fact]
    public async Task SearchGroupsCommand_SetsErrorOnFailure()
    {
        _groupService.SearchGroupsAsync("test", Arg.Any<CancellationToken>())
            .Returns<List<Group>>(x => throw new Exception("API error"));

        var vm = new GroupPickerViewModel(_groupService) { SearchText = "test" };

        await vm.SearchGroupsCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("API error", vm.ErrorMessage);
    }
}
