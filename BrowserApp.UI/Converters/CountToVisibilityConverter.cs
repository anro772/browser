using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts a count to Visibility. Shows when count is 0 (for empty states).
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            var visible = invert ? count > 0 : count == 0;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
