using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts enum values to Visibility by matching ConverterParameter.
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return Visibility.Collapsed;
        }

        string parameterValue = parameter.ToString() ?? string.Empty;
        bool isMatch = value.ToString()?.Equals(parameterValue, StringComparison.OrdinalIgnoreCase) == true;
        return isMatch ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
