using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class ConfirmDialog : FluentWindow
{
    public string DialogTitle { get; set; } = "Confirm";
    public string MessageText { get; set; } = "Are you sure?";
    public string ConfirmButtonText { get; set; } = "OK";
    public string CancelButtonText { get; set; } = "Cancel";
    public Visibility CancelVisibility { get; set; } = Visibility.Visible;

    public ConfirmDialog()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (IsDestructive && TryFindResource("ErrorBrush") is Brush err)
        {
            ConfirmBtn.Background = err;
            ConfirmBtn.BorderBrush = err;
            ConfirmBtn.Foreground = Brushes.Black;
        }
    }

    public bool IsDestructive { get; set; }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// One-shot helper: returns true if user confirmed, false otherwise.
    /// </summary>
    public static bool Show(Window? owner, string title, string message, bool destructive = false, string confirmText = "OK", string cancelText = "Cancel", bool showCancel = true)
    {
        var dlg = new ConfirmDialog
        {
            DialogTitle = title,
            MessageText = message,
            ConfirmButtonText = confirmText,
            CancelButtonText = cancelText,
            CancelVisibility = showCancel ? Visibility.Visible : Visibility.Collapsed,
            IsDestructive = destructive,
            Owner = owner
        };
        return dlg.ShowDialog() == true;
    }
}
