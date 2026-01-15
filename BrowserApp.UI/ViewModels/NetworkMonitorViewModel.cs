using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the network monitor panel.
/// Displays captured network requests with filtering and export capabilities.
/// </summary>
public partial class NetworkMonitorViewModel : ObservableObject, IDisposable
{
    private readonly INetworkLogger _networkLogger;
    private readonly IRequestInterceptor _requestInterceptor;
    private readonly INavigationService _navigationService;
    private bool _isDisposed;

    [ObservableProperty]
    private ObservableCollection<NetworkRequest> _requests = new();

    [ObservableProperty]
    private NetworkRequestFilter _selectedFilter = NetworkRequestFilter.All;

    [ObservableProperty]
    private int _totalRequests;

    [ObservableProperty]
    private int _blockedCount;

    [ObservableProperty]
    private string _dataSaved = "0 B";

    [ObservableProperty]
    private bool _isMonitoringEnabled = true;

    [ObservableProperty]
    private NetworkRequest? _selectedRequest;

    private const int MaxDisplayedRequests = 500;

    public NetworkMonitorViewModel(
        INetworkLogger networkLogger,
        IRequestInterceptor requestInterceptor,
        INavigationService navigationService)
    {
        _networkLogger = networkLogger;
        _requestInterceptor = requestInterceptor;
        _navigationService = navigationService;

        // Subscribe to captured requests
        _requestInterceptor.RequestCaptured += OnRequestCaptured;
    }

    /// <summary>
    /// Handles a newly captured network request.
    /// </summary>
    private void OnRequestCaptured(object? sender, NetworkRequest request)
    {
        if (!IsMonitoringEnabled) return;

        // Log to database (async, non-blocking)
        _ = _networkLogger.LogRequestAsync(request);

        // Update UI on dispatcher thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // Add to top of list
            Requests.Insert(0, request);

            // Trim old entries to prevent memory issues
            while (Requests.Count > MaxDisplayedRequests)
            {
                Requests.RemoveAt(Requests.Count - 1);
            }

            // Update stats
            TotalRequests++;
            if (request.WasBlocked)
            {
                BlockedCount++;
            }
        });
    }

    /// <summary>
    /// Applies the selected filter to the request list.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync(string filterName)
    {
        if (Enum.TryParse<NetworkRequestFilter>(filterName, out var filter))
        {
            SelectedFilter = filter;
        }

        try
        {
            var currentHost = GetCurrentPageHost();
            var filtered = await _networkLogger.GetFilteredRequestsAsync(SelectedFilter, currentHost);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Requests.Clear();
                foreach (var request in filtered.Take(MaxDisplayedRequests))
                {
                    Requests.Add(request);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports the current request list to a CSV file.
    /// </summary>
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = $"network-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Title = "Export Network Log"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await ExportToCsvFileAsync(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to export: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Clears all logged requests.
    /// </summary>
    [RelayCommand]
    private async Task ClearAllAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all network logs?",
            "Clear Network Logs",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _networkLogger.ClearAllAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Requests.Clear();
                TotalRequests = 0;
                BlockedCount = 0;
                DataSaved = "0 B";
            });
        }
    }

    /// <summary>
    /// Toggles monitoring on/off.
    /// </summary>
    [RelayCommand]
    private void ToggleMonitoring()
    {
        IsMonitoringEnabled = !IsMonitoringEnabled;

        if (IsMonitoringEnabled)
        {
            _requestInterceptor.Enable();
        }
        else
        {
            _requestInterceptor.Disable();
        }
    }

    /// <summary>
    /// Refreshes stats from the database.
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatsAsync()
    {
        try
        {
            var stats = await _networkLogger.GetStatsAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                TotalRequests = stats.TotalRequests;
                BlockedCount = stats.BlockedRequests;
                DataSaved = stats.FormattedDataSaved;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stats refresh error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current page host for third-party detection.
    /// </summary>
    private string? GetCurrentPageHost()
    {
        try
        {
            var url = _navigationService.CurrentUrl;
            if (string.IsNullOrEmpty(url)) return null;

            return new Uri(url).Host;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Exports requests to a CSV file.
    /// </summary>
    private async Task ExportToCsvFileAsync(string filePath)
    {
        var lines = new List<string>
        {
            "Timestamp,URL,Method,Status,Type,ContentType,Size,Blocked,BlockedBy"
        };

        foreach (var request in Requests)
        {
            var line = string.Join(",",
                EscapeCsv(request.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(request.Url),
                EscapeCsv(request.Method),
                request.StatusCode?.ToString() ?? "",
                EscapeCsv(request.ResourceType),
                EscapeCsv(request.ContentType ?? ""),
                request.Size?.ToString() ?? "",
                request.WasBlocked ? "Yes" : "No",
                EscapeCsv(request.BlockedByRuleId ?? "")
            );
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }

    /// <summary>
    /// Escapes a value for CSV format.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _requestInterceptor.RequestCaptured -= OnRequestCaptured;

        GC.SuppressFinalize(this);
    }
}
