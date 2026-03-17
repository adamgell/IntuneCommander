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
    string? InformationUrl,
    string? PrivacyInformationUrl,
    string? Version,
    string? BundleId,
    string? MinimumOsVersion,
    string? InstallCommand,
    string? UninstallCommand,
    string? InstallContext,
    double? SizeMB,
    string? AppStoreUrl,
    string[] Categories,
    int SupersededAppCount,
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

public sealed record ApplicationAssignmentRowDto(
    string Id,
    string AppId,
    string AppName,
    string Publisher,
    string Description,
    string AppType,
    string Version,
    string Platform,
    string BundleId,
    string PackageId,
    string IsFeatured,
    string CreatedDate,
    string LastModified,
    string AssignmentType,
    string TargetName,
    string TargetGroupId,
    string InstallIntent,
    string AssignmentSettings,
    string IsExclusion,
    string AppStoreUrl,
    string PrivacyUrl,
    string InformationUrl,
    string MinimumOsVersion,
    string MinimumFreeDiskSpaceMB,
    string MinimumMemoryMB,
    string MinimumProcessors,
    string Categories,
    string Notes);

public sealed record AppProtectionPolicyListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string PolicyType,
    string Platform,
    string Version,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int AssignmentCount);

public sealed record AppProtectionPolicyDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string PolicyType,
    string OdataType,
    string Platform,
    string Version,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    string MinimumRequiredAppVersion,
    string MinimumRequiredOsVersion,
    AssignmentDto[] Assignments);

public sealed record ManagedDeviceAppConfigurationListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string Version,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int TargetedMobileAppCount,
    int AssignmentCount);

public sealed record ManagedDeviceAppConfigurationDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string OdataType,
    string Version,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    string[] TargetedMobileApps,
    AssignmentDto[] Assignments);

public sealed record TargetedManagedAppConfigurationListItemDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string Version,
    string AppGroupType,
    bool IsAssigned,
    int DeployedAppCount,
    string CreatedDateTime,
    string LastModifiedDateTime,
    int AssignmentCount);

public sealed record TargetedManagedAppConfigurationDetailDto(
    string Id,
    string DisplayName,
    string? Description,
    string ConfigurationType,
    string OdataType,
    string Version,
    string AppGroupType,
    bool IsAssigned,
    int DeployedAppCount,
    string CreatedDateTime,
    string LastModifiedDateTime,
    string[] RoleScopeTagIds,
    AssignmentDto[] Assignments);

public sealed record VppTokenListItemDto(
    string Id,
    string DisplayName,
    string OrganizationName,
    string AppleId,
    string State,
    string ExpirationDateTime,
    string LastSyncDateTime);

public sealed record VppTokenDetailDto(
    string Id,
    string DisplayName,
    string OrganizationName,
    string AppleId,
    string State,
    string ExpirationDateTime,
    string VppTokenAccountType,
    string LastSyncDateTime,
    string LastSyncStatus,
    string CountryOrRegion,
    string LocationName,
    bool AutomaticallyUpdateApps,
    string[] RoleScopeTagIds);
