using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using BrowserApp.UI.ViewModels;

namespace BrowserApp.UI.Services;

/// <summary>
/// Tails the on-disk log files written by <see cref="ErrorLogger"/> and turns them into
/// structured <see cref="LogEntry"/> events for the debug console.
///
/// The two files we watch:
///   - info_&lt;date&gt;.log   (one line per message, format "[yyyy-MM-dd HH:mm:ss.fff] message")
///   - errors_&lt;date&gt;.log (multi-line block per error, separated by "================================")
///
/// Lifecycle:
///   1. Constructor reads the tail of today's files and stores it as initial history.
///   2. A FileSystemWatcher on the Logs directory raises Changed events whenever the
///      writer appends. We track per-file byte position and parse only the delta.
///   3. New entries are surfaced via <see cref="LogLineAppended"/>; consumers (the view
///      model) marshal to the UI thread themselves.
/// </summary>
public class LogTailService : IDisposable
{
    private const int InitialTailLineCount = 500;
    private const int MaxHistory = 1000;

    private static readonly Regex InfoLineRegex = new(
        @"^\[(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\]\s*(?<msg>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex CategoryRegex = new(
        @"^\[(?<cat>[A-Za-z][A-Za-z0-9 _\-]*)\]\s*",
        RegexOptions.Compiled);

    private readonly string _logDir;
    private readonly Dictionary<string, long> _filePositions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LogEntry> _history = new();
    private readonly object _lock = new();
    private FileSystemWatcher? _watcher;
    private DispatcherTimer? _coalesceTimer;
    private readonly HashSet<string> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Raised once per parsed entry as new lines are flushed from the watched files.
    /// Subscribers run on whatever thread the watcher fires from — marshal as needed.
    /// </summary>
    public event EventHandler<LogEntry>? LogLineAppended;

