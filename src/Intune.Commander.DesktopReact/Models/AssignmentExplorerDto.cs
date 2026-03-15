namespace Intune.Commander.DesktopReact.Models;

public record AssignmentReportRowDto(
    string PolicyId,
    string PolicyName,
    string PolicyType,
    string Platform,
    string AssignmentSummary,
    string AssignmentReason,
    string GroupId,
    string GroupName,
    string Group1Status,
    string Group2Status,
    string TargetDevice,
    string UserPrincipalName,
    string Status,
    string LastReported);

public record GroupSearchResultDto(
    string Id,
    string DisplayName,
    string GroupType,
    string? MembershipRule);
