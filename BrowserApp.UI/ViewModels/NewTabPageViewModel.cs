using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the New Tab Page overlay.
/// Shows frequent sites and a search bar.
/// </summary>
public partial class NewTabPageViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TabStripViewModel _tabStrip;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _greetingText = "What would you like to browse?";

    public ObservableCollection<FrequentSiteItem> FrequentSites { get; } = new();

    /// <summary>
    /// Raised when search is performed, so the new tab page can be hidden.
    /// </summary>
    public event EventHandler? SearchPerformed;

    public NewTabPageViewModel(
        IServiceScopeFactory scopeFactory,
        TabStripViewModel tabStrip)
    {
        _scopeFactory = scopeFactory;
        _tabStrip = tabStrip;

        UpdateGreeting();
    }

    [RelayCommand]
    public async Task LoadFrequentSitesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var historyRepo = scope.ServiceProvider.GetRequiredService<IBrowsingHistoryRepository>();
            var sites = await historyRepo.GetFrequentSitesAsync(8);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                FrequentSites.Clear();
                foreach (var site in sites)
                {
                    // Extract domain for display
                    var domain = string.Empty;
                    try
                    {
                        var uri = new Uri(site.Url);
                        domain = uri.Host.Replace("www.", "");
                    }
                    catch
                    {
                        domain = site.Url;
                    }

                    var initial = domain.Length > 0 ? domain[0].ToString().ToUpper() : "?";

                    FrequentSites.Add(new FrequentSiteItem
                    {
                        Url = site.Url,
                        Title = string.IsNullOrEmpty(site.Title) ? domain : site.Title,
                        Domain = domain,
                        Initial = initial,
                        VisitCount = site.VisitCount
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NewTabPage] Failed to load frequent sites: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Search()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        var tab = _tabStrip.ActiveTab;
        if (tab == null) return;

        // Use Google search for queries, direct nav for URLs
        var text = SearchText.Trim();
        string url;
        if (text.Contains('.') && !text.Contains(' '))
        {
            url = text.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? text
                : $"https://{text}";
        }
        else
        {
            url = $"https://www.google.com/search?q={Uri.EscapeDataString(text)}";
        }

        tab.Navigate(url);
        SearchPerformed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void NavigateToSite(FrequentSiteItem? site)
    {
        if (site == null) return;

        var tab = _tabStrip.ActiveTab;
        if (tab == null) return;

        tab.Navigate(site.Url);
        SearchPerformed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingText = hour switch
        {
            < 12 => "Good morning! What would you like to browse?",
            < 18 => "Good afternoon! What would you like to browse?",
            _ => "Good evening! What would you like to browse?"
        };
    }
}

public class FrequentSiteItem
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Initial { get; set; } = "?";
    public int VisitCount { get; set; }
}
