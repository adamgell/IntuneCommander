using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ConditionalAccessBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyCA = "ConditionalAccess";

    private IConditionalAccessPolicyService? _service;

    public ConditionalAccessBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IConditionalAccessPolicyService GetService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _service ??= new ConditionalAccessPolicyService(client);
        return _service;
    }

    public void Reset()
    {
        _service = null;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        var service = GetService();

        if (tenantId is not null)
        {
            var cached = _cache.Get<ConditionalAccessPolicy>(tenantId, CacheKeyCA);
            if (cached is { Count: > 0 })
                return MapPolicies(cached);
        }

        var policies = await service.ListPoliciesAsync();

        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyCA, policies);

        return MapPolicies(policies);
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Policy ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Policy ID is required");
        var service = GetService();

        var policy = await service.GetPolicyAsync(id)
            ?? throw new InvalidOperationException($"CA Policy {id} not found");

        return MapPolicyDetail(policy);
    }

    private static string FormatState(ConditionalAccessPolicyState? state) => state switch
    {
        ConditionalAccessPolicyState.Enabled => "enabled",
        ConditionalAccessPolicyState.Disabled => "disabled",
        ConditionalAccessPolicyState.EnabledForReportingButNotEnforced => "enabledForReportingButNotEnforced",
        _ => "disabled",
    };

    private static CaPolicyListItem[] MapPolicies(List<ConditionalAccessPolicy> policies)
    {
        return policies.Select(p => new CaPolicyListItem(
            Id: p.Id ?? "",
            DisplayName: p.DisplayName ?? "",
            State: FormatState(p.State),
            CreatedDateTime: p.CreatedDateTime?.ToString("o") ?? "",
            ModifiedDateTime: p.ModifiedDateTime?.ToString("o") ?? "",
            Description: p.Description,
            Conditions: MapConditionsSummary(p.Conditions),
            GrantControls: MapGrantControlsSummary(p.GrantControls),
            SessionControls: MapSessionControlsSummary(p.SessionControls)
        )).ToArray();
    }

    private static CaConditionsSummary MapConditionsSummary(ConditionalAccessConditionSet? conditions)
    {
        if (conditions is null)
            return new CaConditionsSummary("None", "None", "Any", "Any", "Any", "None", "None");

        return new CaConditionsSummary(
            Users: SummarizeUsers(conditions.Users),
            Applications: SummarizeApplications(conditions.Applications),
            Platforms: SummarizePlatforms(conditions.Platforms),
            Locations: SummarizeLocations(conditions.Locations),
            ClientAppTypes: conditions.ClientAppTypes is { Count: > 0 }
                ? string.Join(", ", conditions.ClientAppTypes.Select(t => t?.ToString() ?? ""))
                : "Any",
            SignInRiskLevels: conditions.SignInRiskLevels is { Count: > 0 }
                ? string.Join(", ", conditions.SignInRiskLevels.Select(r => r?.ToString() ?? ""))
                : "None",
            UserRiskLevels: conditions.UserRiskLevels is { Count: > 0 }
                ? string.Join(", ", conditions.UserRiskLevels.Select(r => r?.ToString() ?? ""))
                : "None"
        );
    }

    private static string SummarizeUsers(ConditionalAccessUsers? users)
    {
        if (users is null) return "None";
        if (users.IncludeUsers?.Contains("All") == true) return "All users";
        if (users.IncludeUsers?.Contains("GuestsOrExternalUsers") == true) return "Guests/External";
        var count = (users.IncludeUsers?.Count ?? 0) + (users.IncludeGroups?.Count ?? 0);
        return count > 0 ? $"{count} user(s)/group(s)" : "None";
    }

    private static string SummarizeApplications(ConditionalAccessApplications? apps)
    {
        if (apps is null) return "None";
        if (apps.IncludeApplications?.Contains("All") == true) return "All apps";
        if (apps.IncludeApplications?.Contains("Office365") == true) return "Office 365";
        var count = apps.IncludeApplications?.Count ?? 0;
        return count > 0 ? $"{count} app(s)" : "None";
    }

    private static string SummarizePlatforms(ConditionalAccessPlatforms? platforms)
    {
        if (platforms is null) return "Any";
        if (platforms.IncludePlatforms?.Contains(ConditionalAccessDevicePlatform.All) == true) return "All platforms";
        return platforms.IncludePlatforms is { Count: > 0 }
            ? string.Join(", ", platforms.IncludePlatforms.Select(p => p?.ToString() ?? ""))
            : "Any";
    }

    private static string SummarizeLocations(ConditionalAccessLocations? locations)
    {
        if (locations is null) return "Any";
        if (locations.IncludeLocations?.Contains("All") == true) return "All locations";
        return locations.IncludeLocations is { Count: > 0 }
            ? $"{locations.IncludeLocations.Count} location(s)"
            : "Any";
    }

    private static string[] MapGrantControlsSummary(ConditionalAccessGrantControls? grant)
    {
        if (grant is null) return [];
        return grant.BuiltInControls?.Select(c => c?.ToString() ?? "").ToArray() ?? [];
    }

    private static string[] MapSessionControlsSummary(ConditionalAccessSessionControls? session)
    {
        if (session is null) return [];
        var controls = new List<string>();
        if (session.ApplicationEnforcedRestrictions?.IsEnabled == true) controls.Add("App restrictions");
        if (session.CloudAppSecurity?.IsEnabled == true) controls.Add("Cloud app security");
        if (session.SignInFrequency?.IsEnabled == true) controls.Add($"Sign-in freq: {session.SignInFrequency.Value} {session.SignInFrequency.Type}");
        if (session.PersistentBrowser?.IsEnabled == true) controls.Add($"Persistent browser: {session.PersistentBrowser.Mode}");
        return controls.ToArray();
    }

    private static CaPolicyDetail MapPolicyDetail(ConditionalAccessPolicy policy)
    {
        var conditions = policy.Conditions;
        var grant = policy.GrantControls;
        var session = policy.SessionControls;

        return new CaPolicyDetail(
            Id: policy.Id ?? "",
            DisplayName: policy.DisplayName ?? "",
            State: FormatState(policy.State),
            CreatedDateTime: policy.CreatedDateTime?.ToString("o") ?? "",
            ModifiedDateTime: policy.ModifiedDateTime?.ToString("o") ?? "",
            Description: policy.Description,
            Conditions: new CaConditionsDetail(
                IncludeUsers: conditions?.Users?.IncludeUsers?.ToArray() ?? [],
                ExcludeUsers: conditions?.Users?.ExcludeUsers?.ToArray() ?? [],
                IncludeGroups: conditions?.Users?.IncludeGroups?.ToArray() ?? [],
                ExcludeGroups: conditions?.Users?.ExcludeGroups?.ToArray() ?? [],
                IncludeApplications: conditions?.Applications?.IncludeApplications?.ToArray() ?? [],
                ExcludeApplications: conditions?.Applications?.ExcludeApplications?.ToArray() ?? [],
                IncludePlatforms: conditions?.Platforms?.IncludePlatforms?.Select(p => p?.ToString() ?? "").ToArray() ?? [],
                ExcludePlatforms: conditions?.Platforms?.ExcludePlatforms?.Select(p => p?.ToString() ?? "").ToArray() ?? [],
                IncludeLocations: conditions?.Locations?.IncludeLocations?.ToArray() ?? [],
                ExcludeLocations: conditions?.Locations?.ExcludeLocations?.ToArray() ?? [],
                ClientAppTypes: conditions?.ClientAppTypes?.Select(t => t?.ToString() ?? "").ToArray() ?? [],
                SignInRiskLevels: conditions?.SignInRiskLevels?.Select(r => r?.ToString() ?? "").ToArray() ?? [],
                UserRiskLevels: conditions?.UserRiskLevels?.Select(r => r?.ToString() ?? "").ToArray() ?? []
            ),
            GrantControls: new CaGrantControls(
                Operator: grant?.Operator ?? "OR",
                BuiltInControls: grant?.BuiltInControls?.Select(c => c?.ToString() ?? "").ToArray() ?? [],
                CustomAuthenticationFactors: grant?.CustomAuthenticationFactors?.ToArray() ?? [],
                AuthenticationStrength: grant?.AuthenticationStrength?.DisplayName
            ),
            SessionControls: new CaSessionControls(
                ApplicationEnforcedRestrictions: session?.ApplicationEnforcedRestrictions?.IsEnabled ?? false,
                CloudAppSecurity: session?.CloudAppSecurity?.IsEnabled ?? false,
                SignInFrequency: session?.SignInFrequency?.IsEnabled == true
                    ? $"{session.SignInFrequency.Value} {session.SignInFrequency.Type}"
                    : null,
                PersistentBrowser: session?.PersistentBrowser?.IsEnabled == true
                    ? session.PersistentBrowser.Mode?.ToString()
                    : null,
                DisableResilienceDefaults: session?.DisableResilienceDefaults ?? false
            ));
    }
}
