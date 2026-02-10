using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.UI.Models;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel managing the browser tab strip.
/// Holds the shared CoreWebView2Environment and manages tab lifecycle.
/// </summary>
public partial class TabStripViewModel : ObservableObject, IDisposable
{
    private readonly IBlockingService _blockingService;
    private readonly IRuleEngine _ruleEngine;
    private readonly ISearchEngineService _searchEngineService;
    private CoreWebView2Environment? _environment;
    private bool _isDisposed;

    public ObservableCollection<BrowserTabItem> Tabs { get; } = new();

    [ObservableProperty]
    private BrowserTabItem? _activeTab;

    /// <summary>
    /// Fired when the active tab changes. MainWindow uses this to swap visible WebView2.
    /// </summary>
    public event EventHandler<BrowserTabItem?>? ActiveTabChanged;

    /// <summary>
    /// Fired when a tab is added. MainWindow uses this to add its WebView2 to the visual tree.
    /// </summary>
    public event EventHandler<BrowserTabItem>? TabAdded;

    /// <summary>
    /// Fired when a tab is removed. MainWindow uses this to remove its WebView2 from the visual tree.
    /// </summary>
    public event EventHandler<BrowserTabItem>? TabRemoved;

    /// <summary>
    /// Fired after a tab's CoreWebView2 is fully initialized (interceptor, injectors ready).
    /// MainWindow uses this to wire network logging and download notifications.
    /// </summary>
    public event EventHandler<BrowserTabItem>? TabReady;

    public TabStripViewModel(
        IBlockingService blockingService,
        IRuleEngine ruleEngine,
        ISearchEngineService searchEngineService)
    {
        _blockingService = blockingService;
        _ruleEngine = ruleEngine;
        _searchEngineService = searchEngineService;
    }

    /// <summary>
    /// Initializes the shared WebView2 environment. Must be called once before creating tabs.
    /// </summary>
    public async Task InitializeAsync(string userDataFolder)
    {
        _environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);
    }

    /// <summary>
    /// Creates a new tab and optionally navigates it to a URL.
    /// Two-phase init: WebView2 must be in visual tree before CoreWebView2 can initialize.
    /// </summary>
    [RelayCommand]
    public async Task NewTabAsync(string? url = null)
    {
        if (_environment == null) return;

        var tab = new BrowserTabItem();

        // Phase 1: Create the WebView2 control
        tab.CreateWebView();

        // Add to collection and visual tree BEFORE initializing CoreWebView2
        // (WebView2 needs an HWND from being in the visual tree)
        Tabs.Add(tab);
        TabAdded?.Invoke(this, tab);

        // Phase 2: Initialize CoreWebView2 (now that WebView2 has an HWND)
        await tab.InitializeCoreAsync(_environment, _blockingService, _ruleEngine);

        // Signal that the tab is fully ready (interceptor, CoreWebView2 available)
        TabReady?.Invoke(this, tab);

        ActivateTab(tab);

        if (!string.IsNullOrEmpty(url))
        {
            tab.Navigate(url);
        }
    }

    /// <summary>
    /// Closes a specific tab. If it's the last tab, creates a new empty tab first.
    /// </summary>
    [RelayCommand]
    public async Task CloseTabAsync(BrowserTabItem? tab)
    {
        if (tab == null) return;

        var index = Tabs.IndexOf(tab);
        if (index < 0) return;

        // If closing the last tab, create a new one first
        if (Tabs.Count == 1)
        {
            await NewTabAsync();
        }

        // Activate adjacent tab if closing the active one
        if (tab.IsActive)
        {
            var newIndex = index > 0 ? index - 1 : 0;
            if (Tabs.Count > 1 && Tabs[newIndex] != tab)
            {
                ActivateTab(Tabs[newIndex]);
            }
            else if (Tabs.Count > 1)
            {
                ActivateTab(Tabs[newIndex == 0 ? 1 : 0]);
            }
        }

        Tabs.Remove(tab);
        TabRemoved?.Invoke(this, tab);
        tab.Dispose();
    }

    /// <summary>
    /// Closes the currently active tab.
    /// </summary>
    [RelayCommand]
    public async Task CloseActiveTabAsync()
    {
        if (ActiveTab != null)
        {
            await CloseTabAsync(ActiveTab);
        }
    }

    /// <summary>
    /// Activates a specific tab, deactivating all others.
    /// </summary>
    [RelayCommand]
    public void ActivateTab(BrowserTabItem? tab)
    {
        if (tab == null || !Tabs.Contains(tab)) return;

        foreach (var t in Tabs)
        {
            t.IsActive = t == tab;
        }

        ActiveTab = tab;
        ActiveTabChanged?.Invoke(this, tab);
    }

    /// <summary>
    /// Switches to the next tab (wraps around).
    /// </summary>
    [RelayCommand]
    public void NextTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;

        var index = Tabs.IndexOf(ActiveTab);
        var nextIndex = (index + 1) % Tabs.Count;
        ActivateTab(Tabs[nextIndex]);
    }

    /// <summary>
    /// Switches to the previous tab (wraps around).
    /// </summary>
    [RelayCommand]
    public void PreviousTab()
    {
        if (Tabs.Count <= 1 || ActiveTab == null) return;

        var index = Tabs.IndexOf(ActiveTab);
        var prevIndex = (index - 1 + Tabs.Count) % Tabs.Count;
        ActivateTab(Tabs[prevIndex]);
    }

    /// <summary>
    /// Saves current tab session to the database.
    /// </summary>
    public async Task SaveSessionAsync(IServiceScopeFactory scopeFactory)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();

            // Clear existing session
            var existing = await db.TabSessions.ToListAsync();
            db.TabSessions.RemoveRange(existing);

            // Bug 7: Take a snapshot to avoid collection-modified exception
            var snapshot = Tabs.ToList();
            for (int i = 0; i < snapshot.Count; i++)
            {
                var tab = snapshot[i];
                if (!string.IsNullOrEmpty(tab.Url))
                {
                    db.TabSessions.Add(new TabSessionEntity
                    {
                        Url = tab.Url,
                        Title = tab.Title,
                        TabOrder = i,
                        IsActive = tab.IsActive,
                        SavedAt = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[TabStrip] Session saved: {Tabs.Count} tabs");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabStrip] Failed to save session: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores tab session from the database. Returns true if session was restored.
    /// </summary>
    public async Task<bool> RestoreSessionAsync(IServiceScopeFactory scopeFactory)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();

            var sessions = await db.TabSessions
                .OrderBy(s => s.TabOrder)
                .ToListAsync();

            if (sessions.Count == 0)
                return false;

            BrowserTabItem? activeTab = null;

            foreach (var session in sessions)
            {
                // Bug 12: Validate URL before restoring
                if (string.IsNullOrWhiteSpace(session.Url) ||
                    !Uri.TryCreate(session.Url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    continue;
                }

                await NewTabAsync(session.Url);

                var tab = Tabs.LastOrDefault();
                if (tab != null && session.IsActive)
                {
                    activeTab = tab;
                }
            }

            if (activeTab != null)
            {
                ActivateTab(activeTab);
            }

            System.Diagnostics.Debug.WriteLine($"[TabStrip] Session restored: {sessions.Count} tabs");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabStrip] Failed to restore session: {ex.Message}");
            return false;
        }
    }

    partial void OnActiveTabChanged(BrowserTabItem? value)
    {
        OnPropertyChanged(nameof(ActiveTab));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var tab in Tabs)
        {
            tab.Dispose();
        }
        Tabs.Clear();

        GC.SuppressFinalize(this);
    }
}
