using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using BrowserApp.Core.Interfaces;
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
    /// </summary>
    [RelayCommand]
    public async Task NewTabAsync(string? url = null)
    {
        if (_environment == null) return;

        var tab = new BrowserTabItem();
        await tab.InitializeAsync(_environment, _blockingService, _ruleEngine);

        Tabs.Add(tab);
        TabAdded?.Invoke(this, tab);

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
