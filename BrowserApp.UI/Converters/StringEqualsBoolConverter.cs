using System.Globalization;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Returns true when both bound values are equal strings (case-insensitive).
/// Used by Marketplace tag chips to drive an active visual state when the chip's
/// tag matches <c>MarketplaceViewModel.SelectedTag</c>.
/// </summary>
public class StringEqualsBoolConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return false;
        var a = values[0]?.ToString();
        var b = values[1]?.ToString();
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
