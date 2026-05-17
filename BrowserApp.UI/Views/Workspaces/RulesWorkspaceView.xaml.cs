using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
    /// Row click → open editor. Skip if the click landed on the toggle switch,
    /// the "..." button, or the origin marker (which has its own tooltip).
    /// </summary>
    private void RuleRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not RuleItemViewModel rule) return;
        if (DataContext is not RuleManagerViewModel vm) return;

        // Walk visual tree from OriginalSource upward; bail if we hit an interactive child.
        var node = e.OriginalSource as DependencyObject;
        while (node != null && node != fe)
        {
            // ButtonBase covers Button, ToggleButton, RepeatButton; Wpf.Ui.Controls.ToggleSwitch is a ToggleButton.
            if (node is ButtonBase)
            {
                return;
            }
            node = VisualTreeHelper.GetParent(node) ?? (node as FrameworkElement)?.Parent;
        }

        vm.SelectedRule = rule;
        if (vm.EditRuleCommand.CanExecute(rule))
        {
            vm.EditRuleCommand.Execute(rule);
        }
    }

    private void RulesRoot_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not RuleManagerViewModel vm) return;

        if (e.Key == Key.Delete && vm.SelectedRule is { } target)
        {
            if (vm.DeleteRuleCommand.CanExecute(target))
            {
                vm.DeleteRuleCommand.Execute(target);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (vm.QuickAddRuleCommand.CanExecute(null))
            {
                vm.QuickAddRuleCommand.Execute(null);
                e.Handled = true;
            }
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

        var duplicate = new MenuItem { Header = "Duplicate" };
        duplicate.Click += (_, _) => { if (vm.DuplicateRuleCommand.CanExecute(rule)) vm.DuplicateRuleCommand.Execute(rule); };
        menu.Items.Add(duplicate);

        if (rule.CanPublish)
        {
            var publish = new MenuItem { Header = "Publish to marketplace" };
            publish.Click += (_, _) => { if (vm.PublishRuleCommand.CanExecute(rule)) vm.PublishRuleCommand.Execute(rule); };
            menu.Items.Add(publish);
        }

        var copyId = new MenuItem { Header = "Copy rule ID" };
        copyId.Click += (_, _) => { if (vm.CopyRuleIdCommand.CanExecute(rule)) vm.CopyRuleIdCommand.Execute(rule); };
        menu.Items.Add(copyId);

        menu.Items.Add(new Separator());

        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) => { if (vm.DeleteRuleCommand.CanExecute(rule)) vm.DeleteRuleCommand.Execute(rule); };
        menu.Items.Add(delete);

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
