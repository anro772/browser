using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Marketplace panel.
/// Handles browsing and installing marketplace rules.
/// </summary>
public partial class MarketplaceViewModel : ObservableObject
{
    private const int PageSize = 50;

    private readonly IMarketplaceApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    [ObservableProperty]
    private ObservableCollection<MarketplaceRuleItemViewModel> _rules = new();

    [ObservableProperty]
    private MarketplaceRuleItemViewModel? _selectedRule;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Filtered count (what the user sees in the grid).</summary>
    [ObservableProperty]
    private int _totalRules;

    /// <summary>Unfiltered server-side total — drives the "Load more" footer.</summary>
    [ObservableProperty]
    private int _serverTotalCount;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private MarketplaceRuleItemViewModel? _topRule;

    [ObservableProperty]
    private ObservableCollection<string> _availableTags = new();

    [ObservableProperty]
    private string? _selectedTag;

    private List<MarketplaceRuleItemViewModel> _allRules = new();
    private int _currentPage = 1;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    public bool HasMore => _allRules.Count < ServerTotalCount;

    partial void OnServerTotalCountChanged(int value) => OnPropertyChanged(nameof(HasMore));

    partial void OnSearchFilterChanged(string value) => FilterRules();

    partial void OnSelectedTagChanged(string? value)
    {
        // When the user toggles a tag chip, hit the server-side /search endpoint.
        _ = ReloadForTagAsync();
    }

