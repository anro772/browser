using System;
using System.IO;

namespace BrowserApp.UI;

/// <summary>
/// Simple error logger that writes to a dedicated log directory.
/// </summary>
public static class ErrorLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp",
        "Logs");

    private static readonly object _lock = new object();

    static ErrorLogger()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
        }
        catch
        {
            // If we can't create log directory, we'll fail silently
        }
    }

    public static void LogError(string context, Exception ex)
    {
        try
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                var fileName = $"error_{timestamp}.txt";
                var filePath = Path.Combine(LogDirectory, fileName);

                var logContent = $@"Error Context: {context}
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
Error Message: {ex.Message}
Exception Type: {ex.GetType().FullName}

Inner Exception: {ex.InnerException?.Message ?? "None"}
Inner Exception Type: {ex.InnerException?.GetType().FullName ?? "None"}

Stack Trace:
{ex.StackTrace}

{(ex.InnerException != null ? $@"
Inner Stack Trace:
{ex.InnerException.StackTrace}
" : "")}
================================
";

                File.WriteAllText(filePath, logContent);

                // Also append to a consolidated log
                var consolidatedLog = Path.Combine(LogDirectory, $"errors_{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(consolidatedLog, logContent);
            }
        }
        catch
        {
            // If logging fails, we can't do much about it
        }
    }

    public static string GetLogDirectory() => LogDirectory;

    public static void LogInfo(string message)
    {
        try
        {
            lock (_lock)
            {
                var consolidatedLog = Path.Combine(LogDirectory, $"info_{DateTime.Now:yyyy-MM-dd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
                File.AppendAllText(consolidatedLog, logEntry);
            }
        }
        catch
        {
            // If logging fails, we can't do much about it
        }
    }
}
