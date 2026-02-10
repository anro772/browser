using System.Globalization;
using System.Windows.Data;
using BrowserApp.UI.Models;
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

/// <summary>
/// Converts a CertificateStatus to a security icon, overriding the URL-based icon when cert errors exist.
/// </summary>
public class CertificateStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CertificateStatus status)
        {
            return status switch
            {
                CertificateStatus.Error => SymbolRegular.ShieldError24,
                CertificateStatus.Warning => SymbolRegular.Warning24,
                CertificateStatus.Valid => SymbolRegular.LockClosed24,
                _ => SymbolRegular.Globe24,
            };
        }
        return SymbolRegular.Globe24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a CertificateStatus to a security color.
/// </summary>
public class CertificateStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CertificateStatus status)
        {
            return status switch
            {
                CertificateStatus.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 52, 56)), // Red
                CertificateStatus.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 140, 0)), // Orange
                CertificateStatus.Valid => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 124, 16)), // Green
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 120)), // Gray
            };
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 120));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
