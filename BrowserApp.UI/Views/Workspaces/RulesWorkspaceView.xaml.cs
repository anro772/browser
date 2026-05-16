using System;
using System.Windows;
using System.Windows.Controls;
using BrowserApp.UI.Models;
using BrowserApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.Views.Workspaces;

public partial class RulesWorkspaceView : UserControl
{
    private readonly IServiceProvider? _serviceProvider;

    public RulesWorkspaceView(RuleManagerViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _serviceProvider = serviceProvider;
    }

    private async void RulesWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RuleManagerViewModel viewModel)
        {
            await viewModel.LoadRulesCommand.ExecuteAsync(null);
        }
    }

    private void RulesEmpty_Primary(object? sender, EventArgs e)
    {
        if (DataContext is RuleManagerViewModel vm && vm.QuickAddRuleCommand.CanExecute(null))
        {
            vm.QuickAddRuleCommand.Execute(null);
        }
    }

    private void RulesEmpty_Secondary(object? sender, EventArgs e)
    {
        // Jump to the Marketplace workspace via MainViewModel's OpenWorkspaceCommand.
        if (_serviceProvider?.GetService<MainViewModel>() is { } main
            && main.OpenWorkspaceCommand.CanExecute(WorkspaceSection.Marketplace))
        {
            main.OpenWorkspaceCommand.Execute(WorkspaceSection.Marketplace);
        }
    }

    /// <summary>
    /// Per-row "..." button — opens Edit / Publish / Delete in a context menu.
    /// The button's DataContext is the RuleItemViewModel.
    /// </summary>
    private void RuleMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not RuleItemViewModel rule) return;
        if (DataContext is not RuleManagerViewModel vm) return;

        var menu = new ContextMenu();

        var edit = new MenuItem { Header = "Edit rule" };
        edit.Click += (_, _) => { if (vm.EditRuleCommand.CanExecute(rule)) vm.EditRuleCommand.Execute(rule); };
        menu.Items.Add(edit);

        if (rule.CanPublish)
        {
            var publish = new MenuItem { Header = "Publish to marketplace" };
            publish.Click += (_, _) => { if (vm.PublishRuleCommand.CanExecute(rule)) vm.PublishRuleCommand.Execute(rule); };
            menu.Items.Add(publish);
        }

        menu.Items.Add(new Separator());

        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) => { if (vm.DeleteRuleCommand.CanExecute(rule)) vm.DeleteRuleCommand.Execute(rule); };
        menu.Items.Add(delete);

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
