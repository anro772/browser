using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the log viewer window.
/// Captures Debug.WriteLine output and displays it in real-time.
/// </summary>
public partial class LogViewerViewModel : ObservableObject
{
    private const int MaxLogEntries = 1000;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = new();

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showDebug = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarning = true;

    [ObservableProperty]
    private bool _showError = true;

    private readonly object _lock = new();

    public LogViewerViewModel()
    {
        // Add a custom trace listener to capture Debug.WriteLine
        Trace.Listeners.Add(new DebugTraceListener(AddLogEntry));
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
                LogEntries.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    Level = level
                });

                // Remove old entries if we exceed the limit
                while (LogEntries.Count > MaxLogEntries)
                {
                    LogEntries.RemoveAt(0);
                }
            }
        });
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
            LogEntries.Select(e => $"[{e.Timestamp:HH:mm:ss.fff}] [{e.Level}] {e.Message}"));

        Clipboard.SetText(allLogs);
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
    public LogLevel Level { get; init; }

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string LevelBadge => Level switch
    {
        LogLevel.Debug => "🔍",
        LogLevel.Info => "ℹ️",
        LogLevel.Warning => "⚠️",
        LogLevel.Error => "❌",
        _ => "•"
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
