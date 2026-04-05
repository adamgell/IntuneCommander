namespace Intune.Commander.DesktopReact.Models;

public record PolicySummaryItemDto(
    string Id,
    string DisplayName,
    string Category);

public record PolicyComparisonResultDto(
    string PolicyAName,
    string PolicyBName,
    string Category,
    int TotalProperties,
    int DifferingProperties,
    string NormalizedJsonA,
    string NormalizedJsonB,
    SettingDiffItem[]? SettingsDiff);

public record SettingDiffItem(
    string Label,
    string Category,
    string? ValueA,
    string? ValueB,
    string Status);
