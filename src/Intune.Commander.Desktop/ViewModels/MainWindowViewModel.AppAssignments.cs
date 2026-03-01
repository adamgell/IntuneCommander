using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Globalization;

using System.Linq;

using System.Text.Json;

using System.Threading;

using System.Threading.Tasks;

using Avalonia.Threading;

using Intune.Commander.Core.Services;

using Microsoft.Graph.Beta.Models;



namespace Intune.Commander.Desktop.ViewModels;



public partial class MainWindowViewModel : ViewModelBase

{



    // --- Application Assignments flattened view ---



    private async Task LoadAppAssignmentRowsAsync()

    {

        if (_applicationService == null || _graphClient == null) return;



        IsBusy = true;

        IsLoadingDetails = true;

        Overview.IsLoading = true;

        StatusText = "Loading application assignments...";



        try

        {

            // Reuse existing apps list if available, otherwise fetch

            var apps = Applications.Count > 0

                ? Applications.ToList()

                : await _applicationService.ListApplicationsAsync();



            var rows = new List<AppAssignmentRow>();

            var total = apps.Count;

            var processed = 0;



            // Use a semaphore to limit concurrent Graph API calls

            using var semaphore = new SemaphoreSlim(5, 5);

            var tasks = apps.Select(async app =>

            {

                await semaphore.WaitAsync();

                try

                {

                    var assignments = app.Id != null

                        ? await _applicationService.GetAssignmentsAsync(app.Id)

                        : [];



                    var appRows = new List<AppAssignmentRow>();

                    foreach (var assignment in assignments)

                    {

                        appRows.Add(await BuildAppAssignmentRowAsync(app, assignment));

                    }



                    // If app has no assignments, still include it with empty assignment fields

                    if (assignments.Count == 0)

                    {

                        appRows.Add(BuildAppRowNoAssignment(app));

                    }



                    var currentProcessed = Interlocked.Increment(ref processed);

                    lock (rows)

                    {

                        rows.AddRange(appRows);

                    }



                    // Update status on UI thread periodically

                    if (currentProcessed % 10 == 0 || currentProcessed == total)

                    {

                        var currentTotal = total;

                        Dispatcher.UIThread.Post(() =>

                            StatusText = $"Loading assignments... {currentProcessed}/{currentTotal} apps");

                    }

                }

                finally

                {

                    semaphore.Release();

                }

            }).ToList();



            await Task.WhenAll(tasks);



            // Sort by app name, then target name

            rows.Sort((a, b) =>

            {

                var cmp = string.Compare(a.AppName, b.AppName, StringComparison.OrdinalIgnoreCase);

                return cmp != 0 ? cmp : string.Compare(a.TargetName, b.TargetName, StringComparison.OrdinalIgnoreCase);

            });



            AppAssignmentRows = new ObservableCollection<AppAssignmentRow>(rows);

            _appAssignmentsLoaded = true;

            ApplyFilter();



            // Save to cache

            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAppAssignments, rows);

                DebugLog.Log("Cache", $"Saved {rows.Count} app assignment row(s) to cache");

            }



            // Update Overview dashboard now that all data is ready

            Overview.Update(

                ActiveProfile,

                (IReadOnlyList<DeviceConfiguration>)DeviceConfigurations,

                (IReadOnlyList<DeviceCompliancePolicy>)CompliancePolicies,

                (IReadOnlyList<MobileApp>)Applications,

                (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows,

                SettingsCatalogPolicies.Count,

                EndpointSecurityIntents.Count,

                AdministrativeTemplates.Count,

                ConditionalAccessPolicies.Count,

                EnrollmentConfigurations.Count,

                DeviceManagementScripts.Count + DeviceShellScripts.Count,

                AppProtectionPolicies.Count);



            StatusText = $"Loaded {rows.Count} application assignments row(s) from {total} apps";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load Application Assignments: {FormatGraphError(ex)}");

            StatusText = "Error loading Application Assignments";

        }

        finally

