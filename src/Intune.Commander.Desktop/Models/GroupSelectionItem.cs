namespace Intune.Commander.Desktop.Models;

public record GroupSelectionItem(string GroupId, string DisplayName, string? GroupType)
{
    public bool IsExclusion { get; set; }
}
