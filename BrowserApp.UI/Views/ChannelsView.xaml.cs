using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for ChannelsView.xaml
/// </summary>
public partial class ChannelsView : FluentWindow
{
    public ChannelsView()
    {
        InitializeComponent();

        // Add converters to resources
        Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());
        Resources.Add("CountToVisibilityConverter", new CountToVisibilityConverter());
    }

    public ChannelsView(ChannelsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void ChannelsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChannelsViewModel viewModel)
        {
            await viewModel.LoadChannelsCommand.ExecuteAsync(null);
        }
    }
}

/// <summary>
/// Converts bool to Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts count to visibility (visible if count is 0).
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
