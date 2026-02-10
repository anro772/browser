using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace BrowserApp.UI.Controls;

public partial class DownloadNotification : UserControl
{
    private string? _downloadPath;

    /// <summary>
    /// Fired when a download starts.
    /// </summary>
    public event EventHandler<DownloadStartedEventArgs>? DownloadStarted;

    /// <summary>
    /// Fired when download progress changes.
    /// </summary>
    public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;

    /// <summary>
    /// Fired when a download completes or fails.
    /// </summary>
    public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

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

    /// <summary>
    /// Unsubscribes from download events on the given CoreWebView2.
    /// Call this when a tab is closed to prevent memory leaks.
    /// </summary>
    public void UnwireFromWebView(CoreWebView2 coreWebView2)
    {
        coreWebView2.DownloadStarting -= OnDownloadStarting;
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        // Bug 3: Capture the per-download result file path in a local variable.
        // Don't set _downloadPath here — it would be overwritten by concurrent downloads.
        var downloadResultPath = e.ResultFilePath;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            var fileName = Path.GetFileName(downloadResultPath);
            FileNameText.Text = $"Downloading: {fileName}";
            DownloadProgress.IsIndeterminate = false;
            DownloadProgress.Value = 0;
            OpenButton.Visibility = Visibility.Collapsed;
            ShowInFolderButton.Visibility = Visibility.Collapsed;
            Visibility = Visibility.Visible;

            // Fire download started event
            DownloadStarted?.Invoke(this, new DownloadStartedEventArgs
            {
                FileName = fileName,
                SourceUrl = e.DownloadOperation.Uri,
                DestinationPath = downloadResultPath,
                TotalBytes = (long)(e.DownloadOperation.TotalBytesToReceive ?? 0)
            });
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

                // Fire progress event
                DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    DestinationPath = downloadResultPath,
                    ReceivedBytes = (long)op.BytesReceived,
                    TotalBytes = (long)(op.TotalBytesToReceive ?? 0)
                });
            });
        };

        e.DownloadOperation.StateChanged += (s, _) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var state = e.DownloadOperation.State;
                if (state == CoreWebView2DownloadState.Completed)
                {
                    // Bug 3: Set _downloadPath only on completion, using the captured per-download path
                    _downloadPath = downloadResultPath;
                    var fileName = Path.GetFileName(downloadResultPath);
                    FileNameText.Text = $"Downloaded: {fileName}";
                    DownloadProgress.Value = DownloadProgress.Maximum;
                    OpenButton.Visibility = Visibility.Visible;
                    ShowInFolderButton.Visibility = Visibility.Visible;

                    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                    {
                        DestinationPath = downloadResultPath,
                        Success = true
                    });
                }
                else if (state == CoreWebView2DownloadState.Interrupted)
                {
                    FileNameText.Text = "Download failed";
                    DownloadProgress.IsIndeterminate = false;

                    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                    {
                        DestinationPath = downloadResultPath,
                        Success = false
                    });
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

public class DownloadStartedEventArgs : EventArgs
{
    public string FileName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
}

public class DownloadProgressEventArgs : EventArgs
{
    public string DestinationPath { get; set; } = string.Empty;
    public long ReceivedBytes { get; set; }
    public long TotalBytes { get; set; }
}

public class DownloadCompletedEventArgs : EventArgs
{
    public string DestinationPath { get; set; } = string.Empty;
    public bool Success { get; set; }
}
