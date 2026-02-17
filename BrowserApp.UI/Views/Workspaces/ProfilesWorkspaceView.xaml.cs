using System.Windows.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ProfilesWorkspaceView : UserControl
{
    public ProfilesWorkspaceView(ProfileSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
