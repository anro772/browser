using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserApp.UI.Converters;

public class StatusCodeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hex = value switch
        {
            null             => "#F87171",
            int code when code is >= 100 and <= 299 => "#34D399",
            int code when code is >= 300 and <= 399 => "#FBBF24",
            int code when code is >= 400 and <= 499 => "#FB923C",
            _                => "#F87171",
        };
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
