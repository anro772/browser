using System.Windows;
using System.Windows.Controls;
using BrowserApp.Core.Models;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ProfilesWorkspaceView : UserControl
{
    public ProfilesWorkspaceView(ProfileSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Per-row "..." → context menu with Change color + Delete (Delete hidden for the default profile).
    /// </summary>
    private void ProfileMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not BrowserProfile profile) return;
        if (DataContext is not ProfileSelectorViewModel vm) return;

        var menu = new ContextMenu();

        var color = new MenuItem { Header = "Change color…" };
        color.Click += (_, _) => { if (vm.ChangeProfileColorCommand.CanExecute(profile)) vm.ChangeProfileColorCommand.Execute(profile); };
        menu.Items.Add(color);

        if (!profile.IsDefault)
        {
            menu.Items.Add(new Separator());
            var delete = new MenuItem { Header = "Delete profile" };
            delete.Click += (_, _) => { if (vm.DeleteProfileCommand.CanExecute(profile)) vm.DeleteProfileCommand.Execute(profile); };
            menu.Items.Add(delete);
        }

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
