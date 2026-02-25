using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Intune.Commander.Core.Models;

namespace Intune.Commander.Desktop.Converters;

/// <summary>Returns a green or red SolidColorBrush based on AllPermissionsGranted.</summary>
public sealed class PermissionSummaryBrushConverter : IValueConverter
{
    public static readonly PermissionSummaryBrushConverter Instance = new();

    private static readonly SolidColorBrush Green = new(Color.Parse("#1A6B3C"));
    private static readonly SolidColorBrush Red   = new(Color.Parse("#CC3300"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Green : Red;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts a PermissionCheckResult to a summary string like "14 / 15 Granted".</summary>
public sealed class PermissionSummaryTextConverter : IValueConverter
{
    public static readonly PermissionSummaryTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PermissionCheckResult r) return "—";
        return r.AllPermissionsGranted
            ? $"All {r.GrantedPermissions.Count} / {r.RequiredPermissions.Count} Granted"
            : $"{r.GrantedPermissions.Count} / {r.RequiredPermissions.Count} Granted";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns true when an int value is greater than zero (for IsVisible bindings).</summary>
public sealed class CountGreaterThanZeroConverter : IValueConverter
{
    public static readonly CountGreaterThanZeroConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int n && n > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
