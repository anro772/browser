using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class DownloadManagerView : UserControl
{
    public DownloadManagerView(DownloadManagerViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DownloadManagerViewModel vm)
        {
            await vm.LoadDownloadsCommand.ExecuteAsync(null);
        }
    }
}
