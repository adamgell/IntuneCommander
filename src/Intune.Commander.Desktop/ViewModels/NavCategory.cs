using CommunityToolkit.Mvvm.ComponentModel;

namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Represents a navigation category in the left nav panel.
/// </summary>
public partial class NavCategory : ObservableObject
{
    public required string Name { get; init; }
    public required string Icon { get; init; }

    [ObservableProperty]
    private bool _isSelected;

    public override string ToString() => Name;
}
