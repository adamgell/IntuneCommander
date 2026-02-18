using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntuneManager.Core.Models;
using IntuneManager.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Desktop.ViewModels;

public partial class GroupLookupViewModel : ViewModelBase
{
    private readonly IGroupService _groupService;
    private readonly IConfigurationProfileService _configService;
    private readonly ICompliancePolicyService _complianceService;
    private readonly IApplicationService _appService;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _statusText = "Enter a group name or GUID to search";

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isLoadingAssignments;

    [ObservableProperty]
    private Group? _selectedGroup;

    [ObservableProperty]
    private string _selectedGroupInfo = "";

    [ObservableProperty]
    private string _resultSummary = "";

    public ObservableCollection<Group> SearchResults { get; } = [];

    public ObservableCollection<GroupAssignedObject> AssignmentResults { get; } = [];

    [ObservableProperty]
    private ObservableCollection<GroupAssignedObject> _filteredAssignmentResults = [];

    // Filter toggles — null means "show all"
    [ObservableProperty]
    private string? _activeFilter;

    // Summaries per category
    [ObservableProperty]
    private int _configCount;
    [ObservableProperty]
    private int _complianceCount;
    [ObservableProperty]
    private int _appCount;
    [ObservableProperty]
    private int _totalCount;

    public GroupLookupViewModel(
        IGroupService groupService,
        IConfigurationProfileService configService,
        ICompliancePolicyService complianceService,
        IApplicationService appService)
    {
        _groupService = groupService;
        _configService = configService;
        _complianceService = complianceService;
        _appService = appService;
    }

    [RelayCommand]
    private void FilterByCategory(string? category)
    {
        // Toggle: clicking the active filter again clears it
        ActiveFilter = ActiveFilter == category ? null : category;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrEmpty(ActiveFilter))
        {
            FilteredAssignmentResults = new ObservableCollection<GroupAssignedObject>(AssignmentResults);
        }
        else
        {
            FilteredAssignmentResults = new ObservableCollection<GroupAssignedObject>(
                AssignmentResults.Where(r => r.Category == ActiveFilter));
        }
    }

    [RelayCommand]
    private async Task SearchGroupsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        ClearError();
        IsSearching = true;
        SearchResults.Clear();
        AssignmentResults.Clear();
        FilteredAssignmentResults.Clear();
        ActiveFilter = null;
        SelectedGroup = null;
        SelectedGroupInfo = "";
        ResultSummary = "";

        try
        {
            StatusText = "Searching groups...";
            var groups = await _groupService.SearchGroupsAsync(SearchQuery.Trim(), cancellationToken);

            foreach (var g in groups)
                SearchResults.Add(g);

            StatusText = groups.Count == 0
                ? "No groups found"
                : $"Found {groups.Count} group(s) — select one to see assignments";

            // If exactly one result, auto-select it
            if (groups.Count == 1)
            {
                SelectedGroup = groups[0];
            }
        }
        catch (Exception ex)
        {
            SetError($"Search failed: {ex.Message}");
            StatusText = "Search failed";
        }
        finally
        {
            IsSearching = false;
        }
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        if (value != null)
        {
            SelectedGroupInfo = $"{value.DisplayName}  ({GroupService.InferGroupType(value)})  •  {value.Id}";
            _ = LoadAssignmentsAsync(value.Id!, CancellationToken.None);
        }
        else
        {
            SelectedGroupInfo = "";
        }
    }

    private async Task LoadAssignmentsAsync(string groupId, CancellationToken cancellationToken)
    {
        ClearError();
        IsLoadingAssignments = true;
        IsBusy = true;
        AssignmentResults.Clear();
        ConfigCount = 0;
        ComplianceCount = 0;
        AppCount = 0;
        TotalCount = 0;
        ResultSummary = "";

        try
        {
            var results = await _groupService.GetGroupAssignmentsAsync(
                groupId,
                _configService,
                _complianceService,
                _appService,
                progress => Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusText = progress),
                cancellationToken);

            foreach (var r in results)
                AssignmentResults.Add(r);

            ConfigCount = results.Count(r => r.Category == "Device Configuration");
            ComplianceCount = results.Count(r => r.Category == "Compliance Policy");
            AppCount = results.Count(r => r.Category == "Application");
            TotalCount = results.Count;

            ActiveFilter = null;
            ApplyFilter();

            ResultSummary = $"{TotalCount} assignment(s):  {ConfigCount} configs  •  {ComplianceCount} compliance  •  {AppCount} apps";
            StatusText = TotalCount == 0
                ? "No assignments found for this group"
                : ResultSummary;
        }
        catch (Exception ex)
        {
            SetError($"Failed to load assignments: {ex.Message}");
            StatusText = "Error loading assignments";
        }
        finally
        {
            IsLoadingAssignments = false;
            IsBusy = false;
        }
    }
}
