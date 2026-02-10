using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class BookmarksPanel : UserControl
{
    public BookmarksPanel(BookmarkViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BookmarkViewModel vm)
        {
            await vm.LoadBookmarksAsync();
        }
    }
}
