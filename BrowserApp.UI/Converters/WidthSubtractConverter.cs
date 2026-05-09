using System.Globalization;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Subtracts a constant (passed via ConverterParameter, in DIPs) from a numeric width
/// so an element can size itself to "parent.ActualWidth - reservedSpace".
/// Used by the tab strip to cap its scroll viewer to the room left after caption buttons + sticky +.
/// </summary>
public class WidthSubtractConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width || double.IsNaN(width))
            return 0d;

        double reserved = 0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
            reserved = p;
        else if (parameter is double pd)
            reserved = pd;

        var result = width - reserved;
        return result < 0 ? 0d : result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
