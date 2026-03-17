namespace Intune.Commander.DesktopReact.Models;

public sealed record AssignmentFilterListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string Platform,
    string AssignmentFilterManagementType,
    string Rule);

public sealed record BulkAppAssignmentBootstrapDto(
    AppListItemDto[] Apps,
    AssignmentFilterListItemDto[] AssignmentFilters);

public sealed record BulkAppAssignmentTargetDto(
    string TargetType,
    string? TargetId,
    string DisplayName,
    bool IsExclusion,
    string? FilterId,
    string FilterMode);

public sealed record BulkAppAssignmentApplyResultDto(
    int RequestedAppCount,
    int SucceededAppCount,
    int FailedAppCount,
    BulkAppAssignmentAppResultDto[] Results);

public sealed record BulkAppAssignmentAppResultDto(
    string AppId,
    string AppName,
    bool Success,
    int FinalAssignmentCount,
    string? Error);
