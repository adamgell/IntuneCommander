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

    private const string CacheKeyApplications = "Applications";

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
        var service = GetService();

        var app = await service.GetApplicationAsync(id)
            ?? throw new InvalidOperationException($"Application {id} not found");

        var assignments = await service.GetAssignmentsAsync(id);
        var assignmentData = await MapAssignmentsAsync(assignments);

        return new AppDetail(
            Id: app.Id ?? "",
            DisplayName: app.DisplayName ?? "",
            Description: app.Description,
            Publisher: app.Publisher,
            AppType: FormatAppType(app),
            Platform: DetectPlatform(app),
            CreatedDateTime: app.CreatedDateTime?.ToString("o") ?? "",
            LastModifiedDateTime: app.LastModifiedDateTime?.ToString("o") ?? "",
            IsAssigned: app.IsAssigned ?? false,
            PublishingState: app.PublishingState?.ToString() ?? "Unknown",
            IsFeatured: app.IsFeatured ?? false,
            Developer: app.Developer,
            Owner: app.Owner,
            Notes: app.Notes,
            Version: ExtractVersion(app),
            BundleId: ExtractBundleId(app),
            MinimumOsVersion: ExtractMinimumOs(app),
            InstallCommand: ExtractInstallCommand(app),
            UninstallCommand: ExtractUninstallCommand(app),
            InstallContext: ExtractInstallContext(app),
            SizeMB: ExtractSize(app),
            AppStoreUrl: ExtractStoreUrl(app),
            Assignments: assignmentData);
    }

    private static AppListItem[] MapApps(List<MobileApp> apps)
    {
        return apps.Select(a => new AppListItem(
            Id: a.Id ?? "",
            DisplayName: a.DisplayName ?? "",
            Description: a.Description,
            Publisher: a.Publisher,
            AppType: FormatAppType(a),
            Platform: DetectPlatform(a),
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

        foreach (var a in assignments)
        {
            var intent = a.Intent?.ToString() ?? "Unknown";
            var isExclusion = a.Target is ExclusionGroupAssignmentTarget;
            var groupName = await ResolveAssignmentTargetNameAsync(a.Target);

            var entry = new AppAssignmentEntry(groupName, intent, isExclusion, null, null);

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

    private async Task<string> ResolveAssignmentTargetNameAsync(DeviceAndAppManagementAssignmentTarget? target)
    {
        return target switch
        {
            AllDevicesAssignmentTarget => "All Devices",
            AllLicensedUsersAssignmentTarget => "All Users",
            ExclusionGroupAssignmentTarget excl => await ResolveGroupNameAsync(excl.GroupId),
            GroupAssignmentTarget grp => await ResolveGroupNameAsync(grp.GroupId),
            _ => "Unknown Target"
        };
    }

    private async Task<string> ResolveGroupNameAsync(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return "Unknown Group";
        if (!Guid.TryParse(groupId, out _)) return groupId;
        if (_groupNameCache.TryGetValue(groupId, out var cached)) return cached;

        try
        {
            var client = _authBridge.GraphClient;
            if (client != null)
            {
                var response = await client.Groups
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Filter = $"id eq '{groupId}'";
                        req.QueryParameters.Select = ["displayName"];
                        req.QueryParameters.Top = 1;
                    });

                var name = response?.Value?.FirstOrDefault()?.DisplayName ?? groupId;
                _groupNameCache[groupId] = name;
                return name;
            }
        }
        catch { /* Fall back to GUID */ }

        _groupNameCache[groupId] = groupId;
        return groupId;
    }

    private static string FormatAppType(MobileApp app)
    {
        return app switch
        {
            Win32LobApp => "Win32",
            WindowsMobileMSI => "MSI",
            WebApp => "Web Link",
            MicrosoftStoreForBusinessApp => "Store (Business)",
            WindowsAppX => "AppX",
            WindowsUniversalAppX => "UWP",
            IosVppApp => "iOS VPP",
            IosStoreApp => "iOS Store",
            IosLobApp => "iOS LOB",
            AndroidStoreApp => "Android Store",
            AndroidLobApp => "Android LOB",
            AndroidManagedStoreApp => "Android Managed",
            ManagedIOSStoreApp => "Managed iOS",
            ManagedAndroidStoreApp => "Managed Android",
            ManagedIOSLobApp => "Managed iOS LOB",
            ManagedAndroidLobApp => "Managed Android LOB",
            MacOSLobApp => "macOS LOB",
            MacOSDmgApp => "macOS DMG",
            MacOSMicrosoftDefenderApp => "macOS Defender",
            MacOSMicrosoftEdgeApp => "macOS Edge",
            MacOSOfficeSuiteApp => "macOS Office",
            _ => app.OdataType?.Split('.').LastOrDefault()?.Replace("App", " App") ?? "Unknown"
        };
    }

    private static string DetectPlatform(MobileApp app)
    {
        return app switch
        {
            Win32LobApp or WindowsMobileMSI or WindowsAppX or WindowsUniversalAppX
                or MicrosoftStoreForBusinessApp => "Windows",
            IosVppApp or IosStoreApp or IosLobApp or ManagedIOSStoreApp or ManagedIOSLobApp => "iOS",
            AndroidStoreApp or AndroidLobApp or AndroidManagedStoreApp
                or ManagedAndroidStoreApp or ManagedAndroidLobApp => "Android",
            MacOSLobApp or MacOSDmgApp or MacOSMicrosoftDefenderApp
                or MacOSMicrosoftEdgeApp or MacOSOfficeSuiteApp => "macOS",
            WebApp => "Web",
            _ => "Other"
        };
    }

    private static string? ExtractVersion(MobileApp app) => app switch
    {
        Win32LobApp w => w.DisplayVersion,
        IosVppApp v => v.BundleId is not null ? v.VppTokenAppleId : null,
        _ => null
    };

    private static string? ExtractBundleId(MobileApp app) => app switch
    {
        IosVppApp v => v.BundleId,
        IosStoreApp s => s.BundleId,
        ManagedIOSStoreApp m => m.BundleId,
        _ => null
    };

    private static string? ExtractMinimumOs(MobileApp app) => app switch
    {
        IosVppApp v => v.ApplicableDeviceType?.ToString(),
        _ => null
    };

    private static string? ExtractInstallCommand(MobileApp app) => app switch
    {
        Win32LobApp w => w.InstallCommandLine,
        _ => null
    };

    private static string? ExtractUninstallCommand(MobileApp app) => app switch
    {
        Win32LobApp w => w.UninstallCommandLine,
        _ => null
    };

    private static string? ExtractInstallContext(MobileApp app) => app switch
    {
        Win32LobApp w => w.InstallExperience?.RunAsAccount?.ToString(),
        _ => null
    };

    private static double? ExtractSize(MobileApp app) => app switch
    {
        Win32LobApp w when w.Size is not null => Math.Round((double)w.Size / (1024 * 1024), 1),
        MacOSDmgApp d when d.SizeInByte is not null => Math.Round((double)d.SizeInByte / (1024 * 1024), 1),
        _ => null
    };

    private static string? ExtractStoreUrl(MobileApp app) => app switch
    {
        IosStoreApp s => s.AppStoreUrl,
        AndroidStoreApp a => a.AppStoreUrl,
        _ => null
    };
}
