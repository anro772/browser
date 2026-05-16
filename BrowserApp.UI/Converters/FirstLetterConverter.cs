using System.Globalization;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Returns the uppercase first character of the input string (or "?" if empty).
/// Used by the profile pill / channel avatars to render a 1-char monogram.
/// </summary>
public class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            return char.ToUpperInvariant(s[0]).ToString();
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
