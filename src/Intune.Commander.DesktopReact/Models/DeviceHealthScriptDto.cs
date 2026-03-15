namespace Intune.Commander.DesktopReact.Models;

public sealed record HealthScriptListItem(
    string Id,
    string DisplayName,
    string? Description,
    string Publisher,
    string Version,
    string RunAsAccount,
    bool RunAs32Bit,
    bool EnforceSignatureCheck,
    bool IsGlobal,
    string CreatedDateTime,
    string LastModified,
    int DeviceHealthScriptType,
    bool HasRemediation,
    string Status,
    int NoIssueDetectedCount,
    int IssueDetectedCount,
    int IssueRemediatedCount,
    int IssueReoccurredCount,
    int TotalRemediatedCount);

public sealed record HealthScriptDetail(
    string Id,
    string DisplayName,
    string? Description,
    string Publisher,
    string Version,
    string RunAsAccount,
    bool RunAs32Bit,
    bool EnforceSignatureCheck,
    bool IsGlobal,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    string DetectionScript,
    string RemediationScript,
    RunSummaryDto? RunSummary,
    DeviceRunStateDto[] DeviceRunStates,
    HealthScriptAssignmentDto[] Assignments);

public sealed record RunSummaryDto(
    int NoIssueDetectedCount,
    int IssueDetectedCount,
    int IssueRemediatedCount,
    int IssueReoccurredCount,
    int ErrorDeviceCount,
    string? LastScriptRunDateTime);

public sealed record DeviceRunStateDto(
    string DeviceName,
    string DetectionState,
    string RemediationState,
    string LastStateUpdateDateTime,
    string? UserPrincipalName,
    string? PreRemediationDetectionScriptOutput,
    string? PreRemediationDetectionScriptError,
    string? PostRemediationDetectionScriptOutput,
    string? PostRemediationDetectionScriptError,
    string? RemediationScriptError,
    string? ExpectedStateUpdateDateTime,
    string? LastSyncDateTime);

public sealed record HealthScriptAssignmentDto(
    string Target,
    string TargetKind,
    string Schedule,
    bool RunRemediation);
