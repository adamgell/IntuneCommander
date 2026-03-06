namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// Extended assignment display item for Device Health Scripts that includes
/// schedule information and remediation flag.
/// </summary>
public class HealthScriptAssignmentDisplayItem : AssignmentDisplayItem
{
    /// <summary>Human-readable schedule description, e.g. "Daily at 02:00 (UTC)"</summary>
    public string Schedule { get; init; } = "";

    /// <summary>Whether remediation script will also run (not just detection).</summary>
    public bool RunRemediation { get; init; }

    /// <summary>Display-friendly text for RunRemediation.</summary>
    public string RunRemediationText => RunRemediation ? "Yes" : "No";
}
