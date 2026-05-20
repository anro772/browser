using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BrowserApp.UI.Models;
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

    /// <summary>
    /// Single-click on a row opens the file. Clicks landing on the per-row folder button
    /// are ignored so its own Command can fire.
    /// </summary>
    private void DownloadRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject origin && IsDescendantOfFolderButton(origin))
            return;

        if (sender is FrameworkElement fe
            && fe.DataContext is DownloadItemModel item
            && DataContext is DownloadManagerViewModel vm
            && vm.OpenFileCommand.CanExecute(item))
        {
            vm.OpenFileCommand.Execute(item);
        }
    }

    private static bool IsDescendantOfFolderButton(DependencyObject node)
    {
        while (node != null)
        {
            if (node is Wpf.Ui.Controls.Button btn && btn.Name == "DownloadRowFolder")
                return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }
}
