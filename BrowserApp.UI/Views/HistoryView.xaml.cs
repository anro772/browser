using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BrowserApp.UI.Models;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Views;

/// <summary>
/// Interaction logic for HistoryView.xaml
/// Minimal code-behind - all logic in HistoryViewModel.
/// </summary>
public partial class HistoryView : UserControl
{
    private readonly HistoryViewModel _viewModel;

    public HistoryView(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Load history when view becomes visible
        await _viewModel.LoadHistoryCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Single-click navigate. MouseBinding inside a ListView template is swallowed by
    /// the ListBoxItem's selection plumbing, so we wire the event directly.
    /// </summary>
    private void HistoryRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // If the click originated from the "..." button (or its children), let it handle.
        if (e.OriginalSource is DependencyObject origin && IsDescendantOfMoreButton(origin))
            return;

        if (sender is FrameworkElement fe && fe.DataContext is HistoryEntryDisplay entry)
        {
            if (_viewModel.NavigateToEntryCommand.CanExecute(entry))
            {
                _viewModel.NavigateToEntryCommand.Execute(entry);
            }
        }
    }

    private static bool IsDescendantOfMoreButton(DependencyObject node)
    {
        while (node != null)
        {
            if (node is Wpf.Ui.Controls.Button btn && btn.Name == "HistoryRowMore")
                return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    /// <summary>
    /// Opens the … button's ContextMenu on left-click (otherwise it would need right-click).
    /// </summary>
    private void HistoryRowMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }
}
