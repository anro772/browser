using System.Windows.Controls;

namespace BrowserApp.UI.Views.Workspaces;

public partial class WorkspaceHostView : UserControl
{
    public WorkspaceHostView()
    {
        InitializeComponent();
    }

    public void SetWorkspaceContent(
        RulesWorkspaceView rulesView,
        ExtensionsWorkspaceView extensionsView,
        MarketplaceWorkspaceView marketplaceView,
        ChannelsWorkspaceView channelsView,
        ProfilesWorkspaceView profilesView,
        SettingsWorkspaceView settingsView)
    {
        RulesContent.Content = rulesView;
        ExtensionsContent.Content = extensionsView;
        MarketplaceContent.Content = marketplaceView;
        ChannelsContent.Content = channelsView;
        ProfilesContent.Content = profilesView;
        SettingsContent.Content = settingsView;
    }
}