    public MarketplaceViewModel(
        IMarketplaceApiClient apiClient,
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine)
    {
        _apiClient = apiClient;
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    [RelayCommand]
    private async Task LoadRulesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading marketplace rules...";
        _currentPage = 1;

        try
        {
            // Connection check — show offline banner but fall back to locally-installed packs
            // so the view isn't empty when the user already has marketplace rules synced.
            var connected = await _apiClient.CheckConnectionAsync();
            if (!connected)
            {
                IsOffline = true;
                var local = await GetInstalledMarketplacePacksAsync();
                UiThread.Invoke(() =>
                {
                    Rules.Clear();
                    _allRules = local;
                    foreach (var r in local) Rules.Add(r);
                    TopRule = null;
                    TotalRules = Rules.Count;
                    ServerTotalCount = Rules.Count;
                    RebuildAvailableTags();
                });
                StatusMessage = local.Count > 0
                    ? $"Marketplace server is offline — showing {local.Count} installed pack(s) from local cache."
                    : "Marketplace server is offline.";
                return;
            }

            IsOffline = false;

            var response = await _apiClient.GetRulesAsync(_currentPage, PageSize);
            if (response != null)
            {
                var installedIds = await GetInstalledMarketplaceIdsAsync();

                UiThread.Invoke(() =>
                {
                    Rules.Clear();
                    foreach (var rule in response.Rules)
                    {
                        Rules.Add(new MarketplaceRuleItemViewModel(rule)
                        {
                            IsInstalled = installedIds.Contains(rule.Id.ToString())
                        });
                    }
                    _allRules = Rules.ToList();
                    ServerTotalCount = response.TotalCount;
                    RebuildAvailableTags();
                    UpdateTopRule();
                    TotalRules = Rules.Count;
                });

                StatusMessage = $"Loaded {Rules.Count} of {ServerTotalCount} rules from marketplace";
            }
            else
            {
                StatusMessage = "Failed to load rules - server may be unavailable";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading rules: {ex.Message}";
            ErrorLogger.LogError("Failed to load marketplace rules", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMore || IsLoading) return;
        IsLoading = true;
        try
        {
            _currentPage++;
            var response = SelectedTag is { Length: > 0 }
                ? await _apiClient.SearchRulesAsync(SearchFilter, new[] { SelectedTag }, _currentPage, PageSize)
                : await _apiClient.GetRulesAsync(_currentPage, PageSize);

            if (response == null || response.Rules.Count == 0) return;

            var installedIds = await GetInstalledMarketplaceIdsAsync();
            UiThread.Invoke(() =>
            {
                foreach (var rule in response.Rules)
                {
                    var vm = new MarketplaceRuleItemViewModel(rule)
                    {
                        IsInstalled = installedIds.Contains(rule.Id.ToString())
                    };
                    Rules.Add(vm);
                    _allRules.Add(vm);
                }
                RebuildAvailableTags();
                UpdateTopRule();
                TotalRules = Rules.Count;
            });

            StatusMessage = $"Loaded {Rules.Count} of {ServerTotalCount} rules";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load-more failed: {ex.Message}";
            ErrorLogger.LogError("Failed to load more marketplace rules", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadForTagAsync()
    {
        if (SelectedTag is null or "")
        {
            // Clearing the tag → fall back to the unfiltered list.
            await LoadRulesAsync();
            return;
        }

        IsLoading = true;
        StatusMessage = $"Searching by tag '{SelectedTag}'…";
        _currentPage = 1;
        try
        {
            var response = await _apiClient.SearchRulesAsync(SearchFilter, new[] { SelectedTag }, _currentPage, PageSize);
            if (response == null) return;

            var installedIds = await GetInstalledMarketplaceIdsAsync();
            UiThread.Invoke(() =>
            {
                Rules.Clear();
                _allRules.Clear();
                foreach (var rule in response.Rules)
                {
                    var vm = new MarketplaceRuleItemViewModel(rule)
                    {
                        IsInstalled = installedIds.Contains(rule.Id.ToString())
                    };
                    Rules.Add(vm);
                    _allRules.Add(vm);
                }
                ServerTotalCount = response.TotalCount;
                UpdateTopRule();
                TotalRules = Rules.Count;
            });
            StatusMessage = $"Tag '{SelectedTag}': {Rules.Count} matches";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tag search failed: {ex.Message}";
            ErrorLogger.LogError($"Failed to search tag {SelectedTag}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearTagFilter() => SelectedTag = null;

    [RelayCommand]
    private void FilterByAuthor(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return;
        SearchFilter = $"author:{username}";
    }

    [RelayCommand]
    private async Task InstallRuleAsync(MarketplaceRuleItemViewModel? rule)
    {
        if (rule == null) return;

        if (rule.IsInstalled)
        {
            StatusMessage = $"'{rule.Name}' is already installed.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Installing '{rule.Name}'...";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            var existing = await repository.GetByMarketplaceIdAsync(rule.Id.ToString());
            if (existing != null)
            {
                rule.IsInstalled = true;
                StatusMessage = $"'{rule.Name}' is already installed.";
                return;
            }

            var entity = new RuleEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = rule.Name,
                Description = rule.Description,
                Site = rule.Site,
                Priority = rule.Priority,
                RulesJson = rule.RulesJson,
                Source = "marketplace",
                MarketplaceId = rule.Id.ToString(),
                Enabled = true,
                IsEnforced = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(entity);
            await _apiClient.IncrementDownloadAsync(rule.Id);
            await _ruleEngine.ReloadRulesAsync();

            rule.IsInstalled = true;
            rule.DownloadCount++;

            StatusMessage = $"Installed '{rule.Name}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to install '{rule.Name}': {ex.Message}";
            ErrorLogger.LogError($"Failed to install rule {rule.Id}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowRuleDetails(MarketplaceRuleItemViewModel? rule)
    {
        if (rule == null) return;
        var dialog = new Views.MarketplaceRuleDetailDialog(rule, this)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    private void FilterRules()
    {
        UiThread.Invoke(() =>
        {
            Rules.Clear();

            IEnumerable<MarketplaceRuleItemViewModel> filtered = _allRules;

            // Author prefix syntax — power-user shortcut, also fired by author chip clicks.
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                if (SearchFilter.StartsWith("author:", StringComparison.OrdinalIgnoreCase))
                {
                    var who = SearchFilter[7..].Trim();
                    filtered = filtered.Where(r => r.AuthorUsername.Equals(who, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    filtered = filtered.Where(r =>
                        r.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                        r.AuthorUsername.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            foreach (var r in filtered) Rules.Add(r);
            TotalRules = Rules.Count;
            UpdateTopRule();
        });
    }

    private void UpdateTopRule()
    {
        TopRule = _allRules
            .OrderByDescending(r => r.DownloadCount)
            .FirstOrDefault();

        // Mark the top rule so its grid card carries the gold bookmark while the rest
        // get a neutral accent — gives the recommended pack a visual hook in the grid.
        foreach (var r in _allRules) r.IsRecommended = false;
        if (TopRule != null) TopRule.IsRecommended = true;
    }

    private void RebuildAvailableTags()
    {
        var distinct = _allRules
            .SelectMany(r => r.Tags ?? Array.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AvailableTags.Clear();
        foreach (var t in distinct) AvailableTags.Add(t);
    }

    private async Task<HashSet<string?>> GetInstalledMarketplaceIdsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
        var localRules = await repository.GetAllAsync();
        return localRules
            .Where(r => !string.IsNullOrEmpty(r.MarketplaceId))
            .Select(r => r.MarketplaceId)
            .ToHashSet();
    }

    /// <summary>
    /// Builds marketplace card view-models from already-installed marketplace rules in the
    /// local SQLite store. Used as the offline fallback so the Marketplace view is not empty
    /// when the user has packs from a previous sync but the server is unreachable.
    /// Author/tags/download-count are unknown locally and surface as empty/zero.
    /// </summary>
    private async Task<List<MarketplaceRuleItemViewModel>> GetInstalledMarketplacePacksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
        var localRules = await repository.GetAllAsync();

        return localRules
            .Where(r => string.Equals(r.Source, "marketplace", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrEmpty(r.MarketplaceId))
            .Select(r =>
            {
                if (!Guid.TryParse(r.MarketplaceId, out var id)) id = Guid.NewGuid();
                var synthetic = new RuleResponse
                {
                    Id = id,
                    Name = r.Name,
                    Description = r.Description,
                    Site = r.Site,
                    Priority = r.Priority,
                    RulesJson = r.RulesJson,
                    AuthorUsername = string.Empty,
                    DownloadCount = 0,
                    Tags = Array.Empty<string>(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                };
                return new MarketplaceRuleItemViewModel(synthetic) { IsInstalled = true };
            })
            .ToList();
    }
}

/// <summary>
/// ViewModel for a single rule item in the marketplace list.
/// </summary>
public partial class MarketplaceRuleItemViewModel : ObservableObject
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Site { get; }
    public int Priority { get; }
    public string RulesJson { get; }
    public string AuthorUsername { get; }
    public string[] Tags { get; }
    public DateTime CreatedAt { get; }

    [ObservableProperty]
    private int _downloadCount;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isRecommended;

    public MarketplaceRuleItemViewModel(RuleResponse response)
    {
        Id = response.Id;
        Name = response.Name;
        Description = response.Description;
        Site = response.Site;
        Priority = response.Priority;
        RulesJson = response.RulesJson;
        AuthorUsername = response.AuthorUsername;
        DownloadCount = response.DownloadCount;
        Tags = response.Tags;
        CreatedAt = response.CreatedAt;
    }

    public string TagsDisplay => Tags.Length > 0 ? string.Join(", ", Tags) : "No tags";
    public string InstallButtonText => IsInstalled ? "Installed" : "Install";

    /// <summary>
    /// True when the item carries author/download metadata (i.e. came from the server).
    /// Offline-only items synthesized from the local DB lack these — the card hides the
    /// "by … · N installs" line in that case to avoid showing "by  · 0 installs".
    /// </summary>
    public bool HasServerMetadata =>
        !string.IsNullOrWhiteSpace(AuthorUsername) || DownloadCount > 0;
}
