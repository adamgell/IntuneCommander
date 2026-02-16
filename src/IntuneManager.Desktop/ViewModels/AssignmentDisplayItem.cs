namespace IntuneManager.Desktop.ViewModels;

/// <summary>
/// Lightweight display model for showing assignments in the detail pane.
/// Avoids binding directly to Graph SDK types.
/// </summary>
public class AssignmentDisplayItem
{
    /// <summary>"All Devices", "All Users", "Group: {id}", "Exclude: {id}"</summary>
    public required string Target { get; init; }

    /// <summary>"Include" or "Exclude"</summary>
    public required string TargetKind { get; init; }

    /// <summary>For apps only – "Required", "Available", "Uninstall", etc. Empty for configs/policies.</summary>
    public string Intent { get; init; } = "";
}
