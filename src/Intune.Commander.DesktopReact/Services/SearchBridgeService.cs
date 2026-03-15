using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

using SearchResult = Intune.Commander.DesktopReact.Models.SearchResult;

namespace Intune.Commander.DesktopReact.Services;

public class SearchBridgeService
{
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private const int MaxResults = 50;

    /// <summary>
    /// Registry of searchable cache keys: (GraphModelType, NameProperty, CategoryLabel).
    /// NameProperty is the property to read for display name; Description is always checked too.
    /// </summary>
    private static readonly (string CacheKey, Type ModelType, string NameProp, string Label)[] SearchableTypes =
    [
        ("SettingsCatalog", typeof(DeviceManagementConfigurationPolicy), "Name", "Settings Catalog"),
        ("DeviceConfigurations", typeof(DeviceConfiguration), "DisplayName", "Device Configurations"),
        ("CompliancePolicies", typeof(DeviceCompliancePolicy), "DisplayName", "Compliance Policies"),
        ("Applications", typeof(MobileApp), "DisplayName", "Applications"),
        ("ConditionalAccessPolicies", typeof(ConditionalAccessPolicy), "DisplayName", "Conditional Access"),
        ("AssignmentFilters", typeof(DeviceAndAppManagementAssignmentFilter), "DisplayName", "Assignment Filters"),
        ("EndpointSecurityIntents", typeof(DeviceManagementIntent), "DisplayName", "Endpoint Security"),
        ("AdministrativeTemplates", typeof(GroupPolicyConfiguration), "DisplayName", "Administrative Templates"),
        ("EnrollmentConfigurations", typeof(DeviceEnrollmentConfiguration), "DisplayName", "Enrollment Configurations"),
        ("AppProtectionPolicies", typeof(ManagedAppPolicy), "DisplayName", "App Protection Policies"),
        ("AutopilotProfiles", typeof(WindowsAutopilotDeploymentProfile), "DisplayName", "Autopilot Profiles"),
        ("DeviceHealthScripts", typeof(DeviceHealthScript), "DisplayName", "Device Health Scripts"),
        ("DeviceManagementScripts", typeof(DeviceManagementScript), "DisplayName", "Device Scripts"),
        ("DeviceShellScripts", typeof(DeviceShellScript), "DisplayName", "Shell Scripts"),
        ("ComplianceScripts", typeof(DeviceComplianceScript), "DisplayName", "Compliance Scripts"),
        ("FeatureUpdateProfiles", typeof(WindowsFeatureUpdateProfile), "DisplayName", "Feature Updates"),
        ("QualityUpdateProfiles", typeof(WindowsQualityUpdateProfile), "DisplayName", "Quality Updates"),
        ("NamedLocations", typeof(NamedLocation), "DisplayName", "Named Locations"),
        ("ScopeTags", typeof(RoleScopeTag), "DisplayName", "Scope Tags"),
        ("RoleDefinitions", typeof(RoleDefinition), "DisplayName", "Role Definitions"),
        ("TermsAndConditions", typeof(TermsAndConditions), "DisplayName", "Terms and Conditions"),
        ("NotificationTemplates", typeof(NotificationMessageTemplate), "DisplayName", "Notification Templates"),
        ("DeviceCategories", typeof(DeviceCategory), "DisplayName", "Device Categories"),
        ("DynamicGroups", typeof(Group), "DisplayName", "Dynamic Groups"),
        ("AssignedGroups", typeof(Group), "DisplayName", "Assigned Groups"),
    ];

    public SearchBridgeService(ICacheService cache, ShellStateBridgeService shellState)
    {
        _cache = cache;
        _shellState = shellState;
    }

    public Task<object> SearchAsync(JsonElement? payload)
    {
        var query = payload?.TryGetProperty("query", out var q) == true
            ? q.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Task.FromResult<object>(Array.Empty<SearchResult>());

        var tenantId = _shellState.ActiveProfile?.TenantId;
        if (tenantId is null)
            return Task.FromResult<object>(Array.Empty<SearchResult>());

        var results = new List<SearchResult>();

        foreach (var (cacheKey, modelType, nameProp, label) in SearchableTypes)
        {
            if (results.Count >= MaxResults) break;

            // Skip if nothing cached for this type
            var meta = _cache.GetMetadata(tenantId, cacheKey);
            if (meta is null) continue;

            try
            {
                SearchCollection(tenantId, cacheKey, modelType, nameProp, label, query, results);
            }
            catch
            {
                // Skip types that fail to deserialize
            }
        }

        return Task.FromResult<object>(results.ToArray());
    }

    private void SearchCollection(
        string tenantId, string cacheKey, Type modelType,
        string nameProp, string label, string query,
        List<SearchResult> results)
    {
        // Use reflection to call _cache.Get<T> with the correct type
        var method = typeof(ICacheService).GetMethod("Get")!.MakeGenericMethod(modelType);
        var items = method.Invoke(_cache, [tenantId, cacheKey]);
        if (items is not System.Collections.IEnumerable enumerable) return;

        var nameGetter = modelType.GetProperty(nameProp);
        var descGetter = modelType.GetProperty("Description");
        var idGetter = modelType.GetProperty("Id");

        foreach (var item in enumerable)
        {
            if (results.Count >= MaxResults) break;

            var name = nameGetter?.GetValue(item) as string;
            var description = descGetter?.GetValue(item) as string;
            var id = idGetter?.GetValue(item) as string;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(description)) continue;

            var nameMatch = name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
            var descMatch = description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

            if (nameMatch || descMatch)
            {
                results.Add(new SearchResult(
                    Category: label,
                    CategoryKey: cacheKey,
                    Id: id ?? "",
                    Name: name ?? "(unnamed)",
                    Description: Truncate(description, 120)));
            }
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength) return value;
        return value[..maxLength] + "...";
    }
}
