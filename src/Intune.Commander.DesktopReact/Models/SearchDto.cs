namespace Intune.Commander.DesktopReact.Models;

public sealed record SearchResult(
    string Category,
    string CategoryKey,
    string Id,
    string Name,
    string? Description);
