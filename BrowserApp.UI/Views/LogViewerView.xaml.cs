using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for LogViewerView.xaml
/// </summary>
public partial class LogViewerView : UserControl
{
    public LogViewerView(LogViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll when new items are added
        viewModel.LogEntries.CollectionChanged += (s, e) =>
        {
            if (viewModel.AutoScroll)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    LogScrollViewer.ScrollToEnd();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        };
    }
}
