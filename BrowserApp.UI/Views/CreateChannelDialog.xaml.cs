using System.Windows;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class CreateChannelDialog : FluentWindow
{
    public string ChannelName { get; private set; } = string.Empty;
    public string ChannelDescription { get; private set; } = string.Empty;
    public string ChannelPassword { get; private set; } = string.Empty;

    public CreateChannelDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => ChannelNameInput.Focus();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ChannelNameInput.Text))
        {
            System.Windows.MessageBox.Show("Channel name is required.", "Validation",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ChannelPasswordInput.Password) || ChannelPasswordInput.Password.Length < 4)
        {
            System.Windows.MessageBox.Show("Password must be at least 4 characters.", "Validation",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        ChannelName = ChannelNameInput.Text.Trim();
        ChannelDescription = ChannelDescriptionInput.Text?.Trim() ?? string.Empty;
        ChannelPassword = ChannelPasswordInput.Password;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
