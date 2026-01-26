using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Models;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the privacy dashboard panel.
/// Displays privacy stats, top blocked domains, and resource type breakdown.
/// </summary>
public partial class PrivacyDashboardViewModel : ObservableObject
{
    private readonly INetworkLogRepository _networkLogRepository;

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

    public PrivacyDashboardViewModel(INetworkLogRepository networkLogRepository)
    {
        _networkLogRepository = networkLogRepository;
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
            // Get stats in parallel
            var blockedTodayTask = _networkLogRepository.GetBlockedTodayCountAsync();
            var totalBlockedTask = _networkLogRepository.GetBlockedCountAsync();
            var dataSavedTask = _networkLogRepository.GetTotalSizeAsync();
            var topDomainsTask = _networkLogRepository.GetTopBlockedDomainsAsync(5);
            var resourceTypesTask = _networkLogRepository.GetResourceTypeBreakdownAsync();

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
                var maxCount = topDomains.FirstOrDefault().Count;
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
    /// Gets the icon color for the current privacy mode.
    /// </summary>
    public string PrivacyModeColor => CurrentPrivacyMode switch
    {
        PrivacyMode.Relaxed => "#FFC107",  // Yellow
        PrivacyMode.Standard => "#107C10", // Green
        PrivacyMode.Strict => "#D13438",   // Red
        _ => "#107C10"
    };

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
