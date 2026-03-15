namespace Intune.Commander.DesktopReact.Models;

public sealed record GroupListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string GroupType,
    string? MembershipRule,
    int MemberCount,
    string? Mail,
    string CreatedDateTime);

public sealed record GroupDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string GroupType,
    string? MembershipRule,
    string? MembershipRuleProcessingState,
    bool MailEnabled,
    bool SecurityEnabled,
    string? Mail,
    string CreatedDateTime,
    GroupMemberCountsDto MemberCounts,
    GroupMemberInfoDto[] Members,
    GroupAssignedObjectDto[] Assignments);

public sealed record GroupMemberCountsDto(
    int Users,
    int Devices,
    int NestedGroups,
    int Total);

public sealed record GroupMemberInfoDto(
    string MemberType,
    string DisplayName,
    string SecondaryInfo,
    string TertiaryInfo,
    string Status,
    string Id);

public sealed record GroupAssignedObjectDto(
    string ObjectId,
    string DisplayName,
    string ObjectType,
    string Category,
    string Platform,
    string AssignmentIntent,
    bool IsExclusion);
