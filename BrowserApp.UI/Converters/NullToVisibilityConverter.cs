using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Visibility.Visible when the bound value is non-null and (for strings) non-empty.
/// Pass <c>Invert</c> as ConverterParameter to flip the polarity.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var present = value switch
        {
            null => false,
            string s => !string.IsNullOrEmpty(s),
            _ => true
        };

        var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
        var visible = invert ? !present : present;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
