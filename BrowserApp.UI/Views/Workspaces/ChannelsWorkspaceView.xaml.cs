using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ChannelsWorkspaceView : UserControl
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly DispatcherTimer _autoRefresh;

    public ChannelsWorkspaceView(ChannelsViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;

        // Background catalog refresh — pulls the latest channel list from the server
        // while the user is on this workspace. Stops as soon as the view unloads, so
        // there's no traffic when the panel is hidden.
        _autoRefresh = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _autoRefresh.Tick += OnAutoRefreshTick;
        Unloaded += (_, _) => _autoRefresh.Stop();
    }

    private async void ChannelsWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChannelsViewModel viewModel)
        {
            await viewModel.LoadChannelsCommand.ExecuteAsync(null);
        }
        _autoRefresh.Start();
    }

    private async void OnAutoRefreshTick(object? sender, EventArgs e)
    {
        if (DataContext is not ChannelsViewModel vm) return;
        if (vm.IsLoading) return;
        await vm.LoadChannelsCommand.ExecuteAsync(null);
    }

    private async void ChannelsEmpty_Refresh(object? sender, EventArgs e)
    {
        if (DataContext is ChannelsViewModel vm)
        {
            await vm.LoadChannelsCommand.ExecuteAsync(null);
        }
    }

    private void ChannelsEmpty_Skip(object? sender, EventArgs e)
    {
        // "Skip for now" — closes the modal so a solo user can go back to browsing.
        if (_serviceProvider?.GetService<MainViewModel>() is { } main
            && main.CloseWorkspaceCommand.CanExecute(null))
        {
            main.CloseWorkspaceCommand.Execute(null);
        }
    }

    /// <summary>
    /// Per-row "..." on a joined channel. View details + (for owners) Add rule,
    /// then Leave at the bottom (destructive, separated).
    /// </summary>
    private void ChannelMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not UnifiedChannelViewModel channel) return;
        if (DataContext is not ChannelsViewModel vm) return;

        var menu = new ContextMenu();

        var view = new MenuItem { Header = "View details" };
        view.Click += (_, _) => { if (vm.ShowChannelDetailsCommand.CanExecute(channel)) vm.ShowChannelDetailsCommand.Execute(channel); };
        menu.Items.Add(view);

        if (channel.IsOwner)
        {
            var add = new MenuItem { Header = "Add rule…" };
            add.Click += (_, _) => { if (vm.AddRuleToChannelCommand.CanExecute(channel)) vm.AddRuleToChannelCommand.Execute(channel); };
            menu.Items.Add(add);
        }

        menu.Items.Add(new Separator());

        var leave = new MenuItem { Header = "Leave channel" };
        leave.Click += (_, _) => { if (vm.LeaveChannelCommand.CanExecute(channel)) vm.LeaveChannelCommand.Execute(channel); };
        menu.Items.Add(leave);

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>
    /// Click anywhere on a channel card → opens the detail dialog. Skip interactive
    /// children (Join/Sync/More buttons) so they keep their own behavior.
    /// </summary>
    private void ChannelCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not UnifiedChannelViewModel channel) return;
        if (DataContext is not ChannelsViewModel vm) return;

        var node = e.OriginalSource as DependencyObject;
        while (node != null && node != fe)
        {
            if (node is ButtonBase)
            {
                return;
            }
            node = VisualTreeHelper.GetParent(node) ?? (node as FrameworkElement)?.Parent;
        }

        if (vm.ShowChannelDetailsCommand.CanExecute(channel))
        {
            vm.ShowChannelDetailsCommand.Execute(channel);
        }
    }
}
