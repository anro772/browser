using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Maps a PrivacyMode to a brush sampled from the theme dictionary
/// (Relaxed=yellow, Standard=teal, Strict=violet).
/// </summary>
public class PrivacyModeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var resourceKey = value switch
        {
            PrivacyMode.Relaxed => "PrivacyRelaxedBrush",
            PrivacyMode.Strict => "PrivacyStrictBrush",
            _ => "PrivacyStandardBrush",
        };

        if (Application.Current?.TryFindResource(resourceKey) is Brush b)
            return b;

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Lowercased label used in the profile pill (matches the design's "Standard" label).
/// </summary>
public class PrivacyModeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            PrivacyMode.Relaxed => "Relaxed",
            PrivacyMode.Strict => "Strict",
            _ => "Standard",
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
