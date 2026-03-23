using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class CreateProfileDialog : FluentWindow
{
    public string ProfileName { get; private set; } = string.Empty;
    public string SelectedColor { get; set; } = "#0078D4";
    public string DialogTitle { get; set; } = "Create Profile";
    public string DialogSubtitle { get; set; } = "Create a new browser profile with its own data.";
    public bool ShowNameField { get; set; } = true;

    private static readonly string[] Colors = new[]
    {
        "#0078D4", "#107C10", "#E74856", "#FF8C00",
        "#881798", "#00B7C3", "#767676", "#FFB900"
    };

    private Border? _selectedBorder;

    public CreateProfileDialog()
    {
        InitializeComponent();
        DataContext = this;
        ColorSwatches.ItemsSource = Colors;
        Loaded += (s, e) =>
        {
            if (ShowNameField)
                ProfileNameInput.Focus();
            HighlightSelectedColor();
        };
    }

    private void HighlightSelectedColor()
    {
        // Find and highlight the initially selected color swatch
        for (int i = 0; i < Colors.Length; i++)
        {
            if (Colors[i] == SelectedColor)
            {
                var container = ColorSwatches.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container != null)
                {
                    var button = FindVisualChild<System.Windows.Controls.Button>(container);
                    if (button != null)
                    {
                        var border = FindVisualChild<Border>(button);
                        if (border != null)
                        {
                            border.BorderBrush = (Brush)FindResource("TextFillColorPrimaryBrush");
                            border.BorderThickness = new Thickness(3);
                            _selectedBorder = border;
                        }
                    }
                }
                break;
            }
        }
    }

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string color)
        {
            SelectedColor = color;

            // Reset previous selection
            if (_selectedBorder != null)
            {
                _selectedBorder.BorderBrush = (Brush)FindResource("BorderSubtleBrush");
                _selectedBorder.BorderThickness = new Thickness(2);
            }

            // Highlight new selection
            var border = FindVisualChild<Border>(button);
            if (border != null)
            {
                border.BorderBrush = (Brush)FindResource("TextFillColorPrimaryBrush");
                border.BorderThickness = new Thickness(3);
                _selectedBorder = border;
            }
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (ShowNameField && string.IsNullOrWhiteSpace(ProfileNameInput.Text))
        {
            System.Windows.MessageBox.Show("Profile name is required.", "Validation",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        ProfileName = ProfileNameInput.Text?.Trim() ?? string.Empty;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
