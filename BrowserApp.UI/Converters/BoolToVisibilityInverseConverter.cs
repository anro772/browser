using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts a boolean to Visibility, where true = Collapsed and false = Visible (inverse of standard).
/// </summary>
public class BoolToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return false;
    }
}
