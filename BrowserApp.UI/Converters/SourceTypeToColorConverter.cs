using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BrowserApp.UI.Converters;

public class SourceTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value?.ToString()?.ToLowerInvariant()) switch
        {
            "local"       => new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)), // blue
            "template"    => new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)), // purple
            "marketplace" => new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)), // green
            "channel"     => new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE)), // cyan
            _             => new SolidColorBrush(Color.FromRgb(0x73, 0x80, 0x99))  // gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
