namespace Intune.Commander.DesktopReact.Models;

// -- Device Configuration DTOs --
public record DeviceConfigListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int AssignmentCount);

public record DeviceConfigDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    AssignmentDto[] Assignments,
    string RawJson);

// -- Compliance Policy DTOs --
public record CompliancePolicyListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string PolicyType,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int AssignmentCount);

public record CompliancePolicyDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string PolicyType,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    AssignmentDto[] Assignments,
    string RawJson);

// -- Endpoint Security DTOs --
public record EndpointSecurityListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string IntentType,
    bool IsAssigned,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int AssignmentCount);

public record EndpointSecurityDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string IntentType,
    bool IsAssigned,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    AssignmentDto[] Assignments,
    string RawJson);

// -- Enrollment & Autopilot DTOs --
public record EnrollmentConfigListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    int Priority,
    string CreatedDateTime,
    string LastModifiedDateTime);

public record EnrollmentConfigDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    int Priority,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    string RawJson);

// Shared assignment DTO
public record AssignmentDto(
    string Target,
    string TargetKind);
