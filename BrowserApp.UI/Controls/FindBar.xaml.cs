using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;

namespace BrowserApp.UI.Controls;

public partial class FindBar : UserControl
{
    private WebView2? _webView;
    private string _lastQuery = string.Empty;

    public FindBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the find bar and focuses the text box.
    /// </summary>
    public void Open(WebView2? webView)
    {
        _webView = webView;
        Visibility = Visibility.Visible;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    /// <summary>
    /// Closes the find bar and clears the find highlights.
    /// </summary>
    public void Close()
    {
        Visibility = Visibility.Collapsed;
        _lastQuery = string.Empty;
        MatchCountText.Text = "";
    }

    private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = FindTextBox.Text;
        if (string.IsNullOrEmpty(query))
        {
            MatchCountText.Text = "";
            return;
        }

        _lastQuery = query;
        FindInPage(query, forward: true);
    }

    private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void FindNext()
    {
        if (!string.IsNullOrEmpty(_lastQuery))
        {
            FindInPage(_lastQuery, forward: true);
        }
    }

    private void FindPrevious()
    {
        if (!string.IsNullOrEmpty(_lastQuery))
        {
            FindInPage(_lastQuery, forward: false);
        }
    }

    private async void FindInPage(string query, bool forward)
    {
        if (_webView?.CoreWebView2 == null) return;

        try
        {
            var direction = forward ? "false" : "true";
            var script = $"window.find('{EscapeJs(query)}', false, {direction}, true, false, false, false)";
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);

            MatchCountText.Text = result == "true" ? "" : "No matches";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FindBar] Error: {ex.Message}");
        }
    }

    private static string EscapeJs(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => FindPrevious();
    private void NextButton_Click(object sender, RoutedEventArgs e) => FindNext();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
