using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Globalization;

using System.Linq;

using System.Text.Json;

using System.Threading;

using System.Threading.Tasks;

using Avalonia.Threading;

using IntuneManager.Core.Services;

using Microsoft.Graph.Beta.Models;



namespace IntuneManager.Desktop.ViewModels;



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

                (IReadOnlyList<AppAssignmentRow>)AppAssignmentRows);



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