        {

            IsBusy = false;

            IsLoadingDetails = false;

            Overview.IsLoading = false;

        }

    }



    private async Task<AppAssignmentRow> BuildAppAssignmentRowAsync(MobileApp app, MobileAppAssignment assignment)

    {

        var (assignmentType, targetName, targetGroupId, isExclusion) =

            await ResolveAssignmentTargetAsync(assignment.Target);



        return new AppAssignmentRow

        {

            AppId = app.Id ?? "",

            AppName = app.DisplayName ?? "",

            Publisher = app.Publisher ?? "",

            Description = app.Description ?? "",

            AppType = ExtractShortTypeName(app.OdataType),

            Version = ExtractVersion(app),

            Platform = InferPlatform(app.OdataType),

            BundleId = ExtractBundleId(app),

            PackageId = ExtractPackageId(app),

            IsFeatured = app.IsFeatured == true ? "True" : "False",

            CreatedDate = app.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",

            LastModified = app.LastModifiedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",

            AssignmentType = assignmentType,

            TargetName = targetName,

            TargetGroupId = targetGroupId,

            InstallIntent = assignment.Intent?.ToString()?.ToLowerInvariant() ?? "",

            AssignmentSettings = FormatAssignmentSettings(assignment.Settings),

            IsExclusion = isExclusion,

            AppStoreUrl = ExtractAppStoreUrl(app),

            PrivacyUrl = app.PrivacyInformationUrl ?? "",

            InformationUrl = app.InformationUrl ?? "",

            MinimumOsVersion = ExtractMinOsVersion(app),

            MinimumFreeDiskSpaceMB = ExtractMinDiskSpace(app),

            MinimumMemoryMB = ExtractMinMemory(app),

            MinimumProcessors = ExtractMinProcessors(app),

            Categories = app.Categories != null

                ? string.Join(", ", app.Categories.Select(c => c.DisplayName ?? ""))

                : "",

            Notes = app.Notes ?? ""

        };

    }



    private AppAssignmentRow BuildAppRowNoAssignment(MobileApp app)

    {

        return new AppAssignmentRow

        {

            AppId = app.Id ?? "",

            AppName = app.DisplayName ?? "",

            Publisher = app.Publisher ?? "",

            Description = app.Description ?? "",

            AppType = ExtractShortTypeName(app.OdataType),

            Version = ExtractVersion(app),

            Platform = InferPlatform(app.OdataType),

            BundleId = ExtractBundleId(app),

            PackageId = ExtractPackageId(app),

            IsFeatured = app.IsFeatured == true ? "True" : "False",

            CreatedDate = app.CreatedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",

            LastModified = app.LastModifiedDateTime?.ToString("g", CultureInfo.InvariantCulture) ?? "",

            AssignmentType = "None",

            TargetName = "",

            TargetGroupId = "",

            InstallIntent = "",

            AssignmentSettings = "",

            IsExclusion = "False",

            AppStoreUrl = ExtractAppStoreUrl(app),

            PrivacyUrl = app.PrivacyInformationUrl ?? "",

            InformationUrl = app.InformationUrl ?? "",

            MinimumOsVersion = ExtractMinOsVersion(app),

            MinimumFreeDiskSpaceMB = ExtractMinDiskSpace(app),

            MinimumMemoryMB = ExtractMinMemory(app),

            MinimumProcessors = ExtractMinProcessors(app),

            Categories = app.Categories != null

                ? string.Join(", ", app.Categories.Select(c => c.DisplayName ?? ""))

                : "",

            Notes = app.Notes ?? ""

        };

    }



    private async Task<(string Type, string Name, string GroupId, string IsExclusion)>

        ResolveAssignmentTargetAsync(DeviceAndAppManagementAssignmentTarget? target)

    {

        return target switch

        {

            AllDevicesAssignmentTarget => ("All Devices", "All Devices", "", "False"),

            AllLicensedUsersAssignmentTarget => ("All Users", "All Users", "", "False"),

            ExclusionGroupAssignmentTarget excl =>

                ("Group", await ResolveGroupNameAsync(excl.GroupId), excl.GroupId ?? "", "True"),

            GroupAssignmentTarget grp =>

                ("Group", await ResolveGroupNameAsync(grp.GroupId), grp.GroupId ?? "", "False"),

            _ => ("Unknown", "Unknown", "", "False")

        };

    }



    // --- Type-specific field extractors ---



    private static string? TryGetAdditionalString(MobileApp app, string key)

    {

        if (app.AdditionalData?.TryGetValue(key, out var val) == true)

            return val?.ToString();

        return null;

    }



    private static string ExtractShortTypeName(string? odataType)

    {

        if (string.IsNullOrEmpty(odataType)) return "";

        // "#microsoft.graph.win32LobApp" → "win32LobApp"

        return odataType.Split('.').LastOrDefault() ?? odataType;

    }



    private static string ExtractVersion(MobileApp app)

    {

        return app switch

        {

            Win32LobApp w => TryGetAdditionalString(w, "displayVersion")

                             ?? w.MsiInformation?.ProductVersion ?? "",

            MacOSLobApp m => m.VersionNumber ?? "",

            MacOSDmgApp d => d.PrimaryBundleVersion ?? "",

            IosLobApp i => i.VersionNumber ?? "",

            _ => ""

        };

    }



    private static string ExtractBundleId(MobileApp app)

    {

        return app switch

        {

            IosLobApp i => i.BundleId ?? "",

            IosStoreApp s => s.BundleId ?? "",

            IosVppApp v => v.BundleId ?? "",

            MacOSLobApp m => m.BundleId ?? "",

            MacOSDmgApp d => d.PrimaryBundleId ?? "",

            _ => ""

        };

    }



    private static string ExtractPackageId(MobileApp app)

    {

        return app switch

        {

            AndroidStoreApp a => a.PackageId ?? "",

            _ => ""

        };

    }



    private static string ExtractAppStoreUrl(MobileApp app)

    {

        return app switch

        {

            IosStoreApp i => i.AppStoreUrl ?? "",

            AndroidStoreApp a => a.AppStoreUrl ?? "",

            WebApp w => w.AppUrl ?? "",

            _ => ""

        };

    }



    private static string ExtractMinOsVersion(MobileApp app)

    {

        return app switch

        {

            Win32LobApp w => w.MinimumSupportedWindowsRelease ?? "",

            _ => ""

        };

    }



    private static string ExtractMinDiskSpace(MobileApp app)

    {

        return app switch

        {

            Win32LobApp w when w.MinimumFreeDiskSpaceInMB.HasValue =>

                w.MinimumFreeDiskSpaceInMB.Value.ToString(CultureInfo.InvariantCulture),

            _ => ""

        };

    }



    private static string ExtractMinMemory(MobileApp app)

    {

        return app switch

        {

            Win32LobApp w when w.MinimumMemoryInMB.HasValue =>

                w.MinimumMemoryInMB.Value.ToString(CultureInfo.InvariantCulture),

            _ => ""

        };

    }



    private static string ExtractMinProcessors(MobileApp app)

    {

        return app switch

        {

            Win32LobApp w when w.MinimumNumberOfProcessors.HasValue =>

                w.MinimumNumberOfProcessors.Value.ToString(CultureInfo.InvariantCulture),

            _ => ""

        };

    }






    private static string ExtractMinimumOS(MobileApp? app)
    {
        if (app == null) return "";

        return app switch
        {
            IosLobApp ios => FormatIosMinVersion(ios.MinimumSupportedOperatingSystem),
            IosStoreApp iosStore => FormatIosMinVersion(iosStore.MinimumSupportedOperatingSystem),
            MacOSLobApp mac => FormatMacOSMinVersion(mac.MinimumSupportedOperatingSystem),
            MacOSDmgApp macDmg => FormatMacOSMinVersion(macDmg.MinimumSupportedOperatingSystem),
            AndroidStoreApp androidStore => FormatAndroidMinVersion(androidStore.MinimumSupportedOperatingSystem),
            Win32LobApp win32 => FormatWindowsMinVersion(win32.MinimumSupportedWindowsRelease),
            _ => ""
        };
    }

    private static string FormatIosMinVersion(IosMinimumOperatingSystem? os)
    {
        if (os == null) return "";
        if (os.V180 == true) return "iOS 18.0+";
        if (os.V170 == true) return "iOS 17.0+";
        if (os.V160 == true) return "iOS 16.0+";
        if (os.V150 == true) return "iOS 15.0+";
        if (os.V140 == true) return "iOS 14.0+";
        if (os.V130 == true) return "iOS 13.0+";
        if (os.V120 == true) return "iOS 12.0+";
        if (os.V110 == true) return "iOS 11.0+";
        if (os.V100 == true) return "iOS 10.0+";
        if (os.V90 == true) return "iOS 9.0+";
        if (os.V80 == true) return "iOS 8.0+";
        return "";
    }

    private static string FormatMacOSMinVersion(MacOSMinimumOperatingSystem? os)
    {
        if (os == null) return "";
        if (os.V150 == true) return "macOS 15.0+";
        if (os.V140 == true) return "macOS 14.0+";
        if (os.V130 == true) return "macOS 13.0+";
        if (os.V120 == true) return "macOS 12.0+";
        if (os.V110 == true) return "macOS 11.0+";
        if (os.V1015 == true) return "macOS 10.15+";
        if (os.V1014 == true) return "macOS 10.14+";
        if (os.V1013 == true) return "macOS 10.13+";
        if (os.V1012 == true) return "macOS 10.12+";
        if (os.V1011 == true) return "macOS 10.11+";
        if (os.V1010 == true) return "macOS 10.10+";
        if (os.V109 == true) return "macOS 10.9+";
        if (os.V108 == true) return "macOS 10.8+";
        if (os.V107 == true) return "macOS 10.7+";
        return "";
    }

    private static string FormatAndroidMinVersion(AndroidMinimumOperatingSystem? os)
    {
        if (os == null) return "";
        if (os.V150 == true) return "Android 15.0+";
        if (os.V140 == true) return "Android 14.0+";
        if (os.V130 == true) return "Android 13.0+";
        if (os.V120 == true) return "Android 12.0+";
        if (os.V110 == true) return "Android 11.0+";
        if (os.V100 == true) return "Android 10.0+";
        if (os.V90 == true) return "Android 9.0+";
        if (os.V81 == true) return "Android 8.1+";
        if (os.V80 == true) return "Android 8.0+";
        if (os.V71 == true) return "Android 7.1+";
        if (os.V70 == true) return "Android 7.0+";
        if (os.V60 == true) return "Android 6.0+";
        if (os.V51 == true) return "Android 5.1+";
        if (os.V50 == true) return "Android 5.0+";
        if (os.V44 == true) return "Android 4.4+";
        if (os.V43 == true) return "Android 4.3+";
        if (os.V42 == true) return "Android 4.2+";
        if (os.V41 == true) return "Android 4.1+";
        if (os.V403 == true) return "Android 4.0.3+";
        if (os.V40 == true) return "Android 4.0+";
        return "";
    }

    private static string FormatWindowsMinVersion(string? minRelease)
    {
        if (string.IsNullOrEmpty(minRelease)) return "";
        return $"Windows {minRelease}+";
    }

    private static string ExtractInstallCommand(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.InstallCommandLine ?? "",
            _ => ""
        };
    }

    private static string ExtractUninstallCommand(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.UninstallCommandLine ?? "",
            _ => ""
        };
    }

    private static string ExtractInstallContext(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.InstallExperience?.RunAsAccount?.ToString() ?? "",
            _ => ""
        };
    }

    private static double ExtractSizeInMB(MobileApp? app)
    {
        if (app == null) return 0;
        
        var size = app switch
        {
            Win32LobApp w => w.Size,
            IosLobApp i => i.Size,
            MacOSLobApp m => m.Size,
            _ => null
        };
        
        if (size == null) return 0;
        return size.Value / 1048576.0;
    }

    private static ObservableCollection<string> ExtractCategories(MobileApp? app)
    {
        if (app?.Categories == null || app.Categories.Count == 0)
            return [];

        var categories = app.Categories
            .Where(c => !string.IsNullOrEmpty(c.DisplayName))
            .Select(c => c.DisplayName!)
            .ToList();

        return new ObservableCollection<string>(categories);
    }

    private static int ExtractSupersededCount(MobileApp? app)
    {
        if (app?.AdditionalData == null) return 0;
        
        if (app.AdditionalData.TryGetValue("supersededAppCount", out var val))
        {
            if (val is int count) return count;
            if (int.TryParse(val?.ToString(), out var parsed)) return parsed;
        }
        
        return 0;
    }

    // --- Dynamic Groups view ---



    private async Task LoadDynamicGroupRowsAsync()

    {

        if (_groupService == null) return;



        IsBusy = true;

        StatusText = "Loading dynamic groups...";



        try

        {

            var groups = await _groupService.ListDynamicGroupsAsync();

            var rows = new List<GroupRow>();

            var total = groups.Count;

            var processed = 0;



            using var semaphore = new SemaphoreSlim(5, 5);

            var tasks = groups.Select(async group =>

            {

                await semaphore.WaitAsync();

                try

                {

                    var counts = group.Id != null

                        ? await _groupService.GetMemberCountsAsync(group.Id)

                        : new GroupMemberCounts(0, 0, 0, 0);



                    var row = BuildGroupRow(group, counts);



                    var currentProcessed = Interlocked.Increment(ref processed);

                    lock (rows)

                    {

                        rows.Add(row);

                    }



                    if (currentProcessed % 10 == 0 || currentProcessed == total)

                    {

                        Dispatcher.UIThread.Post(() =>

                            StatusText = $"Loading dynamic groups... {currentProcessed}/{total}");

                    }

                }

                finally { semaphore.Release(); }

            }).ToList();



            await Task.WhenAll(tasks);



            rows.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase));



            DynamicGroupRows = new ObservableCollection<GroupRow>(rows);

            _dynamicGroupsLoaded = true;

            ApplyFilter();



            // Save to cache

            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyDynamicGroups, rows);

                DebugLog.Log("Cache", $"Saved {rows.Count} dynamic group row(s) to cache");

            }



            StatusText = $"Loaded {rows.Count} dynamic group(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load dynamic groups: {FormatGraphError(ex)}");

            StatusText = "Error loading dynamic groups";

        }

        finally

        {

            IsBusy = false;

        }

    }



    // --- Assigned Groups view ---



    private async Task LoadAssignedGroupRowsAsync()

    {

        if (_groupService == null) return;



        IsBusy = true;

        StatusText = "Loading assigned groups...";



        try

        {

            var groups = await _groupService.ListAssignedGroupsAsync();

            var rows = new List<GroupRow>();

            var total = groups.Count;

            var processed = 0;



            using var semaphore = new SemaphoreSlim(5, 5);

            var tasks = groups.Select(async group =>

            {

                await semaphore.WaitAsync();

                try

                {

                    var counts = group.Id != null

                        ? await _groupService.GetMemberCountsAsync(group.Id)

                        : new GroupMemberCounts(0, 0, 0, 0);



                    var row = BuildGroupRow(group, counts);



                    var currentProcessed = Interlocked.Increment(ref processed);

                    lock (rows)

                    {

                        rows.Add(row);

                    }



                    if (currentProcessed % 10 == 0 || currentProcessed == total)

                    {

                        Dispatcher.UIThread.Post(() =>

                            StatusText = $"Loading assigned groups... {currentProcessed}/{total}");

                    }

                }

                finally { semaphore.Release(); }

            }).ToList();



            await Task.WhenAll(tasks);



            rows.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, StringComparison.OrdinalIgnoreCase));



            AssignedGroupRows = new ObservableCollection<GroupRow>(rows);

            _assignedGroupsLoaded = true;

            ApplyFilter();



            // Save to cache

            if (ActiveProfile?.TenantId != null)

            {

                _cacheService.Set(ActiveProfile.TenantId, CacheKeyAssignedGroups, rows);

                DebugLog.Log("Cache", $"Saved {rows.Count} assigned group row(s) to cache");

            }



            StatusText = $"Loaded {rows.Count} assigned group(s)";

        }

        catch (Exception ex)

        {

            SetError($"Failed to load assigned groups: {FormatGraphError(ex)}");

            StatusText = "Error loading assigned groups";

        }

        finally

        {

            IsBusy = false;

        }

    }

}

