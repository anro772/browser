using System.Windows;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class ProfileSelectorView : Window
{
    public ProfileSelectorView(ProfileSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
