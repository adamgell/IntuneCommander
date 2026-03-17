namespace Intune.Commander.DesktopReact.Models;

public sealed record AppListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string? Publisher,
    string AppType,
    string Platform,
    string CreatedDateTime,
    string LastModifiedDateTime,
    bool IsAssigned,
    string PublishingState,
    bool IsFeatured);

public sealed record AppDetail(
    string Id,
    string DisplayName,
    string? Description,
    string? Publisher,
    string AppType,
    string Platform,
    string CreatedDateTime,
    string LastModifiedDateTime,
    bool IsAssigned,
    string PublishingState,
    bool IsFeatured,
    string? Developer,
    string? Owner,
    string? Notes,
    string? Version,
    string? BundleId,
    string? MinimumOsVersion,
    string? InstallCommand,
    string? UninstallCommand,
    string? InstallContext,
    double? SizeMB,
    string? AppStoreUrl,
    AppAssignmentData Assignments);

public sealed record AppAssignmentData(
    AppAssignmentEntry[] Required,
    AppAssignmentEntry[] Available,
    AppAssignmentEntry[] Uninstall);

public sealed record AppAssignmentEntry(
    string GroupName,
    string Intent,
    bool IsExclusion,
    string? Filter,
    string? FilterMode);
