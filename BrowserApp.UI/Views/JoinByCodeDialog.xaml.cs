using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class JoinByCodeDialog : FluentWindow
{
    public Guid ChannelId { get; private set; }
    public string Password { get; private set; } = string.Empty;

    public JoinByCodeDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => ChannelIdInput.Focus();
    }

    private void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        var raw = ChannelIdInput.Text?.Trim() ?? string.Empty;
        if (!Guid.TryParse(raw, out var id))
        {
            System.Windows.MessageBox.Show("That doesn't look like a valid channel ID.",
                "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(PasswordInput.Password))
        {
            System.Windows.MessageBox.Show("Password is required to join a channel.",
                "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        ChannelId = id;
        Password = PasswordInput.Password;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
