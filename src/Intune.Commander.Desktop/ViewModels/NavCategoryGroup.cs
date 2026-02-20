using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Represents a collapsible group of navigation categories in the left nav panel.
/// </summary>
public partial class NavCategoryGroup : ObservableObject
{
    public required string Name { get; init; }
    public required string Icon { get; init; }

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<NavCategory> Children { get; init; } = [];
}
