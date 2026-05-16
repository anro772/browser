using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.Models;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Controls;

/// <summary>
/// Active-profile chrome pill (Placement A from the Claude Design recommendation).
/// Inherits its DataContext from MainViewModel so it picks up the active profile
/// and privacy mode without any wiring on the host site.
/// </summary>
public partial class ActiveProfilePill : UserControl
{
    public ActiveProfilePill()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Click → opens the Profiles workspace. We keep the popup-switcher idea for
    /// later; jumping straight to the workspace is the cheapest disambiguation
    /// for now and reuses an existing destination.
    /// </summary>
    private void Pill_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.OpenWorkspaceCommand.CanExecute(WorkspaceSection.Profiles))
        {
            vm.OpenWorkspaceCommand.Execute(WorkspaceSection.Profiles);
        }
    }
}
