using System;
using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.Views.Workspaces;

public partial class ChannelsWorkspaceView : UserControl
{
    private readonly IServiceProvider? _serviceProvider;

    public ChannelsWorkspaceView(ChannelsViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
    }

    private async void ChannelsWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChannelsViewModel viewModel)
        {
            await viewModel.LoadChannelsCommand.ExecuteAsync(null);
        }
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
    /// Per-row "..." on a joined channel — currently houses Leave to keep the
    /// destructive action away from the dominant Sync button.
    /// </summary>
    private void ChannelMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is null) return;
        if (DataContext is not ChannelsViewModel vm) return;

        var menu = new ContextMenu();

        var leave = new MenuItem { Header = "Leave channel" };
        leave.Click += (_, _) => { if (vm.LeaveChannelCommand.CanExecute(fe.Tag)) vm.LeaveChannelCommand.Execute(fe.Tag); };
        menu.Items.Add(leave);

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
