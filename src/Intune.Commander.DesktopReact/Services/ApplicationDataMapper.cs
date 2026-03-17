using System.Globalization;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.DesktopReact.Services;

internal static class ApplicationDataMapper
{
    public static string FormatAppType(MobileApp app)
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

    public static string DetectPlatform(MobileApp app)
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

    public static string ExtractVersion(MobileApp? app)
    {
        if (app is null) return "";

        return app switch
        {
            Win32LobApp w => TryGetAdditionalString(w, "displayVersion")
                             ?? w.DisplayVersion
                             ?? w.MsiInformation?.ProductVersion
                             ?? "",
            MacOSLobApp m => m.VersionNumber ?? "",
            MacOSDmgApp d => d.PrimaryBundleVersion ?? "",
            IosLobApp i => i.VersionNumber ?? "",
            _ => ""
        };
    }

    public static string ExtractVersion(ManagedAppPolicy? policy) => policy?.Version ?? "";

    public static string ExtractVersion(ManagedDeviceMobileAppConfiguration? configuration) =>
        configuration?.Version?.ToString(CultureInfo.InvariantCulture) ?? "";

    public static string ExtractVersion(TargetedManagedAppConfiguration? configuration) =>
        configuration?.Version?.ToString(CultureInfo.InvariantCulture) ?? "";

    public static string ExtractBundleId(MobileApp? app)
    {
        if (app is null) return "";

        return app switch
        {
            IosLobApp i => i.BundleId ?? "",
            IosStoreApp s => s.BundleId ?? "",
            IosVppApp v => v.BundleId ?? "",
            ManagedIOSStoreApp managed => managed.BundleId ?? "",
            MacOSLobApp m => m.BundleId ?? "",
            MacOSDmgApp d => d.PrimaryBundleId ?? "",
            _ => ""
        };
    }

    public static string ExtractPackageId(MobileApp? app)
    {
        return app switch
        {
            AndroidStoreApp a => a.PackageId ?? "",
            _ => ""
        };
    }

    public static string ExtractAppStoreUrl(MobileApp? app)
    {
        return app switch
        {
            IosStoreApp i => i.AppStoreUrl ?? "",
            AndroidStoreApp a => a.AppStoreUrl ?? "",
            WebApp w => w.AppUrl ?? "",
            _ => ""
        };
    }

    public static string ExtractInformationUrl(MobileApp? app) => app?.InformationUrl ?? "";
    public static string ExtractPrivacyUrl(MobileApp? app) => app?.PrivacyInformationUrl ?? "";
    public static string ExtractOwner(MobileApp? app) => app?.Owner ?? "";
    public static string ExtractDeveloper(MobileApp? app) => app?.Developer ?? "";
    public static string ExtractPublisher(MobileApp? app) => app?.Publisher ?? "";
    public static string ExtractNotes(MobileApp? app) => app?.Notes ?? "";

    public static string ExtractMinimumOS(MobileApp? app)
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

    public static string ExtractInstallCommand(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.InstallCommandLine ?? "",
            _ => ""
        };
    }

    public static string ExtractUninstallCommand(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.UninstallCommandLine ?? "",
            _ => ""
        };
    }

    public static string ExtractInstallContext(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w => w.InstallExperience?.RunAsAccount?.ToString() ?? "",
            _ => ""
        };
    }

    public static double? ExtractSizeInMB(MobileApp? app)
    {
        if (app == null) return null;

        var size = app switch
        {
            Win32LobApp w => w.Size,
            IosLobApp i => i.Size,
            MacOSLobApp m => m.Size,
            _ => null
        };

        return size is null ? null : Math.Round(size.Value / 1048576.0, 1);
    }

    public static string[] ExtractCategories(MobileApp? app)
    {
        if (app?.Categories == null || app.Categories.Count == 0)
            return [];

        return app.Categories
            .Where(c => !string.IsNullOrWhiteSpace(c.DisplayName))
            .Select(c => c.DisplayName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string ExtractCategoriesText(MobileApp? app) => string.Join(", ", ExtractCategories(app));

    public static int ExtractSupersededCount(MobileApp? app)
    {
        if (app?.AdditionalData == null) return 0;

        if (app.AdditionalData.TryGetValue("supersededAppCount", out var value))
        {
            if (value is int intCount) return intCount;
            if (int.TryParse(value?.ToString(), out var parsedCount)) return parsedCount;
        }

        return 0;
    }

    public static string ExtractMinimumFreeDiskSpace(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumFreeDiskSpaceInMB.HasValue =>
                w.MinimumFreeDiskSpaceInMB.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    public static string ExtractMinimumMemory(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumMemoryInMB.HasValue =>
                w.MinimumMemoryInMB.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    public static string ExtractMinimumProcessors(MobileApp? app)
    {
        return app switch
        {
            Win32LobApp w when w.MinimumNumberOfProcessors.HasValue =>
                w.MinimumNumberOfProcessors.Value.ToString(CultureInfo.InvariantCulture),
            _ => ""
        };
    }

    public static string FormatAssignmentSettings(MobileAppAssignmentSettings? settings)
    {
        if (settings is Win32LobAppAssignmentSettings win32Settings)
        {
            var parts = new List<string>();
            if (win32Settings.Notifications.HasValue)
                parts.Add($"Notifications: {win32Settings.Notifications.Value.ToString().ToLowerInvariant()}");
            if (win32Settings.InstallTimeSettings != null)
                parts.Add("Install Time: configured");
            if (win32Settings.DeliveryOptimizationPriority.HasValue)
            {
                parts.Add($"Delivery Priority: {win32Settings.DeliveryOptimizationPriority.Value.ToString().ToLowerInvariant()}");
            }

            return parts.Count > 0 ? string.Join("; ", parts) : "N/A";
        }

        return "N/A";
    }

    public static string BuildAssignmentRowId(
        MobileApp app,
        MobileAppAssignment? assignment,
        string targetName,
        string targetGroupId,
        bool isExclusion)
    {
        var intent = assignment?.Intent?.ToString() ?? "none";
        var target = string.IsNullOrWhiteSpace(targetGroupId) ? targetName : targetGroupId;
        return $"{app.Id ?? "unknown"}::{intent}::{target}::{isExclusion}";
    }

    public static string FormatTypeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        var text = value.StartsWith("#microsoft.graph.", StringComparison.OrdinalIgnoreCase)
            ? value["#microsoft.graph.".Length..]
            : value;

        var chars = new List<char>(text.Length + 8);
        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(text[i - 1]))
                chars.Add(' ');
            chars.Add(current);
        }

        return string.Join(' ',
            new string(chars.ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Length > 0
                    ? char.ToUpperInvariant(segment[0]) + segment[1..]
                    : segment));
    }

    private static string? TryGetAdditionalString(MobileApp app, string key)
    {
        if (app.AdditionalData?.TryGetValue(key, out var value) == true)
            return value?.ToString();

        return null;
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
        return string.IsNullOrWhiteSpace(minRelease) ? "" : $"Windows {minRelease}+";
    }
}
