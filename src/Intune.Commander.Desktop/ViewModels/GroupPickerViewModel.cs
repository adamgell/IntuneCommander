using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Services;
using Intune.Commander.Desktop.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.ViewModels;

public partial class GroupPickerViewModel : ViewModelBase
{
    private readonly IGroupService _groupService;
    private CancellationTokenSource? _debounceCts;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _includeAllDevices;
    [ObservableProperty] private bool _includeAllUsers;

    public ObservableCollection<GroupSelectionItem> Groups { get; } = [];
    public ObservableCollection<GroupSelectionItem> SelectedGroups { get; } = [];

    public GroupPickerViewModel(IGroupService groupService)
    {
        _groupService = groupService;
    }

    partial void OnSearchTextChanged(string value)
    {
        // Cancel in-flight debounce without disposing to avoid ObjectDisposedException
        // in Task.Delay continuations that are still running.
        _debounceCts?.Cancel();
        if (string.IsNullOrWhiteSpace(value))
        {
            _debounceCts = null;
            Groups.Clear();
            return;
        }
        var cts = new CancellationTokenSource();
        _debounceCts = cts;
        _ = DebounceSearchAsync(cts.Token);
    }

    private async Task DebounceSearchAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(300, ct);
            await SearchGroupsCoreAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task SearchGroupsAsync(CancellationToken ct)
    {
        // Cancel in-flight debounce without disposing to avoid ObjectDisposedException.
        _debounceCts?.Cancel();
        _debounceCts = null;
        await SearchGroupsCoreAsync(ct);
    }

    private async Task SearchGroupsCoreAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        IsSearching = true;
        ClearError();
        try
        {
            var results = await _groupService.SearchGroupsAsync(SearchText, ct);
            Groups.Clear();
            foreach (var g in results)
            {
                if (!string.IsNullOrEmpty(g.Id))
                {
                    Groups.Add(new GroupSelectionItem(
                        g.Id,
                        g.DisplayName ?? "",
                        GroupService.InferGroupType(g)));
                }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            SetError($"Search failed: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void AddGroup(GroupSelectionItem group)
    {
        if (SelectedGroups.Any(g => g.GroupId == group.GroupId)) return;
        SelectedGroups.Add(new GroupSelectionItem(group.GroupId, group.DisplayName, group.GroupType));
    }

    [RelayCommand]
    private void RemoveGroup(GroupSelectionItem group)
    {
        SelectedGroups.Remove(group);
    }

    public List<T> BuildAssignments<T>() where T : Entity, new()
    {
        var targets = new List<DeviceAndAppManagementAssignmentTarget>();

        if (IncludeAllDevices)
            targets.Add(new AllDevicesAssignmentTarget());
        if (IncludeAllUsers)
            targets.Add(new AllLicensedUsersAssignmentTarget());

        foreach (var g in SelectedGroups)
        {
            if (g.IsExclusion)
                targets.Add(new ExclusionGroupAssignmentTarget { GroupId = g.GroupId });
            else
                targets.Add(new GroupAssignmentTarget { GroupId = g.GroupId });
        }

        return targets.Select(t =>
        {
            T assignment = new();
            switch (assignment)
            {
                case DeviceManagementConfigurationPolicyAssignment sc:
                    sc.Target = t;
                    return (T)(object)sc;
                case DeviceManagementIntentAssignment es:
                    es.Target = t;
                    return (T)(object)es;
                case DeviceCompliancePolicyAssignment cp:
                    cp.Target = t;
                    return (T)(object)cp;
                default:
                    throw new NotSupportedException($"Assignment type {typeof(T).Name} is not supported");
            }
        }).ToList();
    }
}
