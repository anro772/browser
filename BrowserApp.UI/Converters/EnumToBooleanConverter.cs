using System.Globalization;
using System.Windows.Data;

namespace BrowserApp.UI.Converters;

/// <summary>
/// Two-way converter for enum-backed RadioButton IsChecked bindings.
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        string parameterValue = parameter.ToString() ?? string.Empty;
        return value.ToString()?.Equals(parameterValue, StringComparison.OrdinalIgnoreCase) == true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked || parameter == null)
        {
            return Binding.DoNothing;
        }

        string parameterValue = parameter.ToString() ?? string.Empty;
        return Enum.Parse(targetType, parameterValue, ignoreCase: true);
    }
}
