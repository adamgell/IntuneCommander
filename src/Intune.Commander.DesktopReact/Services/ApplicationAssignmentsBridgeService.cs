using System.Collections.Concurrent;
using System.Text.Json;
using Intune.Commander.Core.Services;
using Intune.Commander.DesktopReact.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

public class ApplicationAssignmentsBridgeService
{
    private readonly AuthBridgeService _authBridge;
    private readonly ICacheService _cache;
    private readonly ShellStateBridgeService _shellState;
    private readonly ConcurrentDictionary<string, string> _groupNameCache = new(StringComparer.OrdinalIgnoreCase);

    private const string CacheKeyApps = "Applications";
    private const string CacheKeyRows = "ApplicationAssignments";

    private IApplicationService? _service;

    public ApplicationAssignmentsBridgeService(
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
        var client = _authBridge.GraphClient ?? throw new InvalidOperationException("Not connected — authenticate first");
        _service ??= new ApplicationService(client);
        return _service;
    }

    private string? GetTenantId() => _shellState.ActiveProfile?.TenantId;

    public void Reset()
    {
        _service = null;
        _groupNameCache.Clear();
    }

    public async Task<object> ListAsync()
    {
        var tenantId = GetTenantId();
        if (tenantId is not null)
        {
            var cachedRows = _cache.Get<ApplicationAssignmentRowDto>(tenantId, CacheKeyRows);
            if (cachedRows is { Count: > 0 })
                return cachedRows.ToArray();
        }

        var service = GetService();
        var apps = await GroupResolutionHelper.GetCachedOrFetchAsync(
            _cache,
            tenantId,
            CacheKeyApps,
            () => service.ListApplicationsAsync());

        var rows = new List<ApplicationAssignmentRowDto>();
        using var semaphore = new SemaphoreSlim(5, 5);

        var tasks = apps.Select(async app =>
        {
            await semaphore.WaitAsync();
            try
            {
                var appAssignments = app.Id is not null
                    ? await service.GetAssignmentsAsync(app.Id)
                    : [];

                var appRows = await BuildRowsAsync(app, appAssignments);
                lock (rows)
                {
                    rows.AddRange(appRows);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        rows.Sort((left, right) =>
        {
            var byName = string.Compare(left.AppName, right.AppName, StringComparison.OrdinalIgnoreCase);
            return byName != 0
                ? byName
                : string.Compare(left.TargetName, right.TargetName, StringComparison.OrdinalIgnoreCase);
        });

        if (tenantId is not null)
            _cache.Set(tenantId, CacheKeyRows, rows);

        return rows.ToArray();
    }

    public async Task<object> GetDetailAsync(JsonElement? payload)
    {
        if (payload is null || !payload.Value.TryGetProperty("id", out var idProp))
            throw new ArgumentException("Assignment row ID is required");

        var id = idProp.GetString() ?? throw new ArgumentException("Assignment row ID is required");
        var tenantId = GetTenantId();

        if (tenantId is not null)
        {
            var cachedRows = _cache.Get<ApplicationAssignmentRowDto>(tenantId, CacheKeyRows);
            var cachedRow = cachedRows?.FirstOrDefault(row => row.Id == id);
            if (cachedRow is not null)
                return cachedRow;
        }

        var rows = (ApplicationAssignmentRowDto[])await ListAsync();
        return rows.FirstOrDefault(row => row.Id == id)
            ?? throw new InvalidOperationException($"Application assignment row {id} not found");
    }

    private async Task<List<ApplicationAssignmentRowDto>> BuildRowsAsync(
        MobileApp app,
        List<MobileAppAssignment> assignments)
    {
        if (assignments.Count == 0)
            return [BuildRow(app, null, "None", "", "", false, "None")];

        await PreResolveGroupNamesAsync(assignments.Select(assignment => assignment.Target).ToList());

        var rows = new List<ApplicationAssignmentRowDto>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var (assignmentType, targetName, targetGroupId, isExclusion) =
                ResolveAssignmentTarget(assignment.Target);

            rows.Add(BuildRow(
                app,
                assignment,
                assignmentType,
                targetName,
                targetGroupId,
                isExclusion,
                assignment.Intent?.ToString()?.ToLowerInvariant() ?? ""));
        }

        return rows;
    }

    private ApplicationAssignmentRowDto BuildRow(
        MobileApp app,
        MobileAppAssignment? assignment,
        string assignmentType,
        string targetName,
        string targetGroupId,
        bool isExclusion,
        string installIntent)
    {
        return new ApplicationAssignmentRowDto(
            Id: ApplicationDataMapper.BuildAssignmentRowId(app, assignment, targetName, targetGroupId, isExclusion),
            AppId: app.Id ?? "",
            AppName: app.DisplayName ?? "",
            Publisher: app.Publisher ?? "",
            Description: app.Description ?? "",
            AppType: ApplicationDataMapper.FormatAppType(app),
            Version: ApplicationDataMapper.ExtractVersion(app),
            Platform: ApplicationDataMapper.DetectPlatform(app),
            BundleId: ApplicationDataMapper.ExtractBundleId(app),
            PackageId: ApplicationDataMapper.ExtractPackageId(app),
            IsFeatured: app.IsFeatured == true ? "True" : "False",
            CreatedDate: app.CreatedDateTime?.ToString("o") ?? "",
            LastModified: app.LastModifiedDateTime?.ToString("o") ?? "",
            AssignmentType: assignmentType,
            TargetName: targetName,
            TargetGroupId: targetGroupId,
            InstallIntent: installIntent,
            AssignmentSettings: ApplicationDataMapper.FormatAssignmentSettings(assignment?.Settings),
            IsExclusion: isExclusion ? "True" : "False",
            AppStoreUrl: ApplicationDataMapper.ExtractAppStoreUrl(app),
            PrivacyUrl: ApplicationDataMapper.ExtractPrivacyUrl(app),
            InformationUrl: ApplicationDataMapper.ExtractInformationUrl(app),
            MinimumOsVersion: ApplicationDataMapper.ExtractMinimumOS(app),
            MinimumFreeDiskSpaceMB: ApplicationDataMapper.ExtractMinimumFreeDiskSpace(app),
            MinimumMemoryMB: ApplicationDataMapper.ExtractMinimumMemory(app),
            MinimumProcessors: ApplicationDataMapper.ExtractMinimumProcessors(app),
            Categories: ApplicationDataMapper.ExtractCategoriesText(app),
            Notes: ApplicationDataMapper.ExtractNotes(app));
    }

    private async Task PreResolveGroupNamesAsync(List<DeviceAndAppManagementAssignmentTarget?> targets)
    {
        var groupIds = targets.OfType<GroupAssignmentTarget>().Select(g => g.GroupId)
            .Concat(targets.OfType<ExclusionGroupAssignmentTarget>().Select(g => g.GroupId))
            .Where(id => !string.IsNullOrEmpty(id) && !_groupNameCache.ContainsKey(id!))
            .Distinct()
            .ToList();

        if (groupIds.Count == 0 || _authBridge.GraphClient is null)
            return;

        using var semaphore = new SemaphoreSlim(5);
        var tasks = groupIds.Select(async groupId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var group = await _authBridge.GraphClient.Groups[groupId!]
                    .GetAsync(req => req.QueryParameters.Select = ["displayName"]);
                _groupNameCache[groupId!] = group?.DisplayName ?? groupId!;
            }
            catch
            {
                _groupNameCache[groupId!] = groupId!;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private (string AssignmentType, string TargetName, string TargetGroupId, bool IsExclusion) ResolveAssignmentTarget(
        DeviceAndAppManagementAssignmentTarget? target)
    {
        return target switch
        {
            AllDevicesAssignmentTarget => ("All Devices", "All Devices", "", false),
            AllLicensedUsersAssignmentTarget => ("All Users", "All Users", "", false),
            ExclusionGroupAssignmentTarget exclusion => (
                "Group",
                _groupNameCache.GetValueOrDefault(exclusion.GroupId ?? "", exclusion.GroupId ?? "Unknown"),
                exclusion.GroupId ?? "",
                true),
            GroupAssignmentTarget group => (
                "Group",
                _groupNameCache.GetValueOrDefault(group.GroupId ?? "", group.GroupId ?? "Unknown"),
                group.GroupId ?? "",
                false),
            _ => ("Unknown", "Unknown", "", false)
        };
    }
}
