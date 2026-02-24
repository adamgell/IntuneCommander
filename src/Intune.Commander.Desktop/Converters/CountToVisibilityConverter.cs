using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Intune.Commander.Desktop.Converters;

/// <summary>
/// Converts a count (int) to a boolean visibility value.
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public static readonly CountToVisibilityConverter ZeroIsVisible = new() { ShowWhenZero = true };
    public static readonly CountToVisibilityConverter NonZeroIsVisible = new() { ShowWhenZero = false };

    public bool ShowWhenZero { get; init; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return ShowWhenZero ? count == 0 : count > 0;
        }
        return ShowWhenZero;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindingOperations.DoNothing;
}
