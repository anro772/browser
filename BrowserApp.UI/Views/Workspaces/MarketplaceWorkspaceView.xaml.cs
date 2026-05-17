using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views.Workspaces;

public partial class MarketplaceWorkspaceView : UserControl
{
    public MarketplaceWorkspaceView(MarketplaceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void MarketplaceWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MarketplaceViewModel viewModel)
        {
            await viewModel.LoadRulesCommand.ExecuteAsync(null);
        }
    }

    private async void MarketplaceEmpty_Refresh(object? sender, EventArgs e)
    {
        if (DataContext is MarketplaceViewModel vm)
        {
            await vm.LoadRulesCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Card click → opens the detail dialog. Skip if click landed on the Install
    /// button or the inline author chip.
    /// </summary>
    private void MarketCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not MarketplaceRuleItemViewModel rule) return;
        if (DataContext is not MarketplaceViewModel vm) return;

        var node = e.OriginalSource as DependencyObject;
        while (node != null && node != fe)
        {
            if (node is ButtonBase)
            {
                return;
            }
            node = VisualTreeHelper.GetParent(node) ?? (node as FrameworkElement)?.Parent;
        }

        if (vm.ShowRuleDetailsCommand.CanExecute(rule))
        {
            vm.ShowRuleDetailsCommand.Execute(rule);
        }
    }

    private void AuthorChip_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MarketplaceViewModel vm) return;
        if (sender is FrameworkElement fe && fe.Tag is string author)
        {
            if (vm.FilterByAuthorCommand.CanExecute(author))
            {
                vm.FilterByAuthorCommand.Execute(author);
            }
        }
        e.Handled = true;
    }

    /// <summary>
    /// Tag chip click → sets SelectedTag (which triggers the /search reload).
    /// </summary>
    private void TagChip_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MarketplaceViewModel vm) return;
        if (sender is FrameworkElement fe && fe.Tag is string tag)
        {
            vm.SelectedTag = string.Equals(vm.SelectedTag, tag, StringComparison.OrdinalIgnoreCase)
                ? null
                : tag;
        }
        e.Handled = true;
    }
}
