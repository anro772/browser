using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the privacy dashboard panel.
/// Displays privacy stats, top blocked domains, and resource type breakdown.
/// </summary>
public partial class PrivacyDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SettingsService _settingsService;
    private readonly IBlockingService _blockingService;
    private readonly DispatcherTimer? _refreshTimer;
    private readonly EventHandler<PrivacyMode> _privacyModeChangedHandler;
    private readonly EventHandler<NetworkRequest> _requestBlockedHandler;
    private bool _disposed;

    [ObservableProperty]
    private PrivacyMode _currentPrivacyMode = PrivacyMode.Standard;

    [ObservableProperty]
    private int _blockedThisSession;

    /// <summary>
    /// Total requests evaluated this session (blocked + allowed). Sourced from
    /// <see cref="IBlockingService.GetDetectedCount"/> so it matches what the network
    /// monitor sees — a discrepancy here indicates a logging/event-routing bug.
    /// </summary>
    [ObservableProperty]
    private int _detectedThisSession;

    [ObservableProperty]
    private string _dataSaved = "0 B";

    [ObservableProperty]
    private int _totalBlocked;

    [ObservableProperty]
    private ObservableCollection<BlockedDomainItem> _topBlockedDomains = new();

    [ObservableProperty]
    private ObservableCollection<ResourceTypeItem> _resourceTypeBreakdown = new();

    [ObservableProperty]
    private bool _isLoading;

    public PrivacyDashboardViewModel(IServiceScopeFactory scopeFactory, SettingsService settingsService, IBlockingService blockingService)
    {
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;
        _blockingService = blockingService;

        // Load current privacy mode from settings
        CurrentPrivacyMode = _settingsService.PrivacyMode;

        // Debounced refresh: collapses bursts of blocking events into one DB query 500ms after the last block.
        // Only created when an Application is alive (skip in unit tests where there's no WPF dispatcher).
        if (Application.Current != null)
        {
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _refreshTimer.Tick += OnRefreshTimerTick;
        }

        _privacyModeChangedHandler = (_, mode) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentPrivacyMode = mode;
                OnPropertyChanged(nameof(PrivacyModeDisplay));
                OnPropertyChanged(nameof(PrivacyModeDescription));
                OnPropertyChanged(nameof(PrivacyModeColor));
            });
            ScheduleRefresh();
        };
        _requestBlockedHandler = (_, _) => ScheduleRefresh();

        _settingsService.PrivacyModeChanged += _privacyModeChangedHandler;
        _blockingService.RequestBlocked += _requestBlockedHandler;

        // Populate DB-backed all-time stats immediately so the dashboard isn't blank
        // before the first RequestBlocked event of this session fires. Guarded the same
        // way as the timer so unit tests (no WPF Application) skip this.
        if (Application.Current != null)
        {
            _ = RefreshStatsAsync();
        }
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        _refreshTimer?.Stop();
        _ = RefreshStatsAsync();
    }

    /// <summary>
    /// Restarts the debounced refresh timer. Called whenever a blocking event arrives;
    /// repeated calls within the interval window collapse to a single refresh.
    /// </summary>
    private void ScheduleRefresh()
    {
        if (_refreshTimer == null) return;
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }, DispatcherPriority.Background);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _settingsService.PrivacyModeChanged -= _privacyModeChangedHandler;
        _blockingService.RequestBlocked -= _requestBlockedHandler;

        if (_refreshTimer != null)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTimerTick;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Refreshes all dashboard statistics.
    /// </summary>
    [RelayCommand]
    public async Task RefreshStatsAsync()
    {
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var networkLogRepository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();

            // Get stats in parallel
            var totalBlockedTask = networkLogRepository.GetBlockedCountAsync();
            var dataSavedTask = networkLogRepository.GetTotalSizeAsync();
            var topDomainsTask = networkLogRepository.GetTopBlockedDomainsAsync(5);
            var resourceTypesTask = networkLogRepository.GetResourceTypeBreakdownAsync();

            await Task.WhenAll(totalBlockedTask, dataSavedTask, topDomainsTask, resourceTypesTask);

            var totalBlocked = await totalBlockedTask;
            var totalBytes = await dataSavedTask;
            var topDomains = await topDomainsTask;
            var resourceTypes = await resourceTypesTask;

            // Use BlockingService in-memory counters for accurate session stats
            var sessionBlocked = _blockingService.GetBlockedCount();
            var sessionDetected = _blockingService.GetDetectedCount();
            var sessionBytes = _blockingService.GetBytesSaved();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                BlockedThisSession = sessionBlocked;
                DetectedThisSession = sessionDetected;
                TotalBlocked = totalBlocked;
                DataSaved = FormatBytes(sessionBytes);

                // Update top blocked domains
                TopBlockedDomains.Clear();
                var maxCount = topDomains.Count > 0 ? topDomains[0].Count : 0;
                foreach (var (domain, count) in topDomains)
                {
                    TopBlockedDomains.Add(new BlockedDomainItem
                    {
                        Domain = domain,
                        Count = count,
                        Percentage = maxCount > 0 ? (double)count / maxCount * 100 : 0
                    });
                }

                // Update resource type breakdown
                ResourceTypeBreakdown.Clear();
                var totalRequests = resourceTypes.Sum(r => r.Count);
                foreach (var (type, count) in resourceTypes.Take(6))
                {
                    ResourceTypeBreakdown.Add(new ResourceTypeItem
                    {
                        Type = type,
                        Count = count,
                        Percentage = totalRequests > 0 ? (double)count / totalRequests * 100 : 0
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard refresh error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets the display name for the current privacy mode.
    /// </summary>
    public string PrivacyModeDisplay => CurrentPrivacyMode switch
    {
        PrivacyMode.Relaxed => "Relaxed",
        PrivacyMode.Standard => "Standard",
        PrivacyMode.Strict => "Strict",
        _ => "Standard"
    };

    /// <summary>
    /// Gets the description for the current privacy mode.
    /// </summary>
    public string PrivacyModeDescription => CurrentPrivacyMode switch
    {
        PrivacyMode.Relaxed => "User rules only — templates and channels skipped",
        PrivacyMode.Standard => "All enabled rules apply",
        PrivacyMode.Strict => "All rules + built-in tracker blocklist",
        _ => "Balanced blocking"
    };

    /// <summary>
    /// Gets the icon color for the current privacy mode.
    /// </summary>
    public string PrivacyModeColor => CurrentPrivacyMode switch
    {
        PrivacyMode.Relaxed => "#FFC107",  // Yellow
        PrivacyMode.Standard => "#107C10", // Green
        PrivacyMode.Strict => "#D13438",   // Red
        _ => "#107C10"
    };

    /// <summary>
    /// Resets all network statistics (clears logs from database).
    /// </summary>
    [RelayCommand]
    private async Task ResetStatsAsync()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to reset all network statistics? This will clear all logged requests.",
            "Reset Statistics",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var networkLogRepository = scope.ServiceProvider.GetRequiredService<INetworkLogRepository>();
                await networkLogRepository.ClearAllAsync();

                // Refresh stats to show zeroed values
                await RefreshStatsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset stats error: {ex.Message}");
            }
        }
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
}

/// <summary>
/// Represents a blocked domain for display in the dashboard.
/// </summary>
public class BlockedDomainItem
{
    public string Domain { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Represents a resource type for display in the dashboard.
/// </summary>
public class ResourceTypeItem
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
