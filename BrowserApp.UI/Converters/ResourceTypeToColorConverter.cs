using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserApp.UI.Converters;

public class ResourceTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value as string ?? string.Empty;
        var hex = type.ToLowerInvariant() switch
        {
            "script"     => "#FBBF24",
            "stylesheet" => "#A78BFA",
            "image"      => "#60A5FA",
            "xhr"        => "#22D3EE",
            "fetch"      => "#22D3EE",
            "document"   => "#34D399",
            _            => "#738099",
        };
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
