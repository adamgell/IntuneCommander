namespace Intune.Commander.DesktopReact.Models;

public record SecurityPostureSummary(
    int CaEnabled,
    int CaReportOnly,
    int CaDisabled,
    int CaTotal,
    int CompliancePolicies,
    string[] CompliancePlatforms,
    int EndpointSecurityIntents,
    int AppProtectionPolicies,
    int AuthStrengthPolicies,
    int NamedLocations,
    int SecurityScore,
    ScoreCategory[] ScoreBreakdown,
    SecurityGap[] Gaps);

public record ScoreCategory(
    string Category,
    int Score,
    int MaxScore,
    string[] Items);

public record SecurityGap(
    string Severity,
    string Category,
    string Description);

public record CaPolicySummaryItem(
    string Id,
    string DisplayName,
    string State);

public record CompliancePolicySummaryItem(
    string Id,
    string DisplayName,
    string Platform);

public record EndpointSecurityItem(
    string Id,
    string DisplayName,
    string Category);

public record AppProtectionItem(
    string Id,
    string DisplayName,
    string Platform);

public record AuthStrengthItem(
    string Id,
    string DisplayName,
    string[] AllowedCombinations);

public record NamedLocationItem(
    string Id,
    string DisplayName,
    string LocationType,
    bool IsTrusted);

public record SecurityPostureDetail(
    CaPolicySummaryItem[] ConditionalAccessPolicies,
    CompliancePolicySummaryItem[] CompliancePolicies,
    EndpointSecurityItem[] EndpointSecurityIntents,
    AppProtectionItem[] AppProtectionPolicies,
    AuthStrengthItem[] AuthStrengthPolicies,
    NamedLocationItem[] NamedLocations);
