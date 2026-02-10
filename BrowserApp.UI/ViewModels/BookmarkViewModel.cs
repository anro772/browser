using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for bookmark management.
/// Handles bookmark toggle, list display, and navigation to bookmarks.
/// </summary>
public partial class BookmarkViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TabStripViewModel _tabStrip;

    [ObservableProperty]
    private bool _isCurrentPageBookmarked;

    public ObservableCollection<BookmarkEntity> Bookmarks { get; } = new();

    public BookmarkViewModel(
        IServiceScopeFactory scopeFactory,
        TabStripViewModel tabStrip)
    {
        _scopeFactory = scopeFactory;
        _tabStrip = tabStrip;

        // Monitor active tab changes to update bookmark state
        _tabStrip.ActiveTabChanged += async (s, tab) =>
        {
            if (tab != null)
            {
                tab.SourceChanged += async (s2, url) => await CheckBookmarkStateAsync(url);
                await CheckBookmarkStateAsync(tab.Url);
            }
        };
    }

    [RelayCommand]
    public async Task LoadBookmarksAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBookmarkRepository>();
            var bookmarks = await repo.GetAllAsync();

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Bookmarks.Clear();
                foreach (var b in bookmarks)
                {
                    Bookmarks.Add(b);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bookmarks] Load error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles bookmark for the current page.
    /// </summary>
    [RelayCommand]
    public async Task ToggleBookmarkAsync()
    {
        var tab = _tabStrip.ActiveTab;
        if (tab == null || string.IsNullOrEmpty(tab.Url)) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBookmarkRepository>();

            if (IsCurrentPageBookmarked)
            {
                // Remove bookmark
                var existing = await repo.GetByUrlAsync(tab.Url);
                if (existing != null)
                {
                    await repo.RemoveAsync(existing.Id);
                }
                IsCurrentPageBookmarked = false;
            }
            else
            {
                // Add bookmark
                await repo.AddAsync(new BookmarkEntity
                {
                    Url = tab.Url,
                    Title = tab.Title ?? tab.Url,
                    CreatedAt = DateTime.UtcNow
                });
                IsCurrentPageBookmarked = true;
            }

            await LoadBookmarksAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bookmarks] Toggle error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void NavigateToBookmark(BookmarkEntity? bookmark)
    {
        if (bookmark == null) return;

        var tab = _tabStrip.ActiveTab;
        if (tab != null)
        {
            tab.Navigate(bookmark.Url);
        }
    }

    [RelayCommand]
    private async Task RemoveBookmarkAsync(BookmarkEntity? bookmark)
    {
        if (bookmark == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBookmarkRepository>();
            await repo.RemoveAsync(bookmark.Id);
            await LoadBookmarksAsync();

            // Update button state if we removed the current page
            var tab = _tabStrip.ActiveTab;
            if (tab != null)
            {
                await CheckBookmarkStateAsync(tab.Url);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bookmarks] Remove error: {ex.Message}");
        }
    }

    private async Task CheckBookmarkStateAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            IsCurrentPageBookmarked = false;
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBookmarkRepository>();
            var exists = await repo.ExistsAsync(url);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsCurrentPageBookmarked = exists;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bookmarks] Check error: {ex.Message}");
        }
    }
}
