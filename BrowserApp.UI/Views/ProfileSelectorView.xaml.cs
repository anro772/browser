using System.Windows;
using Wpf.Ui.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

public partial class ProfileSelectorView : FluentWindow
{
    public ProfileSelectorView(ProfileSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
