using System.Windows;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class PublishRuleDialog : FluentWindow
{
    public string RuleName
    {
        get => RuleNameInput.Text;
        set => RuleNameInput.Text = value;
    }

    public string Description
    {
        get => DescriptionInput.Text?.Trim() ?? string.Empty;
        set => DescriptionInput.Text = value;
    }

    public string[] Tags => string.IsNullOrWhiteSpace(TagsInput.Text)
        ? []
        : TagsInput.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public PublishRuleDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => DescriptionInput.Focus();
    }

    private void PublishButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionInput.Text))
        {
            System.Windows.MessageBox.Show("Please add a description so others know what this rule does.",
                "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
