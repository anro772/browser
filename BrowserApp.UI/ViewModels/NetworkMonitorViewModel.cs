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
    private readonly IBlockingService _blockingService;
    private bool _isDisposed;

    // DB-historical baseline (loaded once at startup). Live counters = baseline + BlockingService session counts.
    private int _baselineTotal;
    private int _baselineBlocked;
    private long _baselineBytes;

    // Track the currently subscribed tab's interceptor
    private BrowserTabItem? _subscribedTab;

    // Buffering for high-performance UI updates
    private readonly ConcurrentQueue<NetworkRequest> _pendingRequests = new();
    private readonly DispatcherTimer _updateTimer;
    private const int BatchSize = 50;
    private const int UpdateIntervalMs = 250;

    // Running total of bytes saved from blocked requests (seeded from DB on startup)
    private long _totalBytesSaved;

    /// <summary>Live, sidebar-scoped list — trimmed to the last <see cref="MaxDisplayedRequests"/> for fast rendering in the narrow panel.</summary>
    [ObservableProperty]
    private ObservableCollection<NetworkRequest> _requests = new();

    /// <summary>Full history list — populated on demand by the expanded modal via <see cref="LoadAllRequestsAsyncCommand"/>.</summary>
    [ObservableProperty]
    private ObservableCollection<NetworkRequest> _allRequests = new();

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

    /// <summary>Search filter applied to <see cref="AllRequests"/> in the expanded modal — matches URL/host substring.</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>Number of rows currently visible in <see cref="AllRequests"/> after the search filter — used by the modal footer.</summary>
    [ObservableProperty]
    private int _filteredAllRequestsCount;

    private const int MaxDisplayedRequests = 100;
    private const int MaxAllRequestsRows = 5000;

    public NetworkMonitorViewModel(
        INetworkLogger networkLogger,
        TabStripViewModel tabStrip,
        IBlockingService blockingService)
    {
        _networkLogger = networkLogger;
        _tabStrip = tabStrip;
        _blockingService = blockingService;

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
    /// Loads initial statistics from the database on startup. The DB totals become the baseline
    /// that we add the live BlockingService session counters onto — that way the dashboard and
    /// the monitor track the same source of truth for the per-session delta.
    /// </summary>
    private async Task LoadInitialStatsAsync()
    {
        try
        {
            var stats = await _networkLogger.GetStatsAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _baselineTotal = stats.TotalRequests;
                _baselineBlocked = stats.BlockedRequests;
                _baselineBytes = stats.TotalBytes;
                _totalBytesSaved = stats.TotalBytes;
                SyncCountersFromBlockingService();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initial stats load error: {ex.Message}");
        }
    }

    /// <summary>
    /// Pulls the live session counters from <see cref="IBlockingService"/> and adds them to the
    /// DB-historical baseline. Called on the dispatcher whenever the displayed numbers should refresh.
    /// </summary>
    private void SyncCountersFromBlockingService()
    {
        TotalRequests = _baselineTotal + _blockingService.GetDetectedCount();
        BlockedCount = _baselineBlocked + _blockingService.GetBlockedCount();
        var bytes = _baselineBytes + _blockingService.GetBytesSaved();
        _totalBytesSaved = bytes;
        DataSaved = FormatBytes(bytes);
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
    /// Called by timer every 250ms. Always re-syncs counters from BlockingService so the
    /// header reflects blocks happening on other tabs even when the active-tab interceptor
    /// has no pending events.
    /// </summary>
    private void FlushPendingRequests(object? sender, EventArgs e)
    {
        var batch = new List<NetworkRequest>();
        while (batch.Count < BatchSize && _pendingRequests.TryDequeue(out var request))
        {
            batch.Add(request);
        }

        // Update UI on background priority to avoid blocking user interactions
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            foreach (var request in batch)
            {
                // Sidebar: trimmed live view.
                Requests.Insert(0, request);
                // Expanded modal: full live view (capped at MaxAllRequestsRows so memory doesn't grow unbounded).
                AllRequests.Insert(0, request);
            }

            // Stats now come from BlockingService (singleton, sees every block across all tabs),
            // not from this VM's per-tab interceptor subscription. That keeps the network monitor
            // and the privacy dashboard in lockstep — both read the same counters.
            SyncCountersFromBlockingService();

            // Trim sidebar list aggressively — it's narrow and we want snappy redraws.
            while (Requests.Count > MaxDisplayedRequests)
            {
                Requests.RemoveAt(Requests.Count - 1);
            }
            // Trim expanded list loosely — it's a roomy modal but we still want a memory ceiling.
            while (AllRequests.Count > MaxAllRequestsRows)
            {
                AllRequests.RemoveAt(AllRequests.Count - 1);
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Loads the full request history from the database into <see cref="AllRequests"/>.
    /// Called by the expanded monitor on open so the modal shows everything (the sidebar's
    /// <see cref="Requests"/> is capped at the most recent 100 for performance).
    /// </summary>
    [RelayCommand]
    private async Task LoadAllRequestsAsync()
    {
        await ApplyFilterToAllAsync(SelectedFilter.ToString());
    }

    /// <summary>
    /// Applies the selected filter to <see cref="AllRequests"/> only — used by the expanded
    /// modal so filter chips there don't disturb the sidebar's live <see cref="Requests"/> stream.
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterToAllAsync(string filterName)
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
                AllRequests.Clear();
                foreach (var request in filtered.Take(MaxAllRequestsRows))
                {
                    AllRequests.Add(request);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Apply modal filter error: {ex.Message}");
        }
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
            _blockingService.ResetStats(); // Keep the dashboard counters in lockstep.

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _baselineTotal = 0;
                _baselineBlocked = 0;
                _baselineBytes = 0;
                _totalBytesSaved = 0;
                Requests.Clear();
                AllRequests.Clear();
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
    /// Opens the expanded Network Monitor window. Reuses this VM as the window's DataContext.
    /// </summary>
    [RelayCommand]
    private void OpenExpandedMonitor()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var window = new Views.NetworkMonitorExpandedView
            {
                DataContext = this,
                Owner = Application.Current.MainWindow
            };
            window.Show();
        });
    }

    /// <summary>
    /// Refreshes the DB-historical baseline, then re-syncs the displayed counters from the
    /// BlockingService session counters.
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatsAsync()
    {
        try
        {
            var stats = await _networkLogger.GetStatsAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _baselineTotal = stats.TotalRequests;
                _baselineBlocked = stats.BlockedRequests;
                _baselineBytes = stats.TotalBytes;
                SyncCountersFromBlockingService();
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

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
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
