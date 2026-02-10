using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the privacy dashboard panel.
/// Displays privacy stats, top blocked domains, and resource type breakdown.
/// </summary>
public partial class PrivacyDashboardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private PrivacyMode _currentPrivacyMode = PrivacyMode.Standard;

    [ObservableProperty]
    private int _blockedToday;

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

    public PrivacyDashboardViewModel(IServiceScopeFactory scopeFactory, SettingsService settingsService)
    {
        _scopeFactory = scopeFactory;
        _settingsService = settingsService;

        // Load current privacy mode from settings
        CurrentPrivacyMode = _settingsService.PrivacyMode;

        // Subscribe to privacy mode changes
        _settingsService.PrivacyModeChanged += (s, mode) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentPrivacyMode = mode;
                OnPropertyChanged(nameof(PrivacyModeDisplay));
                OnPropertyChanged(nameof(PrivacyModeDescription));
                OnPropertyChanged(nameof(PrivacyModeColor));
            });
        };
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
            var blockedTodayTask = networkLogRepository.GetBlockedTodayCountAsync();
            var totalBlockedTask = networkLogRepository.GetBlockedCountAsync();
            var dataSavedTask = networkLogRepository.GetTotalSizeAsync();
            var topDomainsTask = networkLogRepository.GetTopBlockedDomainsAsync(5);
            var resourceTypesTask = networkLogRepository.GetResourceTypeBreakdownAsync();

            await Task.WhenAll(blockedTodayTask, totalBlockedTask, dataSavedTask, topDomainsTask, resourceTypesTask);

            var blockedToday = await blockedTodayTask;
            var totalBlocked = await totalBlockedTask;
            var dataSaved = await dataSavedTask;
            var topDomains = await topDomainsTask;
            var resourceTypes = await resourceTypesTask;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                BlockedToday = blockedToday;
                TotalBlocked = totalBlocked;
                DataSaved = FormatBytes(dataSaved);

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
        PrivacyMode.Relaxed => "Minimal blocking - sites work best",
        PrivacyMode.Standard => "Balanced - recommended",
        PrivacyMode.Strict => "Maximum blocking - may break sites",
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
