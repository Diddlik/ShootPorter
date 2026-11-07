using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ShootPorter.App.ViewModels;

/// <summary>
/// Background brush of a settings tab: highlighted when it is the current tab.
/// </summary>
public class TabBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? BrushCache.Get("#eff6ff")
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Text brush of a settings tab: accent when it is the current tab.
/// </summary>
public class TabForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? BrushCache.Get("#2563eb")
                : BrushCache.Get("#475569");
        }
        return BrushCache.Get("#475569");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Border brush of a settings tab: accent when it is the current tab.
/// </summary>
public class TabBorderBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string current && parameter is string tab)
        {
            return current == tab
                ? BrushCache.Get("#2563eb")
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Border brush of a profile card: accent when the profile is active.
/// </summary>
public class ProfileBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? BrushCache.Get("#2563eb")
                : BrushCache.Get("#e2e8f0");
        }
        return BrushCache.Get("#e2e8f0");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Caption of a profile card button: ACTIVE or SELECT.
/// </summary>
public class ProfileButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isActive && isActive ? "ACTIVE" : "SELECT";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Background of a profile card button: accent when the profile is active.
/// </summary>
public class ProfileButtonBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? BrushCache.Get("#2563eb")
                : Brushes.Transparent;
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Text brush of a profile card button: white when the profile is active.
/// </summary>
public class ProfileButtonFgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive
                ? Brushes.White
                : BrushCache.Get("#475569");
        }
        return BrushCache.Get("#475569");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
