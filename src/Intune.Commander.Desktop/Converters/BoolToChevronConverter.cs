using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Intune.Commander.Desktop.Converters;

/// <summary>
/// Converts a boolean (IsExpanded) to a chevron character: ▾ when expanded, ▸ when collapsed.
/// </summary>
public class BoolToChevronConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "▾" : "▸";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
