namespace Intune.Commander.DesktopReact.Models;

public record TenantAdminListItem(
    string Id,
    string DisplayName,
    string? Description,
    string EntityType,
    string? LastModifiedDateTime);

public record ScopeTagDetail(
    string Id,
    string DisplayName,
    string? Description,
    bool IsDefault);

public record RoleDefinitionDetail(
    string Id,
    string DisplayName,
    string? Description,
    bool IsBuiltIn,
    string[] RolePermissions,
    RoleAssignmentDto[] Assignments);

public record RoleAssignmentDto(
    string Id,
    string DisplayName,
    string[] ScopeTagIds);

public record IntuneBrandingDetail(
    string Id,
    string DisplayName,
    string? CompanyPortalBlockedActions,
    bool IsDefaultProfile,
    string? ContactItName,
    string? ContactItPhoneNumber,
    string? ContactItEmailAddress,
    string? OnlineSupportSiteName,
    string? OnlineSupportSiteUrl,
    string? PrivacyUrl);

public record AzureBrandingDetail(
    string Id,
    string? SignInPageText,
    string? UsernameHintText,
    string? LoginPageTextVisibilitySettings);

public record TermsAndConditionsDetail(
    string Id,
    string DisplayName,
    string? Description,
    string? Title,
    string? BodyText,
    string? AcceptanceStatement,
    int Version,
    string? CreatedDateTime,
    string? LastModifiedDateTime);

public record TermsOfUseDetail(
    string Id,
    string DisplayName,
    bool IsPerDeviceAcceptanceRequired,
    bool IsViewingBeforeAcceptanceRequired,
    string? UserReacceptRequiredFrequency);

public record ReusablePolicySettingDetail(
    string Id,
    string DisplayName,
    string? Description,
    string? SettingDefinitionId,
    string? SettingInstance,
    string? CreatedDateTime,
    string? LastModifiedDateTime);

public record NotificationTemplateDetail(
    string Id,
    string DisplayName,
    string? DefaultLocale,
    string? BrandingOptions,
    string? LastModifiedDateTime);

public record PolicySetDetail(
    string Id,
    string DisplayName,
    string? Description,
    string? Status,
    string? CreatedDateTime,
    string? LastModifiedDateTime);
