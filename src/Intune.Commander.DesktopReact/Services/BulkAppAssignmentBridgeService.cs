using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Bridge;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class BulkAppAssignmentBridgeService
{
    private const string CacheKeyAssignmentFilters = "AssignmentFilters";
    private const string CacheKeyApplicationAssignments = "ApplicationAssignments";

    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;

    private IApplicationService? _applicationService;
    private IAssignmentFilterService? _assignmentFilterService;

    public BulkAppAssignmentBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IApplicationService GetApplicationService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _applicationService ??= new ApplicationService(client);
        return _applicationService;
    }

    private IAssignmentFilterService GetAssignmentFilterService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _assignmentFilterService ??= new AssignmentFilterService(client);
        return _assignmentFilterService;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public void Reset()
    {
        _applicationService = null;
        _assignmentFilterService = null;
    }

    public async Task<object> GetBootstrapAsync()
    {
        var tenantId = GetTenantId();
        var appService = GetApplicationService();
        var filterService = GetAssignmentFilterService();

        List<MobileApp>? apps = null;
        List<DeviceAndAppManagementAssignmentFilter>? filters = null;

        if (tenantId is not null)
        {
            apps = _cache.Get<MobileApp>(tenantId, ApplicationBridgeService.CacheKeyApplications);
            filters = _cache.Get<DeviceAndAppManagementAssignmentFilter>(tenantId, CacheKeyAssignmentFilters);
        }

        if (apps is null || apps.Count == 0)
        {
            apps = await appService.ListApplicationsAsync(CancellationToken.None);
            if (tenantId is not null)
                _cache.Set(tenantId, ApplicationBridgeService.CacheKeyApplications, apps);
        }

        if (filters is null || filters.Count == 0)
        {
            filters = await filterService.ListFiltersAsync(CancellationToken.None);
            if (tenantId is not null)
                _cache.Set(tenantId, CacheKeyAssignmentFilters, filters);
        }

        return new BulkAppAssignmentBootstrapDto(
            Apps: ApplicationBridgeService.MapApps(apps),
            AssignmentFilters: MapFilters(filters));
    }

    public async Task<object> ApplyAsync(JsonElement? payload)
    {
        if (payload is null)
            throw new ArgumentException("Bulk assignment payload is required");

        var request = JsonSerializer.Deserialize<BulkAppAssignmentApplyRequest>(payload.Value.GetRawText(), BridgeRouter.JsonOptions)
            ?? throw new ArgumentException("Invalid bulk assignment payload");

        var appIds = request.AppIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (appIds.Length == 0)
            throw new ArgumentException("At least one app must be selected");

        if (request.Targets is null || request.Targets.Length == 0)
            throw new ArgumentException("At least one target is required");

        if (!Enum.TryParse<InstallIntent>(request.Intent, ignoreCase: true, out var intent))
            throw new ArgumentException($"Unsupported install intent: {request.Intent}");

        var normalizedTargets = request.Targets.Select(NormalizeTargetRequest).ToArray();
        var applicationService = GetApplicationService();
        var results = new List<BulkAppAssignmentAppResultDto>(appIds.Length);
        var tenantId = GetTenantId();
        var anySuccess = false;

        Debug.WriteLine($"[BulkAppAssignments] Apply start | apps={appIds.Length} | targets={normalizedTargets.Length} | intent={intent}");

        foreach (var appId in appIds)
        {
            var appName = appId;

            try
            {
                var currentAssignments = await applicationService.GetAssignmentsAsync(appId, CancellationToken.None);
                var app = await ResolveAppAsync(appId);
                appName = app.DisplayName ?? appId;

                var newAssignments = normalizedTargets
                    .Select(target => BuildAssignmentForTarget(target, intent, currentAssignments, app))
                    .ToList();

                var replacementKeys = new HashSet<string>(
                    newAssignments.Select(BuildAssignmentIdentity),
                    StringComparer.OrdinalIgnoreCase);

                var mergedAssignments = currentAssignments
                    .Where(existing => !replacementKeys.Contains(BuildAssignmentIdentity(existing)))
                    .ToList();

                mergedAssignments.AddRange(newAssignments);
                await applicationService.AssignApplicationAsync(appId, mergedAssignments, CancellationToken.None);

                anySuccess = true;
                InvalidateAppCaches(tenantId, appId);

                results.Add(new BulkAppAssignmentAppResultDto(
                    AppId: appId,
                    AppName: appName,
                    Success: true,
                    FinalAssignmentCount: mergedAssignments.Count,
                    Error: null));

                Debug.WriteLine($"[BulkAppAssignments] Apply success | appId={appId} | assignments={mergedAssignments.Count}");
            }
            catch (Exception ex)
            {
                results.Add(new BulkAppAssignmentAppResultDto(
                    AppId: appId,
                    AppName: appName,
                    Success: false,
                    FinalAssignmentCount: 0,
                    Error: ex.Message));

                Debug.WriteLine($"[BulkAppAssignments] Apply failure | appId={appId} | error={ex.Message}");
            }
        }

        if (anySuccess && tenantId is not null)
            _cache.Invalidate(tenantId, CacheKeyApplicationAssignments);

        var successCount = results.Count(result => result.Success);
        var failureCount = results.Count - successCount;

        Debug.WriteLine($"[BulkAppAssignments] Apply complete | requested={results.Count} | succeeded={successCount} | failed={failureCount}");

        return new BulkAppAssignmentApplyResultDto(
            RequestedAppCount: results.Count,
            SucceededAppCount: successCount,
            FailedAppCount: failureCount,
            Results: results.ToArray());
    }

    private async Task<MobileApp> ResolveAppAsync(string appId)
    {
        var tenantId = GetTenantId();
        if (tenantId is not null)
        {
            var cachedApps = _cache.Get<MobileApp>(tenantId, ApplicationBridgeService.CacheKeyApplications);
            var cachedApp = cachedApps?.FirstOrDefault(app => string.Equals(app.Id, appId, StringComparison.OrdinalIgnoreCase));
            if (cachedApp is not null)
                return cachedApp;
        }

        return await GetApplicationService().GetApplicationAsync(appId, CancellationToken.None)
            ?? new MobileApp { Id = appId, DisplayName = appId };
    }

    private static BulkAppAssignmentTargetRequest NormalizeTargetRequest(BulkAppAssignmentTargetRequest target)
    {
        if (string.IsNullOrWhiteSpace(target.TargetType))
            throw new ArgumentException("Target type is required");

        var normalizedType = target.TargetType.Trim();
        var normalizedMode = string.IsNullOrWhiteSpace(target.FilterMode)
            ? DeviceAndAppManagementAssignmentFilterType.None.ToString()
            : target.FilterMode.Trim();

        if (!Enum.TryParse<DeviceAndAppManagementAssignmentFilterType>(normalizedMode, ignoreCase: true, out var filterMode))
            throw new ArgumentException($"Unsupported filter mode: {target.FilterMode}");

        if ((normalizedType.Equals("allUsers", StringComparison.OrdinalIgnoreCase)
             || normalizedType.Equals("allDevices", StringComparison.OrdinalIgnoreCase))
            && target.IsExclusion)
        {
            throw new ArgumentException("All Users and All Devices targets cannot be exclusions");
        }

        if (normalizedType.Equals("group", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(target.TargetId))
        {
            throw new ArgumentException("A group target requires a targetId");
        }

        if (filterMode != DeviceAndAppManagementAssignmentFilterType.None
            && string.IsNullOrWhiteSpace(target.FilterId))
        {
            throw new ArgumentException("A filter must be selected when filter mode is include or exclude");
        }

        return target with
        {
            TargetType = normalizedType,
            FilterMode = filterMode.ToString(),
            FilterId = filterMode == DeviceAndAppManagementAssignmentFilterType.None ? null : target.FilterId?.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(target.DisplayName) ? normalizedType : target.DisplayName.Trim(),
            TargetId = string.IsNullOrWhiteSpace(target.TargetId) ? null : target.TargetId.Trim()
        };
    }

    private MobileAppAssignment BuildAssignmentForTarget(
        BulkAppAssignmentTargetRequest target,
        InstallIntent intent,
        IReadOnlyList<MobileAppAssignment> currentAssignments,
        MobileApp app)
    {
        var targetIdentity = BuildAssignmentIdentity(target, intent);

        var settingsSource = currentAssignments
            .FirstOrDefault(existing =>
                string.Equals(BuildAssignmentIdentity(existing), targetIdentity, StringComparison.OrdinalIgnoreCase)
                && existing.Settings is not null)
            ?.Settings
            ?? currentAssignments.FirstOrDefault(existing => existing.Intent == intent && existing.Settings is not null)?.Settings
            ?? currentAssignments.FirstOrDefault(existing => existing.Settings is not null)?.Settings;

        return new MobileAppAssignment
        {
            Intent = intent,
            Target = BuildTarget(target),
            Settings = GetCompatibleSettings(settingsSource, app)
        };
    }

    private static DeviceAndAppManagementAssignmentTarget BuildTarget(BulkAppAssignmentTargetRequest target)
    {
        if (!Enum.TryParse<DeviceAndAppManagementAssignmentFilterType>(target.FilterMode, ignoreCase: true, out var filterMode))
            throw new ArgumentException($"Unsupported filter mode: {target.FilterMode}");

        DeviceAndAppManagementAssignmentTarget assignmentTarget = target.TargetType switch
        {
            var type when type.Equals("allUsers", StringComparison.OrdinalIgnoreCase) => new AllLicensedUsersAssignmentTarget(),
            var type when type.Equals("allDevices", StringComparison.OrdinalIgnoreCase) => new AllDevicesAssignmentTarget(),
            var type when type.Equals("group", StringComparison.OrdinalIgnoreCase) && target.IsExclusion => new ExclusionGroupAssignmentTarget
            {
                GroupId = target.TargetId
            },
            var type when type.Equals("group", StringComparison.OrdinalIgnoreCase) => new GroupAssignmentTarget
            {
                GroupId = target.TargetId
            },
            _ => throw new ArgumentException($"Unsupported target type: {target.TargetType}")
        };

        assignmentTarget.DeviceAndAppManagementAssignmentFilterType = filterMode;
        assignmentTarget.DeviceAndAppManagementAssignmentFilterId = filterMode == DeviceAndAppManagementAssignmentFilterType.None
            ? null
            : target.FilterId;

        return assignmentTarget;
    }

    private static string BuildAssignmentIdentity(MobileAppAssignment assignment)
    {
        var target = assignment.Target;
        var targetType = target switch
        {
            AllLicensedUsersAssignmentTarget => "allUsers",
            AllDevicesAssignmentTarget => "allDevices",
            ExclusionGroupAssignmentTarget => "group",
            GroupAssignmentTarget => "group",
            _ => target?.GetType().Name ?? "unknown"
        };

        var targetId = target switch
        {
            AllLicensedUsersAssignmentTarget => "allUsers",
            AllDevicesAssignmentTarget => "allDevices",
            ExclusionGroupAssignmentTarget exclusion => exclusion.GroupId ?? string.Empty,
            GroupAssignmentTarget group => group.GroupId ?? string.Empty,
            _ => string.Empty
        };

        var isExclusion = target is ExclusionGroupAssignmentTarget;
        var filterId = target?.DeviceAndAppManagementAssignmentFilterId ?? string.Empty;
        var filterMode = target?.DeviceAndAppManagementAssignmentFilterType?.ToString() ?? DeviceAndAppManagementAssignmentFilterType.None.ToString();
        var intent = assignment.Intent?.ToString() ?? string.Empty;

        return $"{targetType}|{targetId}|{isExclusion}|{filterId}|{filterMode}|{intent}";
    }

    private static string BuildAssignmentIdentity(BulkAppAssignmentTargetRequest target, InstallIntent intent)
    {
        return $"{target.TargetType}|{target.TargetId ?? target.TargetType}|{target.IsExclusion}|{target.FilterId ?? string.Empty}|{target.FilterMode}|{intent}";
    }

    private static MobileAppAssignmentSettings? GetCompatibleSettings(
        MobileAppAssignmentSettings? settingsSource,
        MobileApp app)
    {
        var cloned = CloneSettings(settingsSource);
        if (IsSettingsCompatible(app, cloned))
            return cloned;

        return CreateDefaultSettings(app);
    }

    private static MobileAppAssignmentSettings? CloneSettings(MobileAppAssignmentSettings? source)
    {
        if (source is null)
            return null;

        if (Activator.CreateInstance(source.GetType()) is not MobileAppAssignmentSettings clone)
            return null;

        foreach (var property in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite || property.Name == nameof(MobileAppAssignmentSettings.BackingStore))
                continue;

            if (property.Name == nameof(MobileAppAssignmentSettings.AdditionalData)
                && property.GetValue(source) is IDictionary<string, object> additionalData)
            {
                property.SetValue(clone, new Dictionary<string, object>(additionalData));
                continue;
            }

            property.SetValue(clone, property.GetValue(source));
        }

        return clone;
    }

    private static bool IsSettingsCompatible(MobileApp app, MobileAppAssignmentSettings? settings)
    {
        if (settings is null)
            return true;

        return app switch
        {
            Win32CatalogApp => false,
            Win32LobApp => settings is Win32LobAppAssignmentSettings,
            IosVppApp => settings is IosVppAppAssignmentSettings,
            _ => true
        };
    }

    private static MobileAppAssignmentSettings? CreateDefaultSettings(MobileApp app)
    {
        return app switch
        {
            Win32CatalogApp => null,
            Win32LobApp => new Win32LobAppAssignmentSettings(),
            IosVppApp => new IosVppAppAssignmentSettings(),
            _ => null
        };
    }

    private void InvalidateAppCaches(string? tenantId, string appId)
    {
        if (tenantId is null)
            return;

        _cache.Invalidate(tenantId, ApplicationBridgeService.CacheKeyApplications);
        _cache.Invalidate(tenantId, $"{ApplicationBridgeService.CacheKeyApplicationDetail}_{appId}");
    }

    private static AssignmentFilterListItemDto[] MapFilters(IEnumerable<DeviceAndAppManagementAssignmentFilter> filters)
    {
        return filters
            .Where(filter => !string.IsNullOrWhiteSpace(filter.Id))
            .Select(filter => new AssignmentFilterListItemDto(
                Id: filter.Id!,
                DisplayName: filter.DisplayName ?? filter.Id!,
                Description: filter.Description,
                Platform: filter.Platform?.ToString() ?? "Unknown",
                AssignmentFilterManagementType: filter.AssignmentFilterManagementType?.ToString() ?? "Unknown",
                Rule: filter.Rule ?? string.Empty))
            .OrderBy(filter => filter.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record BulkAppAssignmentApplyRequest(
        string[] AppIds,
        string Intent,
        BulkAppAssignmentTargetRequest[] Targets);

    private sealed record BulkAppAssignmentTargetRequest(
        string TargetType,
        string? TargetId,
        string DisplayName,
        bool IsExclusion,
        string? FilterId,
        string FilterMode);
}