    public LogTailService()
    {
        _logDir = ErrorLogger.GetLogDirectory();

        try
        {
            Directory.CreateDirectory(_logDir);
            SeedHistory();
            StartWatcher();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogTailService] init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a snapshot of the most recent parsed entries (oldest first).
    /// </summary>
    public IReadOnlyList<LogEntry> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToArray();
        }
    }

    private void SeedHistory()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var infoFile = Path.Combine(_logDir, $"info_{date}.log");
        var errorFile = Path.Combine(_logDir, $"errors_{date}.log");

        SeedFromFile(infoFile, isError: false);
        SeedFromFile(errorFile, isError: true);
    }

    private void SeedFromFile(string path, bool isError)
    {
        try
        {
            if (!File.Exists(path))
            {
                _filePositions[path] = 0;
                return;
            }

            string text;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                text = reader.ReadToEnd();
                _filePositions[path] = fs.Length;
            }

            var entries = ParseChunk(text, isError).TakeLast(InitialTailLineCount);
            lock (_lock)
            {
                _history.AddRange(entries);
                TrimHistory();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogTailService] seed {path}: {ex.Message}");
        }
    }

    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_logDir, "*.log")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;

        // FileSystemWatcher fires multiple events per write — coalesce them so we read each file
        // at most once per ~150ms instead of thrashing.
        if (Application.Current != null)
        {
            _coalesceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _coalesceTimer.Tick += FlushPending;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Filter to today's logs only — old files are noise.
        var name = Path.GetFileName(e.FullPath);
        if (!name.StartsWith("info_", StringComparison.OrdinalIgnoreCase)
            && !name.StartsWith("errors_", StringComparison.OrdinalIgnoreCase))
            return;

        lock (_lock)
        {
            _pendingFiles.Add(e.FullPath);
        }

        // If we have a UI dispatcher, coalesce. Otherwise just process inline.
        if (_coalesceTimer != null)
        {
            try
            {
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    _coalesceTimer.Stop();
                    _coalesceTimer.Start();
                }, DispatcherPriority.Background);
            }
            catch { /* dispatcher torn down */ }
        }
        else
        {
            FlushPending(this, EventArgs.Empty);
        }
    }

    private void FlushPending(object? sender, EventArgs e)
    {
        _coalesceTimer?.Stop();

        string[] paths;
        lock (_lock)
        {
            if (_pendingFiles.Count == 0) return;
            paths = _pendingFiles.ToArray();
            _pendingFiles.Clear();
        }

        foreach (var path in paths)
        {
            ProcessDelta(path);
        }
    }

    private void ProcessDelta(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _filePositions[path] = 0;
                return;
            }

            var isError = Path.GetFileName(path).StartsWith("errors_", StringComparison.OrdinalIgnoreCase);
            var lastPos = _filePositions.TryGetValue(path, out var p) ? p : 0;

            string delta;
            long newPos;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length < lastPos)
                {
                    // File rotated/truncated — start over.
                    lastPos = 0;
                }
                fs.Position = lastPos;
                using var reader = new StreamReader(fs);
                delta = reader.ReadToEnd();
                newPos = fs.Length;
            }

            _filePositions[path] = newPos;
            if (string.IsNullOrEmpty(delta)) return;

            var entries = ParseChunk(delta, isError);
            foreach (var entry in entries)
            {
                lock (_lock)
                {
                    _history.Add(entry);
                    TrimHistory();
                }
                LogLineAppended?.Invoke(this, entry);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LogTailService] delta {path}: {ex.Message}");
        }
    }

    private void TrimHistory()
    {
        // Caller must hold _lock.
        if (_history.Count <= MaxHistory) return;
        _history.RemoveRange(0, _history.Count - MaxHistory);
    }

    /// <summary>
    /// Parses a chunk of log text. Info logs are line-per-entry; error logs are multi-line
    /// blocks ending with "================================" — we collapse each block into
    /// a single entry whose Message is the block body.
    /// </summary>
    private static IEnumerable<LogEntry> ParseChunk(string text, bool isError)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        if (isError)
        {
            foreach (var entry in ParseErrorBlocks(text))
                yield return entry;
            yield break;
        }

        var lines = text.Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var match = InfoLineRegex.Match(line);
            if (!match.Success)
            {
                // Continuation / unstructured — emit as Debug.
                yield return new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = line,
                    Level = LogLevel.Debug,
                    Category = "App",
                    Source = LogSource.File,
                };
                continue;
            }

            var ts = ParseTimestamp(match.Groups["ts"].Value);
            var msg = match.Groups["msg"].Value;
            var (cleanMsg, category) = ExtractCategory(msg);
            yield return new LogEntry
            {
                Timestamp = ts,
                Message = cleanMsg,
                Level = ClassifyLevel(cleanMsg, isError: false),
                Category = category,
                Source = LogSource.File,
            };
        }
    }

    private static IEnumerable<LogEntry> ParseErrorBlocks(string text)
    {
        var blocks = text.Split(new[] { "================================" }, StringSplitOptions.None);
        foreach (var block in blocks)
        {
            var trimmed = block.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Try to pull the timestamp line from inside the block.
            var ts = DateTime.Now;
            var tsMatch = Regex.Match(trimmed, @"Timestamp:\s*(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})");
            if (tsMatch.Success) ts = ParseTimestamp(tsMatch.Groups["ts"].Value);

            // Pull the context line as the headline; the full block stays as the message body.
            var ctxMatch = Regex.Match(trimmed, @"Error Context:\s*(?<ctx>.*)");
            var headline = ctxMatch.Success ? ctxMatch.Groups["ctx"].Value.Trim() : trimmed.Split('\n')[0];
            var (cleanMsg, category) = ExtractCategory(headline);

            yield return new LogEntry
            {
                Timestamp = ts,
                Message = cleanMsg,
                Detail = trimmed,
                Level = LogLevel.Error,
                Category = string.IsNullOrEmpty(category) ? "Error" : category,
                Source = LogSource.File,
            };
        }
    }

    private static DateTime ParseTimestamp(string s)
        => DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss.fff",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.Now;

    private static (string Message, string Category) ExtractCategory(string message)
    {
        var match = CategoryRegex.Match(message);
        if (!match.Success) return (message, "App");

        var category = match.Groups["cat"].Value.Trim();
        var cleaned = message.Substring(match.Length).TrimStart();
        return (cleaned, category);
    }

    private static LogLevel ClassifyLevel(string message, bool isError)
    {
        if (isError) return LogLevel.Error;
        if (message.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase)) return LogLevel.Info;
        if (message.Contains("error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exception", StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;
        if (message.Contains("warn", StringComparison.OrdinalIgnoreCase)) return LogLevel.Warning;
        return LogLevel.Info;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_coalesceTimer != null)
        {
            _coalesceTimer.Stop();
            _coalesceTimer.Tick -= FlushPending;
            _coalesceTimer = null;
        }

        GC.SuppressFinalize(this);
    }
}
