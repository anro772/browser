using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserApp.Data.Entities;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Controls;

/// <summary>
/// Horizontal bookmarks bar shown under the navigation bar (Chrome/Brave style).
/// Toggled via Ctrl+Shift+B; bound to BookmarkViewModel.Bookmarks.
/// </summary>
public partial class BookmarksBar : UserControl
{
    /// <summary>
    /// Raised when the user requests to open a bookmark in a new tab.
    /// MainWindow handles this since the new-tab plumbing lives there.
    /// </summary>
    public event EventHandler<BookmarkEntity>? OpenInNewTabRequested;

    public BookmarksBar()
    {
        InitializeComponent();
    }

    private BookmarkViewModel? Vm => DataContext as BookmarkViewModel;

    private void BookmarkChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is BookmarkEntity bookmark)
        {
            Vm?.NavigateToBookmarkCommand.Execute(bookmark);
        }
    }

    /// <summary>
    /// Middle-click opens the bookmark in a new tab — common browser convention.
    /// </summary>
    private void BookmarkChip_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle
            && sender is FrameworkElement fe
            && fe.Tag is BookmarkEntity bookmark)
        {
            OpenInNewTabRequested?.Invoke(this, bookmark);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Right-click opens a context menu with Open / Open in new tab / Copy URL / Remove.
    /// Built in code rather than XAML to avoid ContextMenu DataContext-inheritance issues.
    /// </summary>
    private void BookmarkChip_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not BookmarkEntity bookmark) return;

        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open" };
        openItem.Click += (_, _) => Vm?.NavigateToBookmarkCommand.Execute(bookmark);
        menu.Items.Add(openItem);

        var newTabItem = new MenuItem { Header = "Open in new tab" };
        newTabItem.Click += (_, _) => OpenInNewTabRequested?.Invoke(this, bookmark);
        menu.Items.Add(newTabItem);

        menu.Items.Add(new Separator());

        var copyItem = new MenuItem { Header = "Copy URL" };
        copyItem.Click += (_, _) =>
        {
            try { Clipboard.SetText(bookmark.Url); } catch { /* clipboard busy */ }
        };
        menu.Items.Add(copyItem);

        var removeItem = new MenuItem { Header = "Remove" };
        removeItem.Click += async (_, _) =>
        {
            if (Vm != null) await Vm.RemoveBookmarkCommand.ExecuteAsync(bookmark);
        };
        menu.Items.Add(removeItem);

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void BookmarksScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
