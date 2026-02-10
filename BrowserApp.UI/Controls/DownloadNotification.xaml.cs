using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace BrowserApp.UI.Controls;

public partial class DownloadNotification : UserControl
{
    private string? _downloadPath;

    public DownloadNotification()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Subscribes to download events on the given CoreWebView2.
    /// </summary>
    public void WireToWebView(CoreWebView2 coreWebView2)
    {
        coreWebView2.DownloadStarting += OnDownloadStarting;
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _downloadPath = e.ResultFilePath;
            var fileName = Path.GetFileName(e.ResultFilePath);
            FileNameText.Text = $"Downloading: {fileName}";
            DownloadProgress.IsIndeterminate = false;
            DownloadProgress.Value = 0;
            OpenButton.Visibility = Visibility.Collapsed;
            ShowInFolderButton.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Visible;
        });

        e.DownloadOperation.BytesReceivedChanged += (s, _) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var op = e.DownloadOperation;
                if (op.TotalBytesToReceive > 0)
                {
                    DownloadProgress.IsIndeterminate = false;
                    DownloadProgress.Maximum = (double)op.TotalBytesToReceive;
                    DownloadProgress.Value = (double)op.BytesReceived;
                }
            });
        };

        e.DownloadOperation.StateChanged += (s, _) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var state = e.DownloadOperation.State;
                if (state == CoreWebView2DownloadState.Completed)
                {
                    var fileName = Path.GetFileName(_downloadPath ?? "file");
                    FileNameText.Text = $"Downloaded: {fileName}";
                    DownloadProgress.Value = DownloadProgress.Maximum;
                    OpenButton.Visibility = Visibility.Visible;
                    ShowInFolderButton.Visibility = Visibility.Visible;
                }
                else if (state == CoreWebView2DownloadState.Interrupted)
                {
                    FileNameText.Text = "Download failed";
                    DownloadProgress.IsIndeterminate = false;
                }
            });
        };
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_downloadPath) && File.Exists(_downloadPath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_downloadPath) { UseShellExecute = true });
            }
            catch { }
        }
    }

    private void ShowInFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_downloadPath) && File.Exists(_downloadPath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{_downloadPath}\"");
            }
            catch { }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }
}
