using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Visibility.Visible when the bound string equals (case-insensitive) any of the
/// comma-separated tokens passed via ConverterParameter. Used by the source-attribution
/// cell to show one of three visuals (local dot / marketplace bookmark / channel stripe).
/// </summary>
public class StringEqualsVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        var allowed = parameter?.ToString();
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(allowed))
            return Visibility.Collapsed;

        // Use '|' as the delimiter — commas conflict with XAML markup-extension parsing
        // (you'd otherwise have to wrap the parameter in single quotes everywhere).
        foreach (var token in allowed.Split('|'))
        {
            if (string.Equals(s, token.Trim(), StringComparison.OrdinalIgnoreCase))
                return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
