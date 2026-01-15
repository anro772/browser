using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for RuleManagerView.xaml
/// </summary>
public partial class RuleManagerView : Wpf.Ui.Controls.FluentWindow
{
    public RuleManagerView(RuleManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Add converters to resources
        Resources.Add("BoolToVisibilityConverter", new BooleanToVisibilityConverter());
        Resources.Add("InverseBoolConverter", new InverseBoolConverter());

        // Load rules when window opens
        Loaded += async (s, e) => await viewModel.LoadRulesCommand.ExecuteAsync(null);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Converts boolean to inverse boolean.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
