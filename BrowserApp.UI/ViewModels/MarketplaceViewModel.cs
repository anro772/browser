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
    private readonly IMarketplaceApiClient _apiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    [ObservableProperty]
    private ObservableCollection<MarketplaceRuleItemViewModel> _rules = new();

    [ObservableProperty]
    private MarketplaceRuleItemViewModel? _selectedRule;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalRules;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

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

        try
        {
            var response = await _apiClient.GetRulesAsync(1, 50);
            if (response != null)
            {
                // Get installed rule IDs to check for duplicates
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
                var localRules = await repository.GetAllAsync();
                var installedIds = localRules
                    .Where(r => !string.IsNullOrEmpty(r.MarketplaceId))
                    .Select(r => r.MarketplaceId)
                    .ToHashSet();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Rules.Clear();
                    foreach (var rule in response.Rules)
                    {
                        var item = new MarketplaceRuleItemViewModel(rule)
                        {
                            IsInstalled = installedIds.Contains(rule.Id.ToString())
                        };
                        Rules.Add(item);
                    }
                    TotalRules = response.TotalCount;
                });

                StatusMessage = $"Loaded {Rules.Count} rules from marketplace";
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
    private async Task InstallRuleAsync(MarketplaceRuleItemViewModel? rule)
    {
        if (rule == null) return;

        if (rule.IsInstalled)
        {
            MessageBox.Show(
                $"Rule '{rule.Name}' is already installed.",
                "Already Installed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        IsLoading = true;
        StatusMessage = $"Installing '{rule.Name}'...";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            // Check if already exists by marketplace ID
            var existing = await repository.GetByMarketplaceIdAsync(rule.Id.ToString());
            if (existing != null)
            {
                rule.IsInstalled = true;
                MessageBox.Show(
                    $"Rule '{rule.Name}' is already installed.",
                    "Already Installed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // Create local rule entity
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

            // Increment download count on server
            await _apiClient.IncrementDownloadAsync(rule.Id);

            // Reload rules in engine
            await _ruleEngine.ReloadRulesAsync();

            rule.IsInstalled = true;
            rule.DownloadCount++;

            StatusMessage = $"Installed '{rule.Name}' successfully!";
            MessageBox.Show(
                $"Rule '{rule.Name}' has been installed and enabled.",
                "Installation Successful",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error installing rule: {ex.Message}";
            ErrorLogger.LogError($"Failed to install rule {rule.Id}", ex);
            MessageBox.Show(
                $"Failed to install rule: {ex.Message}",
                "Installation Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
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
}
