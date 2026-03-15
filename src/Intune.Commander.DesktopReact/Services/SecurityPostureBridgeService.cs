using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class SecurityPostureBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeyCA = "ConditionalAccess";
    private const string CacheKeyCompliance = "CompliancePolicies";
    private const string CacheKeyEndpointSecurity = "EndpointSecurity";
    private const string CacheKeyAppProtection = "AppProtection";
    private const string CacheKeyAuthStrength = "AuthStrength";
    private const string CacheKeyNamedLocations = "NamedLocations";

    private IConditionalAccessPolicyService? _caService;
    private ICompliancePolicyService? _complianceService;
    private IEndpointSecurityService? _endpointSecurityService;
    private IAppProtectionPolicyService? _appProtectionService;
    private IAuthenticationStrengthService? _authStrengthService;
    private INamedLocationService? _namedLocationService;

    public SecurityPostureBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private Microsoft.Graph.Beta.GraphServiceClient GetClient() =>
        _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public void Reset()
    {
        _caService = null;
        _complianceService = null;
        _endpointSecurityService = null;
        _appProtectionService = null;
        _authStrengthService = null;
        _namedLocationService = null;
    }

    public async Task<object> GetSummaryAsync()
    {
        var client = GetClient();
        var tenantId = GetTenantId();

        _caService ??= new ConditionalAccessPolicyService(client);
        _complianceService ??= new CompliancePolicyService(client);
        _endpointSecurityService ??= new EndpointSecurityService(client);
        _appProtectionService ??= new AppProtectionPolicyService(client);
        _authStrengthService ??= new AuthenticationStrengthService(client);
        _namedLocationService ??= new NamedLocationService(client);

        // Fetch all data in parallel, using cache where available
        var caTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyCA, () => _caService.ListPoliciesAsync());
        var complianceTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyCompliance, () => _complianceService.ListCompliancePoliciesAsync());
        var endpointTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyEndpointSecurity, () => _endpointSecurityService.ListEndpointSecurityIntentsAsync());
        var appProtectionTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyAppProtection, () => _appProtectionService.ListAppProtectionPoliciesAsync());
        var authStrengthTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyAuthStrength, () => _authStrengthService.ListAuthenticationStrengthPoliciesAsync());
        var namedLocationsTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyNamedLocations, () => _namedLocationService.ListNamedLocationsAsync());

        await Task.WhenAll(caTask, complianceTask, endpointTask, appProtectionTask, authStrengthTask, namedLocationsTask);

        var caPolicies = await caTask;
        var compliancePolicies = await complianceTask;
        var endpointIntents = await endpointTask;
        var appProtection = await appProtectionTask;
        var authStrength = await authStrengthTask;
        var namedLocations = await namedLocationsTask;

        // Compute scores and gaps
        var (score, breakdown, gaps) = ComputeSecurityScore(
            caPolicies, compliancePolicies, endpointIntents,
            appProtection, authStrength, namedLocations);

        var compliancePlatforms = compliancePolicies
            .Select(p => DetectPlatform(p))
            .Where(p => p != "Unknown")
            .Distinct()
            .OrderBy(p => p)
            .ToArray();

        return new SecurityPostureSummary(
            CaEnabled: caPolicies.Count(p => p.State == ConditionalAccessPolicyState.Enabled),
            CaReportOnly: caPolicies.Count(p => p.State == ConditionalAccessPolicyState.EnabledForReportingButNotEnforced),
            CaDisabled: caPolicies.Count(p => p.State == ConditionalAccessPolicyState.Disabled),
            CaTotal: caPolicies.Count,
            CompliancePolicies: compliancePolicies.Count,
            CompliancePlatforms: compliancePlatforms,
            EndpointSecurityIntents: endpointIntents.Count,
            AppProtectionPolicies: appProtection.Count,
            AuthStrengthPolicies: authStrength.Count,
            NamedLocations: namedLocations.Count,
            SecurityScore: score,
            ScoreBreakdown: breakdown,
            Gaps: gaps);
    }

    public async Task<object> GetDetailAsync()
    {
        var client = GetClient();
        var tenantId = GetTenantId();

        _caService ??= new ConditionalAccessPolicyService(client);
        _complianceService ??= new CompliancePolicyService(client);
        _endpointSecurityService ??= new EndpointSecurityService(client);
        _appProtectionService ??= new AppProtectionPolicyService(client);
        _authStrengthService ??= new AuthenticationStrengthService(client);
        _namedLocationService ??= new NamedLocationService(client);

        var caTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyCA, () => _caService.ListPoliciesAsync());
        var complianceTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyCompliance, () => _complianceService.ListCompliancePoliciesAsync());
        var endpointTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyEndpointSecurity, () => _endpointSecurityService.ListEndpointSecurityIntentsAsync());
        var appProtectionTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyAppProtection, () => _appProtectionService.ListAppProtectionPoliciesAsync());
        var authStrengthTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyAuthStrength, () => _authStrengthService.ListAuthenticationStrengthPoliciesAsync());
        var namedLocationsTask = GroupResolutionHelper.GetCachedOrFetchAsync(_cache, tenantId, CacheKeyNamedLocations, () => _namedLocationService.ListNamedLocationsAsync());

        await Task.WhenAll(caTask, complianceTask, endpointTask, appProtectionTask, authStrengthTask, namedLocationsTask);

        return new SecurityPostureDetail(
            ConditionalAccessPolicies: (await caTask).Select(p => new CaPolicySummaryItem(
                p.Id ?? "", p.DisplayName ?? "", p.State?.ToString() ?? "disabled"
            )).ToArray(),
            CompliancePolicies: (await complianceTask).Select(p => new CompliancePolicySummaryItem(
                p.Id ?? "", p.DisplayName ?? "", DetectPlatform(p)
            )).ToArray(),
            EndpointSecurityIntents: (await endpointTask).Select(p => new EndpointSecurityItem(
                p.Id ?? "", p.DisplayName ?? "", DetectIntentCategory(p)
            )).ToArray(),
            AppProtectionPolicies: (await appProtectionTask).Select(p => new AppProtectionItem(
                p.Id ?? "", p.DisplayName ?? "", DetectAppProtectionPlatform(p)
            )).ToArray(),
            AuthStrengthPolicies: (await authStrengthTask).Select(p => new AuthStrengthItem(
                p.Id ?? "", p.DisplayName ?? "",
                p.AllowedCombinations?.Select(c => c?.ToString() ?? "").ToArray() ?? []
            )).ToArray(),
            NamedLocations: (await namedLocationsTask).Select(p => new NamedLocationItem(
                p.Id ?? "", p.DisplayName ?? "",
                p is IpNamedLocation ? "IP" : p is CountryNamedLocation ? "Country" : "Unknown",
                p is IpNamedLocation ip ? ip.IsTrusted ?? false : false
            )).ToArray()
        );
    }

    private static (int Score, ScoreCategory[] Breakdown, SecurityGap[] Gaps) ComputeSecurityScore(
        List<ConditionalAccessPolicy> ca,
        List<DeviceCompliancePolicy> compliance,
        List<DeviceManagementIntent> endpoint,
        List<ManagedAppPolicy> appProtection,
        List<AuthenticationStrengthPolicy> authStrength,
        List<NamedLocation> namedLocations)
    {
        var categories = new List<ScoreCategory>();
        var gaps = new List<SecurityGap>();

        // Conditional Access (max 30 points)
        var caEnabled = ca.Count(p => p.State == ConditionalAccessPolicyState.Enabled);
        var caReportOnly = ca.Count(p => p.State == ConditionalAccessPolicyState.EnabledForReportingButNotEnforced);
        var caScore = Math.Min(30, caEnabled * 5 + caReportOnly * 2);
        var caItems = new List<string>();
        if (caEnabled > 0) caItems.Add($"{caEnabled} enabled");
        if (caReportOnly > 0) caItems.Add($"{caReportOnly} report-only");
        categories.Add(new ScoreCategory("Conditional Access", caScore, 30, caItems.ToArray()));

        if (caEnabled == 0)
            gaps.Add(new SecurityGap("high", "Conditional Access", "No Conditional Access policies are enabled"));
        if (ca.All(p => p.Conditions?.Users?.IncludeUsers?.Contains("All") != true))
            gaps.Add(new SecurityGap("medium", "Conditional Access", "No CA policy targets all users"));

        // Compliance (max 25 points)
        var platforms = compliance.Select(DetectPlatform).Where(p => p != "Unknown").Distinct().ToList();
        var complianceScore = Math.Min(25, compliance.Count * 4 + platforms.Count * 3);
        categories.Add(new ScoreCategory("Compliance", complianceScore, 25,
            [$"{compliance.Count} policies", $"{platforms.Count} platforms covered"]));

        if (compliance.Count == 0)
            gaps.Add(new SecurityGap("high", "Compliance", "No compliance policies configured"));
        if (!platforms.Contains("Windows") && !platforms.Contains("windows10"))
            gaps.Add(new SecurityGap("medium", "Compliance", "No Windows compliance policy detected"));

        // Endpoint Security (max 20 points)
        var endpointScore = Math.Min(20, endpoint.Count * 5);
        categories.Add(new ScoreCategory("Endpoint Security", endpointScore, 20,
            [$"{endpoint.Count} intents configured"]));

        if (endpoint.Count == 0)
            gaps.Add(new SecurityGap("medium", "Endpoint Security", "No endpoint security intents configured"));

        // App Protection (max 15 points)
        var appScore = Math.Min(15, appProtection.Count * 5);
        categories.Add(new ScoreCategory("App Protection", appScore, 15,
            [$"{appProtection.Count} policies"]));

        if (appProtection.Count == 0)
            gaps.Add(new SecurityGap("medium", "App Protection", "No app protection policies — mobile data is unprotected"));

        // Auth Strength + Named Locations (max 10 points)
        var miscScore = Math.Min(10, authStrength.Count * 3 + namedLocations.Count * 2);
        categories.Add(new ScoreCategory("Auth & Locations", miscScore, 10,
            [$"{authStrength.Count} auth strengths", $"{namedLocations.Count} named locations"]));

        if (namedLocations.Count == 0)
            gaps.Add(new SecurityGap("low", "Named Locations", "No named locations — consider adding trusted locations"));

        var totalScore = categories.Sum(c => c.Score);
        return (totalScore, categories.ToArray(), gaps.ToArray());
    }

    private static string DetectPlatform(DeviceCompliancePolicy policy)
    {
        var typeName = policy.GetType().Name;
        if (typeName.Contains("Windows")) return "Windows";
        if (typeName.Contains("Ios") || typeName.Contains("IOS")) return "iOS";
        if (typeName.Contains("Android")) return "Android";
        if (typeName.Contains("MacOS") || typeName.Contains("Macos")) return "macOS";
        return "Unknown";
    }

    private static string DetectIntentCategory(DeviceManagementIntent intent)
    {
        var name = intent.DisplayName?.ToLowerInvariant() ?? "";
        if (name.Contains("antivirus") || name.Contains("defender")) return "Antivirus";
        if (name.Contains("firewall")) return "Firewall";
        if (name.Contains("encryption") || name.Contains("bitlocker")) return "Disk Encryption";
        if (name.Contains("edr")) return "EDR";
        if (name.Contains("attack surface") || name.Contains("asr")) return "Attack Surface Reduction";
        return "Other";
    }

    private static string DetectAppProtectionPlatform(ManagedAppPolicy policy)
    {
        var typeName = policy.GetType().Name;
        if (typeName.Contains("Ios") || typeName.Contains("IOS")) return "iOS";
        if (typeName.Contains("Android")) return "Android";
        return "Other";
    }
}
