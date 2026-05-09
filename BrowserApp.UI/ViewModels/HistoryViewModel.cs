using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the browsing history panel.
/// Displays visited pages with search and navigation capabilities.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TabStripViewModel _tabStrip;

    [ObservableProperty]
    private ObservableCollection<HistoryEntryDisplay> _historyEntries = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private HistoryEntryDisplay? _selectedEntry;

    /// <summary>
    /// Grouped/sorted view bound to the ListView so the XAML can render day section headers
    /// without us materializing nested collections by hand.
    /// </summary>
    public ICollectionView GroupedHistory { get; }

    private const int MaxDisplayedEntries = 200;

    public HistoryViewModel(
        IServiceScopeFactory scopeFactory,
        TabStripViewModel tabStrip)
    {
        _scopeFactory = scopeFactory;
        _tabStrip = tabStrip;

        GroupedHistory = CollectionViewSource.GetDefaultView(HistoryEntries);
        GroupedHistory.GroupDescriptions.Add(new PropertyGroupDescription(nameof(HistoryEntryDisplay.GroupLabel)));
    }

    /// <summary>
    /// Materializes display wrappers from raw entities, deduping consecutive identical URLs
    /// so the sidebar doesn't show a wall of repeats when a page reload bumps the history.
    /// </summary>
    private static IEnumerable<HistoryEntryDisplay> Project(IEnumerable<BrowsingHistoryEntity> source)
    {
        string? lastUrl = null;
        foreach (var entity in source.OrderByDescending(e => e.VisitedAt))
        {
            if (string.Equals(entity.Url, lastUrl, StringComparison.OrdinalIgnoreCase))
                continue;
            lastUrl = entity.Url;
            yield return new HistoryEntryDisplay(entity);
        }
    }

    /// <summary>
    /// Loads recent history entries.
    /// </summary>
    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
            var entries = await historyRepository.GetRecentAsync(MaxDisplayedEntries);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                HistoryEntries.Clear();
                foreach (var entry in Project(entries))
                {
                    HistoryEntries.Add(entry);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"History load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Searches history entries by URL or title.
    /// </summary>
    [RelayCommand]
    private async Task SearchHistoryAsync()
    {
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();

            IEnumerable<BrowsingHistoryEntity> entries;

            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                entries = await historyRepository.GetRecentAsync(MaxDisplayedEntries);
            }
            else
            {
                entries = await historyRepository.SearchAsync(SearchQuery);
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                HistoryEntries.Clear();
                foreach (var entry in Project(entries))
                {
                    HistoryEntries.Add(entry);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"History search error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Navigates to the selected history entry in the active tab.
    /// </summary>
    [RelayCommand]
    private Task NavigateToEntryAsync(HistoryEntryDisplay? entry)
    {
        if (entry == null) return Task.CompletedTask;

        _tabStrip.ActiveTab?.Navigate(entry.Url);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all browsing history.
    /// </summary>
    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all browsing history?",
            "Clear History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var historyRepository = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
                await historyRepository.ClearAllAsync();

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    HistoryEntries.Clear();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"History clear error: {ex.Message}");
                MessageBox.Show(
                    $"Failed to clear history: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Called when search query changes - triggers search with debounce handled by UI.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // Fire-and-forget search when query changes
        _ = SearchHistoryAsync();
    }
}
