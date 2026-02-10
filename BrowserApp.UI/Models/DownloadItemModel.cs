using CommunityToolkit.Mvvm.ComponentModel;

namespace BrowserApp.UI.Models;

public partial class DownloadItemModel : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _fileName = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }

    [ObservableProperty]
    private long _receivedBytes;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _status = "downloading";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime? _completedAt;

    public void UpdateProgress(long receivedBytes)
    {
        ReceivedBytes = receivedBytes;
        if (TotalBytes > 0)
        {
            Progress = (double)receivedBytes / TotalBytes * 100;
        }
    }
}
