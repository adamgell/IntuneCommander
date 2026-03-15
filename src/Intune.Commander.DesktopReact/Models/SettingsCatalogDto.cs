namespace Intune.Commander.DesktopReact.Models;

public sealed record PolicyListItem(
    string Id,
    string Name,
    string? Description,
    string Platform,
    string ProfileType,
    string Technologies,
    string CreatedDateTime,
    string LastModified,
    string ScopeTag,
    bool IsAssigned,
    int SettingCount);

public sealed record PolicyDetail(
    string Id,
    string Name,
    string? Description,
    string Platform,
    string ProfileType,
    string Technologies,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] ScopeTags,
    int SettingCount,
    bool IsAssigned,
    string? TemplateReference,
    AssignmentData Assignments,
    SettingGroupDto[] SettingGroups);

public sealed record AssignmentData(
    AssignmentEntry[] Included,
    AssignmentEntry[] Excluded);

public sealed record AssignmentEntry(
    string GroupName,
    string Status,
    string? Filter,
    string? FilterMode);

public sealed record SettingGroupDto(
    string Name,
    int SettingCount,
    SettingEntryDto[] Settings);

public sealed record SettingEntryDto(
    string Label,
    string Value);
