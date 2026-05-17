using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.Controls;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class SettingsWorkspaceView : UserControl
{
    public SettingsWorkspaceView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnVmPropertyChanged;
        Unloaded += (_, _) => viewModel.PropertyChanged -= OnVmPropertyChanged;
    }

    // VM is constructed eagerly at app start, before the WebView2 profile (and built-in
    // extensions) exist. Re-pull the ad-blocker state every time the panel is shown so
    // the toggle reflects the actual DB / WebView2 state — not a stale "false" from
    // boot time.
    private async void SettingsWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.RefreshAdBlockerStateAsync();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.LastSavedCounter))
        {
            SavedPillAnimator.Pulse(SavedPill);
        }
    }
}
