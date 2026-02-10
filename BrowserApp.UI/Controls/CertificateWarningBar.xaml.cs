using System.Windows;
using System.Windows.Controls;

namespace BrowserApp.UI.Controls;

public partial class CertificateWarningBar : UserControl
{
    public event EventHandler? ProceedClicked;
    public event EventHandler? GoBackClicked;

    public CertificateWarningBar()
    {
        InitializeComponent();
    }

    public void Show(string message)
    {
        WarningText.Text = message;
        Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }

    private void ProceedButton_Click(object sender, RoutedEventArgs e)
    {
        ProceedClicked?.Invoke(this, EventArgs.Empty);
    }

    private void GoBackButton_Click(object sender, RoutedEventArgs e)
    {
        GoBackClicked?.Invoke(this, EventArgs.Empty);
        Hide();
    }
}
