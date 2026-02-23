using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Intune.Commander.Desktop.ViewModels;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Desktop.Converters;

/// <summary>
/// Converts an OData type string (e.g. "#microsoft.graph.win32LobApp")
/// into a friendly type name.
/// </summary>
public class ODataTypeConverter : IValueConverter
{
    public static readonly ODataTypeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string odataType || string.IsNullOrEmpty(odataType))
            return "";

        var name = odataType.Split('.')[^1];
        // Insert spaces before capitals: "win32LobApp" → "Win32 Lob App"
        var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        return char.ToUpper(spaced[0]) + spaced[1..];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts an OData type string into a platform name (Windows, iOS, macOS, Android, Web).
/// </summary>
public class PlatformConverter : IValueConverter
{
    public static readonly PlatformConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return MainWindowViewModel.InferPlatform(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts a List&lt;string&gt; or IList&lt;string&gt; into a comma-separated string.
/// </summary>
public class StringListConverter : IValueConverter
{
    public static readonly StringListConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.Generic.IList<string> list && list.Count > 0)
            return string.Join(", ", list);
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts DateTime/DateTimeOffset (or parseable strings) into a human-readable local date/time.
/// </summary>
public class HumanDateTimeConverter : IValueConverter
{
    public static readonly HumanDateTimeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return "—";

        if (value is DateTimeOffset dto)
            return dto.ToLocalTime().ToString("MMM d, yyyy h:mm tt", culture);

        if (value is DateTime dt)
            return dt.ToLocalTime().ToString("MMM d, yyyy h:mm tt", culture);

        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "—";

            if (DateTimeOffset.TryParse(text, culture, DateTimeStyles.AssumeUniversal, out var parsedDto))
                return parsedDto.ToLocalTime().ToString("MMM d, yyyy h:mm tt", culture);

            if (DateTime.TryParse(text, culture, DateTimeStyles.AssumeLocal, out var parsedDt))
                return parsedDt.ToLocalTime().ToString("MMM d, yyyy h:mm tt", culture);

            return text;
        }

        return value.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts a <see cref="byte[]"/> to a UTF-8 string for display.
/// Graph API returns script content as raw bytes that represent UTF-8 text.
/// </summary>
public class BytesToUtf8Converter : IValueConverter
{
    public static readonly BytesToUtf8Converter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            try { return Encoding.UTF8.GetString(bytes); }
            catch { return "(binary content)"; }
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts bytes (long) to megabytes (decimal string with 2 decimals).
/// </summary>
public class BytesToMegabytesConverter : IValueConverter
{
    public static readonly BytesToMegabytesConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes && bytes > 0)
        {
            var mb = bytes / 1048576.0;
            return $"{mb:F2} MB";
        }
        return "—";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Converts minimum OS version objects (iOS/Android/macOS/Windows) to highest required version string.
/// </summary>
public class MinimumOSVersionConverter : IValueConverter
{
    public static readonly MinimumOSVersionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            IosMinimumOperatingSystem ios => GetHighestIosVersion(ios),
            AndroidMinimumOperatingSystem android => GetHighestAndroidVersion(android),
            MacOSMinimumOperatingSystem macos => GetHighestMacOSVersion(macos),
            WindowsMinimumOperatingSystem windows => GetHighestWindowsVersion(windows),
            _ => ""
        };
    }

    private static string GetHighestIosVersion(IosMinimumOperatingSystem os)
    {
        // Check from highest to lowest
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

    private static string GetHighestAndroidVersion(AndroidMinimumOperatingSystem os)
    {
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

    private static string GetHighestMacOSVersion(MacOSMinimumOperatingSystem os)
    {
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

    private static string GetHighestWindowsVersion(WindowsMinimumOperatingSystem os)
    {
        if (os.V1021H1 == true) return "Windows 10 21H1+";
        if (os.V102H20 == true) return "Windows 10 20H2+";
        if (os.V102004 == true) return "Windows 10 2004+";
        if (os.V101909 == true) return "Windows 10 1909+";
        if (os.V101903 == true) return "Windows 10 1903+";
        if (os.V101809 == true) return "Windows 10 1809+";
        if (os.V101803 == true) return "Windows 10 1803+";
        if (os.V101709 == true) return "Windows 10 1709+";
        if (os.V101703 == true) return "Windows 10 1703+";
        if (os.V101607 == true) return "Windows 10 1607+";
        if (os.V100 == true) return "Windows 10+";
        if (os.V81 == true) return "Windows 8.1+";
        if (os.V80 == true) return "Windows 8.0+";
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Checks if the value is an iOS app type.
/// </summary>
public class IsIosAppConverter : IValueConverter
{
    public static readonly IsIosAppConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IosLobApp or IosStoreApp or IosVppApp;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Checks if the value is an Android app type.
/// </summary>
public class IsAndroidAppConverter : IValueConverter
{
    public static readonly IsAndroidAppConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is AndroidManagedStoreApp or AndroidStoreApp;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Checks if the value is a macOS app type.
/// </summary>
public class IsMacOSAppConverter : IValueConverter
{
    public static readonly IsMacOSAppConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MacOSLobApp or MacOSOfficeSuiteApp or MacOSMicrosoftEdgeApp;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}

/// <summary>
/// Checks if the value is a Win32 app type.
/// </summary>
public class IsWin32AppConverter : IValueConverter
{
    public static readonly IsWin32AppConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Win32LobApp;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
