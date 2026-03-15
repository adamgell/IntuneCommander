namespace Intune.Commander.DesktopReact.Models;

public sealed record CaPolicyListItem(
    string Id,
    string DisplayName,
    string State,
    string CreatedDateTime,
    string ModifiedDateTime,
    string? Description,
    CaConditionsSummary Conditions,
    string[] GrantControls,
    string[] SessionControls);

public sealed record CaConditionsSummary(
    string Users,
    string Applications,
    string Platforms,
    string Locations,
    string ClientAppTypes,
    string SignInRiskLevels,
    string UserRiskLevels);

public sealed record CaPolicyDetail(
    string Id,
    string DisplayName,
    string State,
    string CreatedDateTime,
    string ModifiedDateTime,
    string? Description,
    CaConditionsDetail Conditions,
    CaGrantControls GrantControls,
    CaSessionControls SessionControls);

public sealed record CaConditionsDetail(
    string[] IncludeUsers,
    string[] ExcludeUsers,
    string[] IncludeGroups,
    string[] ExcludeGroups,
    string[] IncludeApplications,
    string[] ExcludeApplications,
    string[] IncludePlatforms,
    string[] ExcludePlatforms,
    string[] IncludeLocations,
    string[] ExcludeLocations,
    string[] ClientAppTypes,
    string[] SignInRiskLevels,
    string[] UserRiskLevels);

public sealed record CaGrantControls(
    string Operator,
    string[] BuiltInControls,
    string[] CustomAuthenticationFactors,
    string? AuthenticationStrength);

public sealed record CaSessionControls(
    bool ApplicationEnforcedRestrictions,
    bool CloudAppSecurity,
    string? SignInFrequency,
    string? PersistentBrowser,
    bool DisableResilienceDefaults);
