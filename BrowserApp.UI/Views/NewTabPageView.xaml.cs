using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class NewTabPageView : UserControl
{
    public NewTabPageView(NewTabPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public async Task LoadAsync()
    {
        if (DataContext is NewTabPageViewModel vm)
        {
            await vm.LoadFrequentSitesAsync();
            SearchBox.Focus();
        }
    }
}
