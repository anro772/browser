using System.Globalization;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts a percentage (0-100) to a width value for progress bars.
/// Uses the parent container width as a multiplier base.
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            // Return a relative width that works with percentage-based layouts
            // The actual width will be constrained by the parent container
            return Math.Max(0, Math.Min(100, percent)) / 100.0 * 150; // Base width of 150px
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
