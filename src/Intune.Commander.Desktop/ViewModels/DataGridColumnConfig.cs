using CommunityToolkit.Mvvm.ComponentModel;

namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Represents a user-configurable column in a DataGrid.
/// </summary>
public partial class DataGridColumnConfig : ObservableObject
{
    /// <summary>Column header text shown in the DataGrid.</summary>
    public required string Header { get; init; }

    /// <summary>
    /// Property path to bind to (e.g. "DisplayName", "LastModifiedDateTime").
    /// For computed columns, use a special prefix like "Computed:" handled in code-behind.
    /// </summary>
    public required string BindingPath { get; init; }

    /// <summary>Column width. Use 0 for star-sized (fill remaining).</summary>
    public double Width { get; init; } = 150;

    /// <summary>Whether this column is star-sized (fills remaining space).</summary>
    public bool IsStar { get; init; }

    [ObservableProperty]
    private bool _isVisible = true;
}
