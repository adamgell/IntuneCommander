using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Intune.Commander.Core.Models;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Graph.Beta.Models;
using SkiaSharp;

namespace Intune.Commander.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Overview/Dashboard tab.
/// All data is computed from existing loaded collections — no extra Graph calls.
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    // --- Tenant Info ---
    [ObservableProperty]
    private string _tenantName = "";

    [ObservableProperty]
    private string _tenantId = "";

    [ObservableProperty]
    private string _cloudEnvironment = "";

    [ObservableProperty]
    private string _profileName = "";

    // --- Summary counts ---
    [ObservableProperty]
    private int _totalDeviceConfigs;

    [ObservableProperty]
    private int _totalCompliancePolicies;

    [ObservableProperty]
    private int _totalApplications;

    [ObservableProperty]
    private int _totalAppAssignmentRows;

    [ObservableProperty]
    private int _unassignedAppCount;

    [ObservableProperty]
    private int _totalSettingsCatalog;

    [ObservableProperty]
    private int _totalEndpointSecurity;

    [ObservableProperty]
    private int _totalAdministrativeTemplates;

    [ObservableProperty]
    private int _totalConditionalAccess;

    [ObservableProperty]
    private int _totalEnrollmentConfigs;

    [ObservableProperty]
    private int _totalScripts;

    [ObservableProperty]
    private int _totalAppProtection;

    [ObservableProperty]
    private bool _isLoading;

    // --- Charts ---
    [ObservableProperty]
    private ISeries[] _appsByPlatformSeries = [];

    [ObservableProperty]
    private ISeries[] _configsByPlatformSeries = [];

    // --- Recently modified (split by category) ---
    public ObservableCollection<RecentItem> RecentlyModifiedPolicies { get; } = [];
    public ObservableCollection<RecentItem> RecentlyModifiedApps { get; } = [];

    // --- Palette ---
    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#2196F3"), // Blue
        SKColor.Parse("#4CAF50"), // Green
        SKColor.Parse("#FF9800"), // Orange
        SKColor.Parse("#9C27B0"), // Purple
        SKColor.Parse("#F44336"), // Red
        SKColor.Parse("#00BCD4"), // Cyan
        SKColor.Parse("#795548"), // Brown
        SKColor.Parse("#607D8B")  // Blue Grey
    ];

    public void Update(
        TenantProfile? profile,
        IReadOnlyList<DeviceConfiguration> configs,
        IReadOnlyList<DeviceCompliancePolicy> policies,
        IReadOnlyList<MobileApp> apps,
        IReadOnlyList<AppAssignmentRow> assignmentRows,
        int settingsCatalogCount = 0,
        int endpointSecurityCount = 0,
        int administrativeTemplatesCount = 0,
        int conditionalAccessCount = 0,
        int enrollmentConfigsCount = 0,
        int scriptsCount = 0,
        int appProtectionCount = 0)
    {
        // Tenant info
        TenantName = profile?.Name ?? "";
        TenantId = profile?.TenantId ?? "";
        CloudEnvironment = profile?.Cloud.ToString() ?? "";
        ProfileName = profile?.Name ?? "";

        // Summary counts
        TotalDeviceConfigs = configs.Count;
        TotalCompliancePolicies = policies.Count;
        TotalApplications = apps.Count;
        TotalAppAssignmentRows = assignmentRows.Count;
        TotalSettingsCatalog = settingsCatalogCount;
        TotalEndpointSecurity = endpointSecurityCount;
        TotalAdministrativeTemplates = administrativeTemplatesCount;
        TotalConditionalAccess = conditionalAccessCount;
        TotalEnrollmentConfigs = enrollmentConfigsCount;
        TotalScripts = scriptsCount;
        TotalAppProtection = appProtectionCount;

        // Unassigned apps
        var appsWithAssignments = new HashSet<string>(
            assignmentRows
                .Where(r => r.AssignmentType != "None" && !string.IsNullOrEmpty(r.AppId))
                .Select(r => r.AppId));
        UnassignedAppCount = apps.Count(a => !string.IsNullOrEmpty(a.Id) && !appsWithAssignments.Contains(a.Id!));

        // Platform breakdown for apps
        BuildAppsByPlatformChart(apps);

        // Platform breakdown for configs
        BuildConfigsByPlatformChart(configs);

        // Recently modified
        BuildRecentlyModified(configs, policies, apps);
    }

    private void BuildAppsByPlatformChart(IReadOnlyList<MobileApp> apps)
    {
        var groups = apps
            .GroupBy(a => MainWindowViewModel.InferPlatform(a.OdataType))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderByDescending(g => g.Count())
            .ToList();

        var series = new List<ISeries>();
        for (var i = 0; i < groups.Count; i++)
        {
            var key = groups[i].Key;
            var count = groups[i].Count();
            var color = Palette[i % Palette.Length];
            series.Add(new PieSeries<int>
            {
                Values = [count],
                Name = $"{key} ({count})",
                Fill = new SolidColorPaint(color),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => key
            });
        }

        AppsByPlatformSeries = series.ToArray();
    }

    private void BuildConfigsByPlatformChart(IReadOnlyList<DeviceConfiguration> configs)
    {
        var groups = configs
            .GroupBy(c => MainWindowViewModel.InferPlatform(c.OdataType))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderByDescending(g => g.Count())
            .ToList();

        var series = new List<ISeries>();
        for (var i = 0; i < groups.Count; i++)
        {
            var key = groups[i].Key;
            var count = groups[i].Count();
            var color = Palette[i % Palette.Length];
            series.Add(new PieSeries<int>
            {
                Values = [count],
                Name = $"{key} ({count})",
                Fill = new SolidColorPaint(color),
                DataLabelsSize = 12,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = p => key
            });
        }

        ConfigsByPlatformSeries = series.ToArray();
    }

    /// <summary>
    /// Delegate set by MainWindowViewModel to allow card-click navigation.
    /// </summary>
    public Action<string>? NavigateToCategory { get; set; }

    [RelayCommand]
    private void NavigateToDeviceConfigs() => NavigateToCategory?.Invoke("Device Configurations");

    [RelayCommand]
    private void NavigateToCompliancePolicies() => NavigateToCategory?.Invoke("Compliance Policies");

    [RelayCommand]
    private void NavigateToApplications() => NavigateToCategory?.Invoke("Applications");

    [RelayCommand]
    private void NavigateToUnassignedApps() => NavigateToCategory?.Invoke("Applications");

    [RelayCommand]
    private void NavigateToSettingsCatalog() => NavigateToCategory?.Invoke("Settings Catalog");

    [RelayCommand]
    private void NavigateToEndpointSecurity() => NavigateToCategory?.Invoke("Endpoint Security");

    [RelayCommand]
    private void NavigateToAdministrativeTemplates() => NavigateToCategory?.Invoke("Administrative Templates");

    [RelayCommand]
    private void NavigateToConditionalAccess() => NavigateToCategory?.Invoke("Conditional Access");

    [RelayCommand]
    private void NavigateToEnrollmentConfigs() => NavigateToCategory?.Invoke("Enrollment Configurations");

    [RelayCommand]
    private void NavigateToScripts() => NavigateToCategory?.Invoke("Device Management Scripts");

    [RelayCommand]
    private void NavigateToAppProtection() => NavigateToCategory?.Invoke("App Protection Policies");

    private void BuildRecentlyModified(
        IReadOnlyList<DeviceConfiguration> configs,
        IReadOnlyList<DeviceCompliancePolicy> policies,
        IReadOnlyList<MobileApp> apps)
    {
        RecentlyModifiedPolicies.Clear();
        RecentlyModifiedApps.Clear();

        // Policies: device configs + compliance policies
        var policyItems = new List<RecentItem>();
        foreach (var c in configs.Where(x => x.LastModifiedDateTime.HasValue))
            policyItems.Add(new RecentItem { Name = c.DisplayName ?? "(unnamed)", Category = "Device Configuration", Modified = c.LastModifiedDateTime!.Value });
        foreach (var p in policies.Where(x => x.LastModifiedDateTime.HasValue))
            policyItems.Add(new RecentItem { Name = p.DisplayName ?? "(unnamed)", Category = "Compliance Policy", Modified = p.LastModifiedDateTime!.Value });
        foreach (var item in policyItems.OrderByDescending(i => i.Modified).Take(8))
            RecentlyModifiedPolicies.Add(item);

        // Apps
        foreach (var item in apps
            .Where(x => x.LastModifiedDateTime.HasValue)
            .OrderByDescending(x => x.LastModifiedDateTime!.Value)
            .Take(8)
            .Select(a => new RecentItem { Name = a.DisplayName ?? "(unnamed)", Category = "Application", Modified = a.LastModifiedDateTime!.Value }))
            RecentlyModifiedApps.Add(item);
    }
}

/// <summary>
/// Display model for the Recently Modified list.
/// </summary>
public class RecentItem
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required DateTimeOffset Modified { get; init; }
    public string ModifiedText => Modified.LocalDateTime.ToString("g");
}
