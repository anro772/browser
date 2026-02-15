using System.Windows;
using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class ExtensionManagerView : FluentWindow
{
    public ExtensionManagerView(ExtensionManagerViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (s, e) => await viewModel.LoadExtensionsCommand.ExecuteAsync(null);
    }

}
