using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.Views.Workspaces;

public partial class WorkspaceHostView : UserControl
{
    private IServiceProvider? _serviceProvider;

    public WorkspaceHostView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Arms lazy-resolution on each of the six workspace ContentControls. The actual
    /// view (RulesWorkspaceView, ExtensionsWorkspaceView, etc.) is constructed from
    /// DI only the first time its ContentControl becomes visible — i.e. the first
    /// time the user opens that section. Subsequent toggles cost nothing.
    ///
    /// MainWindow's ctor calls this once after assigning <c>WorkspaceHostContainer.Content</c>.
    /// </summary>
    public void WireLazyWorkspaces(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        WireLazy<RulesWorkspaceView>(RulesContent);
        WireLazy<ExtensionsWorkspaceView>(ExtensionsContent);
        WireLazy<MarketplaceWorkspaceView>(MarketplaceContent);
        WireLazy<ChannelsWorkspaceView>(ChannelsContent);
        WireLazy<ProfilesWorkspaceView>(ProfilesContent);
        WireLazy<SettingsWorkspaceView>(SettingsContent);
    }

    private void WireLazy<T>(ContentControl host) where T : UIElement
    {
        DependencyPropertyChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            // Guard against spurious notifications (layout passes that don't change
            // visibility) and against double-resolution.
            if (!host.IsVisible || host.Content != null || _serviceProvider == null) return;

            host.Content = _serviceProvider.GetRequiredService<T>();
            host.IsVisibleChanged -= handler;
        };
        host.IsVisibleChanged += handler;
    }

    /// <summary>
    /// Forwards a "scroll to Privacy Mode card" request to the lazily-resolved
    /// <see cref="SettingsWorkspaceView"/>. Called by MainWindow when the user clicks
    /// the Mode tile on the Privacy Dashboard. Safe to call before the view has been
    /// resolved — it just no-ops in that case, but in practice MainWindow defers to
    /// the Loaded dispatcher tick so the lazy resolution has already fired.
    /// </summary>
    public void RevealSettingsPrivacyModeSection()
    {
        if (SettingsContent.Content is SettingsWorkspaceView view)
        {
            view.ScrollToPrivacyMode();
        }
    }
}
