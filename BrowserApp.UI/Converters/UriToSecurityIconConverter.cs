using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Converts a URL string to a security icon (lock for HTTPS, warning for HTTP).
/// </summary>
public class UriToSecurityIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return SymbolRegular.LockClosed24;
            }
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return SymbolRegular.Warning24;
            }
        }
        return SymbolRegular.Globe24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a URL string to a security color (green for HTTPS, orange for HTTP).
/// </summary>
public class UriToSecurityColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)); // Green
            }
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 140, 0)); // Orange
            }
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 120)); // Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
