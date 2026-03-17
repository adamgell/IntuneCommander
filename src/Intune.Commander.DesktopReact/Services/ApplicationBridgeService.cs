using System.Collections.Concurrent;
using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ApplicationBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private readonly ConcurrentDictionary<string, string> _groupNameCache = new(StringComparer.OrdinalIgnoreCase);

    internal const string CacheKeyApplications = "Applications";
    internal const string CacheKeyApplicationDetail = "ApplicationDetail";

    private IApplicationService? _service;

    public ApplicationBridgeService(
        AuthBridgeService authBridge,
        ICacheService cache,
        ShellStateBridgeService shellState)
    {
        _authBridge = authBridge;
        _cache = cache;
        _shellState = shellState;
    }

    private IApplicationService GetService()
    {
        var client = _authBridge.GraphClient
            ?? throw new InvalidOperationException("Not connected — authenticate first");

        _service ??= new ApplicationService(client);
        return _service;
    }

    public void Reset()
    {
        _service = null;
        _groupNameCache.Clear();
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        var service = GetService();

        if (tenantId is not null)
        {
            var cached = _cache.Get<MobileApp>(tenantId, CacheKeyApplications);
            if (cached is { Count: > 0 })
                return MapApps(cached);
        }

        var apps = await service.ListApplicationsAsync();

        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyApplications, apps);

        return MapApps(apps);
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("App ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("App ID is required");
        var tenantId = GetTenantId();
        var service = GetService();

        if (tenantId is not null)
        {
            var cached = _cache.GetSingle<AppDetail>(tenantId, $"{CacheKeyApplicationDetail}_{id}");
            if (cached is not null)
                return cached;
        }

        var app = await service.GetApplicationAsync(id)
            ?? throw new InvalidOperationException($"Application {id} not found");

        var assignments = await service.GetAssignmentsAsync(id);
        var assignmentData = await MapAssignmentsAsync(assignments);

        var detail = new AppDetail(
            Id: app.Id ?? "",
            DisplayName: app.DisplayName ?? "",
            Description: app.Description,
            Publisher: app.Publisher,
            AppType: ApplicationDataMapper.FormatAppType(app),
            Platform: ApplicationDataMapper.DetectPlatform(app),
            CreatedDateTime: app.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: app.LastModifiedDateTime?.ToString("o") ?? "",
            IsAssigned: app.IsAssigned ?? false,
            PublishingState: app.PublishingState?.ToString() ?? "Unknown",
            IsFeatured: app.IsFeatured ?? false,
            Developer: app.Developer,
            Owner: app.Owner,
            Notes: app.Notes,
            InformationUrl: app.InformationUrl,
            PrivacyInformationUrl: app.PrivacyInformationUrl,
            Version: ApplicationDataMapper.ExtractVersion(app),
            BundleId: ApplicationDataMapper.ExtractBundleId(app),
            MinimumOsVersion: ApplicationDataMapper.ExtractMinimumOS(app),
            InstallCommand: ApplicationDataMapper.ExtractInstallCommand(app),
            UninstallCommand: ApplicationDataMapper.ExtractUninstallCommand(app),
            InstallContext: ApplicationDataMapper.ExtractInstallContext(app),
            SizeMB: ApplicationDataMapper.ExtractSizeInMB(app),
            AppStoreUrl: ApplicationDataMapper.ExtractAppStoreUrl(app),
            Categories: ApplicationDataMapper.ExtractCategories(app),
            SupersededAppCount: ApplicationDataMapper.ExtractSupersededCount(app),
            Assignments: assignmentData);

        if (tenantId is not null)
            _cache.SetSingle(tenantId, $"{CacheKeyApplicationDetail}_{id}", detail);

        return detail;
    }

    internal static AppListItemDto[] MapApps(List<MobileApp> apps)
    {
        return apps.Select(a => new AppListItemDto(
            Id: a.Id ?? "",
            DisplayName: a.DisplayName ?? "",
            Description: a.Description,
            Publisher: a.Publisher,
            AppType: ApplicationDataMapper.FormatAppType(a),
            Platform: ApplicationDataMapper.DetectPlatform(a),
            CreatedDateTime: a.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: a.LastModifiedDateTime?.ToString("o") ?? "",
            IsAssigned: a.IsAssigned ?? false,
            PublishingState: a.PublishingState?.ToString() ?? "Unknown",
            IsFeatured: a.IsFeatured ?? false
        )).ToArray();
    }

    private async Task<AppAssignmentData> MapAssignmentsAsync(List<MobileAppAssignment> assignments)
    {
        var required = new List<AppAssignmentEntry>();
        var available = new List<AppAssignmentEntry>();
        var uninstall = new List<AppAssignmentEntry>();

        // Pre-resolve all group names in parallel
        await PreResolveGroupNamesAsync(assignments.Select(a => a.Target).ToList());

        foreach (var a in assignments)
        {
            var intent = a.Intent?.ToString() ?? "Unknown";
            var isExclusion = a.Target is ExclusionGroupAssignmentTarget;
            var groupName = ResolveAssignmentTargetName(a.Target);
            var filter = a.Target?.DeviceAndAppManagementAssignmentFilterId;
            var filterMode = a.Target?.DeviceAndAppManagementAssignmentFilterType?.ToString();

            var entry = new AppAssignmentEntry(groupName, intent, isExclusion, filter, filterMode);

            switch (a.Intent)
            {
                case InstallIntent.Required:
                    required.Add(entry);
                    break;
                case InstallIntent.Available:
                case InstallIntent.AvailableWithoutEnrollment:
                    available.Add(entry);
                    break;
                case InstallIntent.Uninstall:
                    uninstall.Add(entry);
                    break;
                default:
                    required.Add(entry);
                    break;
            }
        }

        return new AppAssignmentData(required.ToArray(), available.ToArray(), uninstall.ToArray());
    }

    private async Task PreResolveGroupNamesAsync(List<DeviceAndAppManagementAssignmentTarget?> targets)
    {
        var groupIds = targets.OfType<GroupAssignmentTarget>().Select(g => g.GroupId)
            .Concat(targets.OfType<ExclusionGroupAssignmentTarget>().Select(g => g.GroupId))
            .Where(id => !string.IsNullOrEmpty(id) && Guid.TryParse(id, out _) && !_groupNameCache.ContainsKey(id!))
            .Distinct()
            .ToList();

        if (groupIds.Count == 0) return;

        var client = _authBridge.GraphClient;
        if (client is null) return;

        using var sem = new SemaphoreSlim(5);
        var tasks = groupIds.Select(async gid =>
        {
            await sem.WaitAsync();
            try
            {
                var g = await client.Groups[gid].GetAsync(r => r.QueryParameters.Select = ["displayName"]);
                if (g?.DisplayName is not null)
                    _groupNameCache[gid!] = g.DisplayName;
                else
                    _groupNameCache[gid!] = gid!;
            }
            catch { _groupNameCache[gid!] = gid!; }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }

    private string ResolveAssignmentTargetName(DeviceAndAppManagementAssignmentTarget? target)
    {
        return target switch
        {
            AllDevicesAssignmentTarget => "All Devices",
            AllLicensedUsersAssignmentTarget => "All Users",
            ExclusionGroupAssignmentTarget excl => _groupNameCache.GetValueOrDefault(excl.GroupId ?? "", excl.GroupId ?? "Unknown"),
            GroupAssignmentTarget grp => _groupNameCache.GetValueOrDefault(grp.GroupId ?? "", grp.GroupId ?? "Unknown"),
            _ => "Unknown Target"
        };
    }
}
