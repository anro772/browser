using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the network monitor panel.
/// Displays captured network requests with filtering and export capabilities.
/// Uses buffered updates to prevent UI thread overload with high request volume.
/// </summary>
public partial class NetworkMonitorViewModel : ObservableObject, IDisposable
{
    private readonly INetworkLogger _networkLogger;
    private readonly TabStripViewModel _tabStrip;
    private bool _isDisposed;

    // Track the currently subscribed tab's interceptor
    private BrowserTabItem? _subscribedTab;

    // Buffering for high-performance UI updates
    private readonly ConcurrentQueue<NetworkRequest> _pendingRequests = new();
    private readonly DispatcherTimer _updateTimer;
    private const int BatchSize = 50;
    private const int UpdateIntervalMs = 250;

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
        TabStripViewModel tabStrip)
    {
        _networkLogger = networkLogger;
        _tabStrip = tabStrip;

        // Subscribe to active tab changes to wire up per-tab interceptors
        _tabStrip.ActiveTabChanged += OnActiveTabChanged;

        // Wire up the current active tab if one already exists
        if (_tabStrip.ActiveTab != null)
        {
            SubscribeToTab(_tabStrip.ActiveTab);
        }

        // Initialize buffered update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(UpdateIntervalMs)
        };
        _updateTimer.Tick += FlushPendingRequests;
        _updateTimer.Start();

        // Load initial stats from database (fire-and-forget, errors handled internally)
        _ = LoadInitialStatsAsync();
    }

    private void OnActiveTabChanged(object? sender, BrowserTabItem? tab)
    {
        UnsubscribeFromTab();

        if (tab != null)
        {
            SubscribeToTab(tab);
        }
    }

    private void SubscribeToTab(BrowserTabItem tab)
    {
        _subscribedTab = tab;

        if (tab.RequestInterceptor != null)
        {
            tab.RequestInterceptor.RequestCaptured += OnRequestCaptured;
        }
    }

    private void UnsubscribeFromTab()
    {
        if (_subscribedTab?.RequestInterceptor != null)
        {
            _subscribedTab.RequestInterceptor.RequestCaptured -= OnRequestCaptured;
        }
        _subscribedTab = null;
    }

    /// <summary>
    /// Loads initial statistics from the database on startup.
    /// </summary>
    private async Task LoadInitialStatsAsync()
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
            System.Diagnostics.Debug.WriteLine($"Initial stats load error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles a newly captured network request.
    /// Buffers the request for batched UI updates to prevent overwhelming the UI thread.
    /// </summary>
    private void OnRequestCaptured(object? sender, NetworkRequest request)
    {
        if (!IsMonitoringEnabled) return;

        // Add to buffer for batched UI update
        _pendingRequests.Enqueue(request);
    }

    /// <summary>
    /// Flushes pending requests to the UI in batches.
    /// Called by timer every 250ms to provide smooth updates even under high load.
    /// </summary>
    private void FlushPendingRequests(object? sender, EventArgs e)
    {
        if (_pendingRequests.IsEmpty)
            return;

        // Dequeue up to BatchSize requests
        var batch = new List<NetworkRequest>();
        while (batch.Count < BatchSize && _pendingRequests.TryDequeue(out var request))
        {
            batch.Add(request);
        }

        if (batch.Count == 0)
            return;

        // Update UI on background priority to avoid blocking user interactions
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            foreach (var request in batch)
            {
                // Add to top of list
                Requests.Insert(0, request);

                // Update stats
                TotalRequests++;
                if (request.WasBlocked)
                {
                    BlockedCount++;
                }
            }

            // Trim old entries to prevent memory issues
            while (Requests.Count > MaxDisplayedRequests)
            {
                Requests.RemoveAt(Requests.Count - 1);
            }
        }, DispatcherPriority.Background);
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

        var interceptor = _tabStrip.ActiveTab?.RequestInterceptor;
        if (interceptor != null)
        {
            if (IsMonitoringEnabled)
            {
                interceptor.Enable();
            }
            else
            {
                interceptor.Disable();
            }
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
            var url = _tabStrip.ActiveTab?.Url;
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

        // Stop timer and flush any remaining requests
        _updateTimer.Stop();
        _updateTimer.Tick -= FlushPendingRequests;

        // Unsubscribe from active tab events
        _tabStrip.ActiveTabChanged -= OnActiveTabChanged;
        UnsubscribeFromTab();

        GC.SuppressFinalize(this);
    }
}
