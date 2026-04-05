using System.Text.Json;
using System.Text.Json.Nodes;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class PolicyComparisonBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const string CacheKeySettingsCatalog = "SettingsCatalog";
    private const string CacheKeyCompliance = "CompliancePolicies";
    private const string CacheKeyDeviceConfig = "DeviceConfigurations";
    private const string CacheKeyCA = "ConditionalAccess";
    private const string CacheKeyAppProtection = "AppProtection";
    private const string CacheKeyEndpointSecurity = "EndpointSecurity";

    private ISettingsCatalogService? _settingsCatalogService;
    private ICompliancePolicyService? _complianceService;
    private IConfigurationProfileService? _configService;
    private IConditionalAccessPolicyService? _caService;
    private IAppProtectionPolicyService? _appProtectionService;
    private IEndpointSecurityService? _endpointSecurityService;
    private IExportNormalizer? _normalizer;

    public PolicyComparisonBridgeService(
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
        _settingsCatalogService = null;
        _complianceService = null;
        _configService = null;
        _caService = null;
        _appProtectionService = null;
        _endpointSecurityService = null;
        _normalizer = null;
    }

    /// <summary>
    /// List available policies by category for the picker UI.
    /// </summary>
    public async Task<object> ListPoliciesAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("category", out var catProp))
            throw new ArgumentException("Category is required");

        var category = catProp.GetString() ?? throw new ArgumentException("Category is required");
        var client = GetClient();
        var tenantId = GetTenantId();

        return category switch
        {
            "settingsCatalog" => await ListSettingsCatalog(client, tenantId),
            "compliance" => await ListCompliance(client, tenantId),
            "deviceConfiguration" => await ListDeviceConfigs(client, tenantId),
            "conditionalAccess" => await ListCA(client, tenantId),
            "appProtection" => await ListAppProtection(client, tenantId),
            "endpointSecurity" => await ListEndpointSecurity(client, tenantId),
            _ => throw new ArgumentException($"Unknown category: {category}")
        };
    }

    /// <summary>
    /// Compare two policies side-by-side with normalized JSON diff.
    /// </summary>
    public async Task<object> CompareAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Comparison payload is required");

        var p = payload.Value;
        var category = p.GetProperty("category").GetString()
            ?? throw new ArgumentException("Category is required");
        var idA = p.GetProperty("idA").GetString()
            ?? throw new ArgumentException("Policy A ID is required");
        var idB = p.GetProperty("idB").GetString()
            ?? throw new ArgumentException("Policy B ID is required");

        var client = GetClient();
        _normalizer ??= new ExportNormalizer();

        var (nameA, jsonA) = await GetPolicyJson(category, idA, client);
        var (nameB, jsonB) = await GetPolicyJson(category, idB, client);

        // Strip metadata/envelope fields — keep only settings differences
        var settingsOnlyA = StripMetadataFields(jsonA);
        var settingsOnlyB = StripMetadataFields(jsonB);

        var normalizedA = _normalizer.NormalizeJson(settingsOnlyA);
        var normalizedB = _normalizer.NormalizeJson(settingsOnlyB);

        // Count differing properties
        var (total, differing) = CountDifferences(normalizedA, normalizedB);

        return new PolicyComparisonResultDto(
            PolicyAName: nameA,
            PolicyBName: nameB,
            Category: category,
            TotalProperties: total,
            DifferingProperties: differing,
            NormalizedJsonA: normalizedA,
            NormalizedJsonB: normalizedB);
    }

    private async Task<(string Name, string Json)> GetPolicyJson(
        string category, string id, Microsoft.Graph.Beta.GraphServiceClient client)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        switch (category)
        {
            case "settingsCatalog":
                _settingsCatalogService ??= new SettingsCatalogService(client);
                var sc = await _settingsCatalogService.GetSettingsCatalogPolicyAsync(id)
                    ?? throw new InvalidOperationException($"Policy {id} not found");
                return (sc.Name ?? sc.Id ?? id, JsonSerializer.Serialize(sc, options));

            case "compliance":
                _complianceService ??= new CompliancePolicyService(client);
                var cp = await _complianceService.GetCompliancePolicyAsync(id)
                    ?? throw new InvalidOperationException($"Policy {id} not found");
                return (cp.DisplayName ?? cp.Id ?? id, JsonSerializer.Serialize(cp, options));

            case "deviceConfiguration":
                _configService ??= new ConfigurationProfileService(client);
                var dc = await _configService.GetDeviceConfigurationAsync(id)
                    ?? throw new InvalidOperationException($"Profile {id} not found");
                return (dc.DisplayName ?? dc.Id ?? id, JsonSerializer.Serialize(dc, options));

            case "conditionalAccess":
                _caService ??= new ConditionalAccessPolicyService(client);
                var ca = await _caService.GetPolicyAsync(id)
                    ?? throw new InvalidOperationException($"CA Policy {id} not found");
                return (ca.DisplayName ?? ca.Id ?? id, JsonSerializer.Serialize(ca, options));

            case "appProtection":
                _appProtectionService ??= new AppProtectionPolicyService(client);
                var ap = await _appProtectionService.GetAppProtectionPolicyAsync(id)
                    ?? throw new InvalidOperationException($"Policy {id} not found");
                return (ap.DisplayName ?? ap.Id ?? id, JsonSerializer.Serialize(ap, options));

            case "endpointSecurity":
                _endpointSecurityService ??= new EndpointSecurityService(client);
                var es = await _endpointSecurityService.GetEndpointSecurityIntentAsync(id)
                    ?? throw new InvalidOperationException($"Intent {id} not found");
                return (es.DisplayName ?? es.Id ?? id, JsonSerializer.Serialize(es, options));

            default:
                throw new ArgumentException($"Unknown category: {category}");
        }
    }

    /// <summary>
    /// Fields that describe identity/metadata rather than actual policy settings.
    /// Stripped before comparison so only functional differences remain.
    /// </summary>
    private static readonly HashSet<string> MetadataFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Identity & timestamps (also stripped by ExportNormalizer)
        "id", "createdDateTime", "lastModifiedDateTime", "version",
        // Display metadata
        "displayName", "description", "name",
        // Type discriminators
        "@odata.type", "@odata.context",
        // Template & category metadata
        "templateReference", "templateId",
        "platforms", "technologies", "settingCount",
        // Assignment & scoping (not settings)
        "assignments", "roleScopeTagIds", "roleScopeTags",
        // Graph internals
        "isAssigned", "supportsScopeTags",
    };

    private static string StripMetadataFields(string json)
    {
        var root = JsonNode.Parse(json);
        if (root is JsonObject obj)
            StripFieldsRecursive(obj);
        var options = new JsonSerializerOptions { WriteIndented = true };
        return root?.ToJsonString(options) ?? json;
    }

    private static void StripFieldsRecursive(JsonObject obj)
    {
        var keysToRemove = obj
            .Where(p => MetadataFields.Contains(p.Key))
            .Select(p => p.Key)
            .ToList();
        foreach (var key in keysToRemove)
            obj.Remove(key);

        foreach (var kvp in obj.ToList())
        {
            if (kvp.Value is JsonObject childObj)
                StripFieldsRecursive(childObj);
            else if (kvp.Value is JsonArray arr)
                StripFieldsFromArray(arr);
        }
    }

    private static void StripFieldsFromArray(JsonArray arr)
    {
        foreach (var item in arr)
        {
            if (item is JsonObject obj)
                StripFieldsRecursive(obj);
            else if (item is JsonArray nested)
                StripFieldsFromArray(nested);
        }
    }

    private static (int Total, int Differing) CountDifferences(string jsonA, string jsonB)
    {
        try
        {
            using var docA = JsonDocument.Parse(jsonA);
            using var docB = JsonDocument.Parse(jsonB);
            int total = 0, differing = 0;
            CompareElements(docA.RootElement, docB.RootElement, ref total, ref differing);
            return (total, differing);
        }
        catch
        {
            return (0, jsonA != jsonB ? 1 : 0);
        }
    }

    private static void CompareElements(JsonElement a, JsonElement b, ref int total, ref int differing)
    {
        if (a.ValueKind == JsonValueKind.Object && b.ValueKind == JsonValueKind.Object)
        {
            var propsA = new Dictionary<string, JsonElement>();
            foreach (var prop in a.EnumerateObject())
                propsA[prop.Name] = prop.Value;

            var propsB = new Dictionary<string, JsonElement>();
            foreach (var prop in b.EnumerateObject())
                propsB[prop.Name] = prop.Value;

            var allKeys = new HashSet<string>(propsA.Keys);
            allKeys.UnionWith(propsB.Keys);

            foreach (var key in allKeys)
            {
                total++;
                if (!propsA.TryGetValue(key, out var valA) || !propsB.TryGetValue(key, out var valB))
                {
                    differing++;
                }
                else if (valA.ValueKind == JsonValueKind.Object || valA.ValueKind == JsonValueKind.Array)
                {
                    CompareElements(valA, valB, ref total, ref differing);
                }
                else if (valA.GetRawText() != valB.GetRawText())
                {
                    differing++;
                }
            }
        }
        else if (a.ValueKind == JsonValueKind.Array && b.ValueKind == JsonValueKind.Array)
        {
            var maxLen = Math.Max(a.GetArrayLength(), b.GetArrayLength());
            for (int i = 0; i < maxLen; i++)
            {
                total++;
                if (i >= a.GetArrayLength() || i >= b.GetArrayLength())
                    differing++;
                else
                    CompareElements(a[i], b[i], ref total, ref differing);
            }
        }
        else
        {
            total++;
            if (a.GetRawText() != b.GetRawText())
                differing++;
        }
    }

    private async Task<PolicySummaryItemDto[]> ListSettingsCatalog(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _settingsCatalogService ??= new SettingsCatalogService(client);
        var cached = tenantId is not null ? _cache.Get<DeviceManagementConfigurationPolicy>(tenantId, CacheKeySettingsCatalog) : null;
        var policies = cached is { Count: > 0 } ? cached : await _settingsCatalogService.ListSettingsCatalogPoliciesAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeySettingsCatalog, policies);
        return policies.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.Name ?? "", "settingsCatalog")).ToArray();
    }

    private async Task<PolicySummaryItemDto[]> ListCompliance(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _complianceService ??= new CompliancePolicyService(client);
        var cached = tenantId is not null ? _cache.Get<DeviceCompliancePolicy>(tenantId, CacheKeyCompliance) : null;
        var policies = cached is { Count: > 0 } ? cached : await _complianceService.ListCompliancePoliciesAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeyCompliance, policies);
        return policies.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.DisplayName ?? "", "compliance")).ToArray();
    }

    private async Task<PolicySummaryItemDto[]> ListDeviceConfigs(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _configService ??= new ConfigurationProfileService(client);
        var cached = tenantId is not null ? _cache.Get<DeviceConfiguration>(tenantId, CacheKeyDeviceConfig) : null;
        var policies = cached is { Count: > 0 } ? cached : await _configService.ListDeviceConfigurationsAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeyDeviceConfig, policies);
        return policies.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.DisplayName ?? "", "deviceConfiguration")).ToArray();
    }

    private async Task<PolicySummaryItemDto[]> ListCA(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _caService ??= new ConditionalAccessPolicyService(client);
        var cached = tenantId is not null ? _cache.Get<ConditionalAccessPolicy>(tenantId, CacheKeyCA) : null;
        var policies = cached is { Count: > 0 } ? cached : await _caService.ListPoliciesAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeyCA, policies);
        return policies.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.DisplayName ?? "", "conditionalAccess")).ToArray();
    }

    private async Task<PolicySummaryItemDto[]> ListAppProtection(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _appProtectionService ??= new AppProtectionPolicyService(client);
        var cached = tenantId is not null ? _cache.Get<ManagedAppPolicy>(tenantId, CacheKeyAppProtection) : null;
        var policies = cached is { Count: > 0 } ? cached : await _appProtectionService.ListAppProtectionPoliciesAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeyAppProtection, policies);
        return policies.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.DisplayName ?? "", "appProtection")).ToArray();
    }

    private async Task<PolicySummaryItemDto[]> ListEndpointSecurity(Microsoft.Graph.Beta.GraphServiceClient client, string? tenantId)
    {
        _endpointSecurityService ??= new EndpointSecurityService(client);
        var cached = tenantId is not null ? _cache.Get<DeviceManagementIntent>(tenantId, CacheKeyEndpointSecurity) : null;
        var intents = cached is { Count: > 0 } ? cached : await _endpointSecurityService.ListEndpointSecurityIntentsAsync();
        if (cached is null or { Count: 0 } && tenantId is not null)
            _cache.Set(tenantId, CacheKeyEndpointSecurity, intents);
        return intents.Select(p => new PolicySummaryItemDto(p.Id ?? "", p.DisplayName ?? "", "endpointSecurity")).ToArray();
    }
}
