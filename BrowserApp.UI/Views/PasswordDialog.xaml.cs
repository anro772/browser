using System.Windows;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

/// <summary>
/// Password input dialog with proper masking.
/// </summary>
public partial class PasswordDialog : FluentWindow
{
    /// <summary>
    /// The password entered by the user.
    /// </summary>
    public string Password { get; private set; } = string.Empty;

    /// <summary>
    /// Dialog title displayed in the window title bar.
    /// </summary>
    public string DialogTitle { get; set; } = "Enter Password";

    /// <summary>
    /// Prompt text explaining what the password is for.
    /// </summary>
    public string PromptText { get; set; } = "Please enter password:";

    public PasswordDialog()
    {
        InitializeComponent();
        DataContext = this;

        // Focus the password input when loaded
        Loaded += (s, e) => PasswordInput.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
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
