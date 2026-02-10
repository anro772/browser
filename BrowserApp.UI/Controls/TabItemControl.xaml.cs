using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.Controls;

public partial class TabItemControl : UserControl
{
    public event EventHandler<BrowserTabItem>? TabActivated;
    public event EventHandler<BrowserTabItem>? TabCloseRequested;

    public TabItemControl()
    {
        InitializeComponent();

        // Show close button on hover
        MouseEnter += (s, e) => CloseButton.Opacity = 1;
        MouseLeave += (s, e) => CloseButton.Opacity = 0;
    }

    private void TabBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is BrowserTabItem tab)
        {
            TabActivated?.Invoke(this, tab);
        }
    }

    private void TabBorder_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // Middle-click to close
        if (e.ChangedButton == MouseButton.Middle && DataContext is BrowserTabItem tab)
        {
            TabCloseRequested?.Invoke(this, tab);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrowserTabItem tab)
        {
            TabCloseRequested?.Invoke(this, tab);
        }
    }
}
