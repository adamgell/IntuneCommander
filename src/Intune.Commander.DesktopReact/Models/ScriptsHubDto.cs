namespace Intune.Commander.DesktopReact.Models;

public record ScriptListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string ScriptType,
    string Platform,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string? RunAsAccount,
    bool? RunAs32Bit,
    bool? EnforceSignatureCheck,
    bool? HasRemediation,
    string? Status,
    int? NoIssueDetectedCount,
    int? IssueDetectedCount,
    int? IssueRemediatedCount);

public record ScriptDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string ScriptType,
    string Platform,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string? RunAsAccount,
    bool? RunAs32Bit,
    bool? EnforceSignatureCheck,
    string ScriptContent,
    string? RemediationScriptContent,
    string Language,
    ScriptAssignmentDto[] Assignments);

public record ScriptAssignmentDto(
    string Target,
    string TargetKind);
