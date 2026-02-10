using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts a hex color string (e.g. "#0078D4") to a SolidColorBrush.
/// </summary>
public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(colorString)!;
                return brush;
            }
            catch
            {
                // Fall through to default
            }
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
