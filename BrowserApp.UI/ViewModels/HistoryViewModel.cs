using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Core.Interfaces;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the browsing history panel.
/// Displays visited pages with search and navigation capabilities.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IBrowsingHistoryRepository _historyRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<BrowsingHistoryEntity> _historyEntries = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private BrowsingHistoryEntity? _selectedEntry;

    private const int MaxDisplayedEntries = 200;

    public HistoryViewModel(
        IBrowsingHistoryRepository historyRepository,
        INavigationService navigationService)
    {
        _historyRepository = historyRepository;
        _navigationService = navigationService;
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
            var entries = await _historyRepository.GetRecentAsync(MaxDisplayedEntries);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                HistoryEntries.Clear();
                foreach (var entry in entries)
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
            IEnumerable<BrowsingHistoryEntity> entries;

            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                entries = await _historyRepository.GetRecentAsync(MaxDisplayedEntries);
            }
            else
            {
                entries = await _historyRepository.SearchAsync(SearchQuery);
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                HistoryEntries.Clear();
                foreach (var entry in entries)
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
    /// Navigates to the selected history entry.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToEntryAsync(BrowsingHistoryEntity? entry)
    {
        if (entry == null) return;

        await _navigationService.NavigateAsync(entry.Url);
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
                await _historyRepository.ClearAllAsync();

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
