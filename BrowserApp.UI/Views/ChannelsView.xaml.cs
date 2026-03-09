using System.Windows;
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
