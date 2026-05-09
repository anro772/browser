using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the debug console.
/// Combines two sources of log data:
///   - Trace.WriteLine output (anything calling Debug.WriteLine in the app).
///   - The on-disk log files written by ErrorLogger, tailed via LogTailService.
/// Provides level + category filtering, free-text search, and copy/open commands.
/// </summary>
public partial class LogViewerViewModel : ObservableObject, IDisposable
{
    private const int MaxLogEntries = 1000;
    private readonly LogTailService? _tailService;
    private readonly EventHandler<LogEntry>? _tailHandler;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showDebug = false;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarning = true;

    [ObservableProperty]
    private bool _showError = true;

    [ObservableProperty]
    private string _selectedCategory = "(all)";

    public ObservableCollection<string> Categories { get; } = new() { "(all)" };

    /// <summary>
    /// Filtered/sorted view of <see cref="LogEntries"/> bound by the XAML.
    /// Re-filtered when SearchText or any of the level/category toggles change.
    /// </summary>
    public ICollectionView FilteredEntries { get; }

    private readonly object _lock = new();

    public LogViewerViewModel() : this(null) { }

    public LogViewerViewModel(LogTailService? tailService)
    {
        _tailService = tailService;

        FilteredEntries = CollectionViewSource.GetDefaultView(LogEntries);
        FilteredEntries.Filter = FilterEntry;

        // Add a custom trace listener to capture Debug.WriteLine output
        // (covers code that doesn't go through ErrorLogger).
        Trace.Listeners.Add(new DebugTraceListener(AddLogEntry));

        if (_tailService != null)
        {
            // Seed from the tail service so the console shows the existing tail
            // of today's log files when it's first opened.
            foreach (var seed in _tailService.GetHistory())
            {
                LogEntries.Add(seed);
                EnsureCategory(seed.Category);
            }

            _tailHandler = (_, entry) =>
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    lock (_lock)
                    {
                        LogEntries.Add(entry);
                        EnsureCategory(entry.Category);
                        TrimOldEntries();
                    }
                });
            };
            _tailService.LogLineAppended += _tailHandler;
        }
    }

    partial void OnSearchTextChanged(string value) => FilteredEntries?.Refresh();
    partial void OnShowDebugChanged(bool value) => FilteredEntries?.Refresh();
    partial void OnShowInfoChanged(bool value) => FilteredEntries?.Refresh();
    partial void OnShowWarningChanged(bool value) => FilteredEntries?.Refresh();
    partial void OnShowErrorChanged(bool value) => FilteredEntries?.Refresh();
    partial void OnSelectedCategoryChanged(string value) => FilteredEntries?.Refresh();

    private bool FilterEntry(object obj)
    {
        if (obj is not LogEntry entry) return false;

        switch (entry.Level)
        {
            case LogLevel.Debug when !ShowDebug: return false;
            case LogLevel.Info when !ShowInfo: return false;
            case LogLevel.Warning when !ShowWarning: return false;
            case LogLevel.Error when !ShowError: return false;
        }

        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "(all)"
            && !string.Equals(entry.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            if (entry.Message.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0
                && entry.Category.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private void EnsureCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) return;
        if (Categories.Contains(category)) return;
        // Insert kept simple — sort once on insert to keep dropdown tidy.
        var insertAt = 1; // skip "(all)"
        while (insertAt < Categories.Count
               && string.Compare(Categories[insertAt], category, StringComparison.OrdinalIgnoreCase) < 0)
        {
            insertAt++;
        }
        Categories.Insert(insertAt, category);
    }

    private void TrimOldEntries()
    {
        while (LogEntries.Count > MaxLogEntries)
        {
            LogEntries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Adds a log entry to the collection.
    /// Thread-safe and limits to MaxLogEntries.
    /// </summary>
    public void AddLogEntry(string message, LogLevel level = LogLevel.Debug)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                var (clean, category) = ExtractCategoryFromMessage(message);
                var entry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = clean,
                    Level = level,
                    Category = category,
                    Source = LogSource.Trace,
                };
                LogEntries.Add(entry);
                EnsureCategory(category);
                TrimOldEntries();
            }
        });
    }

    private static (string Message, string Category) ExtractCategoryFromMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return (string.Empty, "App");
        if (!message.StartsWith("[")) return (message, "App");
        var end = message.IndexOf(']');
        if (end <= 1) return (message, "App");
        var category = message.Substring(1, end - 1);
        var clean = message.Substring(end + 1).TrimStart();
        return (clean, category);
    }

    [RelayCommand]
    private void Clear()
    {
        lock (_lock)
        {
            LogEntries.Clear();
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        var allLogs = string.Join(Environment.NewLine,
            FilteredEntries.Cast<LogEntry>().Select(e => $"[{e.Timestamp:HH:mm:ss.fff}] [{e.Level}] [{e.Category}] {e.Message}"));

        try { Clipboard.SetText(allLogs); } catch { /* clipboard busy */ }
    }

    [RelayCommand]
    private void CopyEntry(LogEntry? entry)
    {
        if (entry == null) return;
        var line = $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] [{entry.Category}] {entry.Message}";
        if (!string.IsNullOrEmpty(entry.Detail))
        {
            line += Environment.NewLine + entry.Detail;
        }
        try { Clipboard.SetText(line); } catch { /* clipboard busy */ }
    }

    [RelayCommand]
    private void CopyMessage(LogEntry? entry)
    {
        if (entry == null) return;
        try { Clipboard.SetText(entry.Message); } catch { /* clipboard busy */ }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ErrorLogger.GetLogDirectory(),
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LogViewer] open folder failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_tailService != null && _tailHandler != null)
        {
            _tailService.LogLineAppended -= _tailHandler;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Custom trace listener that forwards to the ViewModel.
    /// </summary>
    private class DebugTraceListener : TraceListener
    {
        private readonly Action<string, LogLevel> _onWrite;

        public DebugTraceListener(Action<string, LogLevel> onWrite)
        {
            _onWrite = onWrite;
        }

        public override void Write(string? message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _onWrite(message, LogLevel.Debug);
            }
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // Parse log level from message if it contains [Level] prefix
                var level = LogLevel.Debug;
                var cleanMessage = message;

                if (message.Contains("[AdBlockerService]") || message.Contains("[FilterParser]"))
                {
                    level = LogLevel.Info;
                }
                else if (message.Contains("Error") || message.Contains("error") || message.Contains("Failed"))
                {
                    level = LogLevel.Error;
                }
                else if (message.Contains("Warning") || message.Contains("warning"))
                {
                    level = LogLevel.Warning;
                }

                _onWrite(cleanMessage, level);
            }
        }
    }
}

/// <summary>
/// Represents a single log entry.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Category { get; init; } = "App";
    public LogLevel Level { get; init; }
    public LogSource Source { get; init; } = LogSource.Trace;

    /// <summary>
    /// Optional multi-line body (e.g. an error block including stack trace).
    /// Surfaced via context-menu copy and tooltip.
    /// </summary>
    public string? Detail { get; init; }

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string LevelLabel => Level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "•"
    };

    /// <summary>
    /// Hex color for the level pill in the debug console (matches BrowserTheme semantic colors).
    /// </summary>
    public string LevelColor => Level switch
    {
        LogLevel.Debug => "#6B7394",   // TextTertiary
        LogLevel.Info => "#93C5FD",    // Info
        LogLevel.Warning => "#FCD34D", // Warning
        LogLevel.Error => "#FCA5A5",   // Error
        _ => "#6B7394"
    };
}

/// <summary>
/// Log severity levels.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Where a log entry originated.
/// </summary>
public enum LogSource
{
    /// <summary>From Trace.WriteLine / Debug.WriteLine.</summary>
    Trace,
    /// <summary>From a tailed log file on disk.</summary>
    File,
}
