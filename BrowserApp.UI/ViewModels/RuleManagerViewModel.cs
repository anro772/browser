using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Core.DTOs;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using BrowserApp.UI.Views;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Rule Manager panel.
/// Displays and manages blocking/injection rules.
/// </summary>
public partial class RuleManagerViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;
    private readonly IMarketplaceApiClient _marketplaceApiClient;
    private readonly SettingsService _settingsService;

    private List<RuleItemViewModel> _allRules = new();

    [ObservableProperty]
    private ObservableCollection<RuleItemViewModel> _rules = new();

    [ObservableProperty]
    private RuleItemViewModel? _selectedRule;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalRules;

    [ObservableProperty]
    private int _enabledRules;

    public int DisabledRules => TotalRules - EnabledRules;

    partial void OnTotalRulesChanged(int value) => OnPropertyChanged(nameof(DisabledRules));
    partial void OnEnabledRulesChanged(int value) => OnPropertyChanged(nameof(DisabledRules));

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private RuleSourceFilter _sourceFilter = RuleSourceFilter.All;

    [ObservableProperty]
    private RuleSortBy _sortBy = RuleSortBy.Name;

    [ObservableProperty]
    private bool _sortDescending;

    partial void OnSearchFilterChanged(string value) => FilterRules();
    partial void OnSourceFilterChanged(RuleSourceFilter value) => FilterRules();
    partial void OnSortByChanged(RuleSortBy value) => FilterRules();
    partial void OnSortDescendingChanged(bool value) => FilterRules();

    /// <summary>
    /// Toggles the chip strip. If the chip clicked is already active and is "All",
    /// no-op; otherwise sets it as the new filter. Called from the workspace
    /// when a chip button fires.
    /// </summary>
    [RelayCommand]
    private void SetSourceFilter(string filterName)
    {
        if (Enum.TryParse<RuleSourceFilter>(filterName, ignoreCase: true, out var parsed))
        {
            SourceFilter = parsed;
        }
    }

    /// <summary>
    /// Column-header click: toggles direction if same column, otherwise switches.
    /// </summary>
    [RelayCommand]
    private void SetSort(string columnName)
    {
        if (!Enum.TryParse<RuleSortBy>(columnName, ignoreCase: true, out var parsed)) return;

        if (parsed == SortBy)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortBy = parsed;
            SortDescending = false;
        }
    }

    public RuleManagerViewModel(
        IServiceScopeFactory scopeFactory,
        IRuleEngine ruleEngine,
        IMarketplaceApiClient marketplaceApiClient,
        SettingsService settingsService)
    {
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
        _marketplaceApiClient = marketplaceApiClient;
        _settingsService = settingsService;

        // Refresh the visible list whenever the engine reloads — e.g. after a marketplace
        // install or channel sync writes new rows to the rule repository. Without this the
        // user had to restart the app for new rules to appear in the Rules workspace.
        _ruleEngine.RulesReloaded += OnRuleEngineRulesReloaded;
    }

    private void OnRuleEngineRulesReloaded(object? sender, EventArgs e)
    {
        _ = LoadRulesAsync();
    }

    [RelayCommand]
    private async Task LoadRulesAsync()
    {
        IsLoading = true;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            var entities = await repository.GetAllAsync();
            var ruleItems = entities.Select(e => new RuleItemViewModel(e)).ToList();

            // Set the master list, then re-apply current source/search/sort so a reload
            // triggered by marketplace install or channel sync doesn't wipe the user's view.
            _allRules = ruleItems;
            FilterRules();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error loading rules: {ex.Message}");
            MessageBox.Show($"Error loading rules: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleRuleAsync(RuleItemViewModel rule)
    {
        if (rule.IsEnforced)
        {
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Enforced rule",
                "This rule is enforced by a channel and cannot be disabled.",
                showCancel: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            var entity = await repository.GetByIdAsync(rule.Id);
            if (entity != null)
            {
                entity.Enabled = !entity.Enabled;
                await repository.UpdateAsync(entity);

                rule.IsEnabled = entity.Enabled;
                await _ruleEngine.ReloadRulesAsync();
                UpdateStats();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error toggling rule: {ex.Message}");
            MessageBox.Show($"Error toggling rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(RuleItemViewModel rule)
    {
        if (rule.IsEnforced)
        {
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Enforced rule",
                "This rule is enforced by a channel and cannot be deleted.",
                showCancel: false);
            return;
        }

        if (!ConfirmDialog.Show(Application.Current.MainWindow,
                "Delete rule",
                $"Are you sure you want to delete the rule '{rule.Name}'?",
                destructive: true,
                confirmText: "Delete"))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            await repository.DeleteAsync(rule.Id);
            Rules.Remove(rule);
            _allRules.Remove(rule);
            await _ruleEngine.ReloadRulesAsync();
            UpdateStats();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error deleting rule: {ex.Message}");
            MessageBox.Show($"Error deleting rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadTemplateAsync(string templateName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"BrowserApp.UI.Resources.DefaultRules.{templateName}.json";

            // Try to find the resource
            string? json = null;

            // First try embedded resource
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                // Fall back to file system
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(basePath, "Resources", "DefaultRules", $"{templateName}.json");

                if (File.Exists(filePath))
                {
                    json = await File.ReadAllTextAsync(filePath);
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                MessageBox.Show($"Template '{templateName}' not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rule = JsonSerializer.Deserialize<Rule>(json, options);

            if (rule == null)
            {
                MessageBox.Show("Failed to parse template.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if already exists
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            if (await repository.ExistsAsync(rule.Id))
            {
                MessageBox.Show($"Template '{rule.Name}' is already loaded.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create entity
            var entity = new RuleEntity
            {
                Id = rule.Id,
                Name = rule.Name,
                Description = rule.Description,
                Site = rule.Site,
                Enabled = true, // Enable by default when loading template
                Priority = rule.Priority,
                RulesJson = JsonSerializer.Serialize(rule.Rules, options),
                Source = "template",
                IsEnforced = false
            };

            await repository.AddAsync(entity);
            ErrorLogger.LogInfo($"Template '{rule.Name}' added to database");

            await _ruleEngine.ReloadRulesAsync();
            var activeCount = _ruleEngine.GetActiveRules().Count();
            ErrorLogger.LogInfo($"RuleEngine reloaded, now has {activeCount} active rules");

            await LoadRulesAsync();

            MessageBox.Show($"Template '{rule.Name}' has been loaded and enabled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error loading template: {ex.Message}");
            MessageBox.Show($"Error loading template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CreateRule()
    {
        var viewModel = new RuleEditorViewModel(_scopeFactory, _ruleEngine);
        var dialog = new RuleEditorDialog(viewModel);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            LoadRulesCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void QuickAddRule()
    {
        var wizardVm = new QuickRuleWizardViewModel(_scopeFactory, _ruleEngine);
        var dialog = new QuickRuleWizardDialog(wizardVm);
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            if (dialog.OpenAdvancedEditor)
            {
                // Fall through to full editor
                CreateRule();
            }
            else if (dialog.WasSaved)
            {
                LoadRulesCommand.Execute(null);
            }
        }
    }

    [RelayCommand]
    private async Task EditRuleAsync(RuleItemViewModel? rule)
    {
        if (rule == null) return;

        if (rule.IsEnforced)
        {
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Enforced rule",
                "This rule is enforced by a channel and cannot be edited.",
                showCancel: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var entity = await repository.GetByIdAsync(rule.Id);
            if (entity == null) return;

            var viewModel = new RuleEditorViewModel(_scopeFactory, _ruleEngine);
            viewModel.LoadFromEntity(entity);

            var dialog = new RuleEditorDialog(viewModel);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                await LoadRulesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error editing rule: {ex.Message}");
            MessageBox.Show($"Error editing rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task PublishRuleAsync(RuleItemViewModel? rule)
    {
        if (rule == null) return;

        var dialog = new PublishRuleDialog
        {
            RuleName = rule.Name,
            Description = rule.Description,
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var request = new RuleUploadRequest
            {
                Name = rule.Name,
                Description = dialog.Description,
                Site = rule.Site,
                Priority = rule.Priority,
                RulesJson = rule.RulesJson,
                AuthorUsername = _settingsService.DisplayUsername,
                Tags = dialog.Tags
            };

            var result = await _marketplaceApiClient.UploadRuleAsync(request);
            if (result != null)
            {
                MessageBox.Show($"Rule '{rule.Name}' published to the marketplace!",
                    "Published", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to publish rule. The server may be unavailable.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error publishing rule: {ex.Message}");
            MessageBox.Show($"Error publishing rule: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DuplicateRuleAsync(RuleItemViewModel? rule)
    {
        if (rule == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var source = await repository.GetByIdAsync(rule.Id);
            if (source == null) return;

            var clone = new RuleEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = source.Name + " (copy)",
                Description = source.Description,
                Site = source.Site,
                Priority = source.Priority,
                RulesJson = source.RulesJson,
                Source = "local",
                MarketplaceId = null,
                ChannelId = null,
                Enabled = source.Enabled,
                IsEnforced = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(clone);
            await _ruleEngine.ReloadRulesAsync();
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error duplicating rule: {ex.Message}");
            ConfirmDialog.Show(Application.Current.MainWindow,
                "Duplicate failed",
                $"Could not duplicate rule: {ex.Message}",
                showCancel: false);
        }
    }

    [RelayCommand]
    private void CopyRuleId(RuleItemViewModel? rule)
    {
        if (rule == null) return;
        try
        {
            Clipboard.SetText(rule.Id);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Clipboard.SetText failed: {ex.Message}");
        }
    }

    private void FilterRules()
    {
        UiThread.Invoke(() =>
        {
            Rules.Clear();
            IEnumerable<RuleItemViewModel> query = _allRules;

            // Source / origin filter
            query = SourceFilter switch
            {
                RuleSourceFilter.Local => query.Where(r => r.Source is "local" or "template" or "ai"),
                RuleSourceFilter.Marketplace => query.Where(r => r.Source == "marketplace"),
                RuleSourceFilter.Channel => query.Where(r => r.Source == "channel"),
                RuleSourceFilter.Enforced => query.Where(r => r.IsEnforced),
                _ => query
            };

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                query = query.Where(r =>
                    r.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    r.Site.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Sort
            query = SortBy switch
            {
                RuleSortBy.Source => SortDescending
                    ? query.OrderByDescending(r => r.SourceDisplay).ThenBy(r => r.Name)
                    : query.OrderBy(r => r.SourceDisplay).ThenBy(r => r.Name),
                RuleSortBy.Updated => SortDescending
                    ? query.OrderBy(r => r.UpdatedAt)
                    : query.OrderByDescending(r => r.UpdatedAt),
                _ => SortDescending
                    ? query.OrderByDescending(r => r.Name)
                    : query.OrderBy(r => r.Name)
            };

            foreach (var r in query) Rules.Add(r);
            UpdateStats();
        });
    }

    private void UpdateStats()
    {
        TotalRules = Rules.Count;
        EnabledRules = Rules.Count(r => r.IsEnabled);
    }
}

/// <summary>
/// ViewModel for a single rule item in the list.
/// </summary>
public partial class RuleItemViewModel : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Site { get; }
    public int Priority { get; }
    public string Source { get; }
    public bool IsEnforced { get; }
    public string RulesJson { get; }
    public bool CanPublish { get; }
    public int ActionCount { get; }
    public int BlockActionCount { get; }
    public int CssActionCount { get; }
    public int JsActionCount { get; }
    public bool HasBlockActions { get; }
    public bool HasCssActions { get; }
    public bool HasJsActions { get; }
    public DateTime UpdatedAt { get; }

    [ObservableProperty]
    private bool _isEnabled;

    public RuleItemViewModel(RuleEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        Site = entity.Site;
        Priority = entity.Priority;
        Source = entity.Source;
        IsEnforced = entity.IsEnforced;
        IsEnabled = entity.Enabled;
        RulesJson = entity.RulesJson;
        CanPublish = Source is "local" or "template" or "ai";
        UpdatedAt = entity.UpdatedAt;

        // Parse and count actions by type
        try
        {
            var actions = JsonSerializer.Deserialize<List<RuleAction>>(entity.RulesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ActionCount = actions?.Count ?? 0;
            BlockActionCount = actions?.Count(a => a.Type == "block") ?? 0;
            CssActionCount = actions?.Count(a => a.Type == "inject_css") ?? 0;
            JsActionCount = actions?.Count(a => a.Type == "inject_js") ?? 0;
        }
        catch
        {
            ActionCount = 0;
        }

        HasBlockActions = BlockActionCount > 0;
        HasCssActions = CssActionCount > 0;
        HasJsActions = JsActionCount > 0;
    }

    /// <summary>
    /// Tooltip text for the origin marker — includes channel name where known.
    /// </summary>
    public string OriginTooltip
    {
        get
        {
            var label = Source switch
            {
                "marketplace" => "Marketplace pack",
                "channel" => "Channel rule",
                "template" => "Loaded from template",
                "ai" => "AI-generated rule",
                _ => "Local rule"
            };
            return IsEnforced ? $"{label} (enforced)" : label;
        }
    }

    public string SourceDisplay => Source switch
    {
        "local" => "Local",
        "template" => "Template",
        "marketplace" => "Marketplace",
        "channel" => "Channel",
        "ai" => "AI",
        _ => Source
    };

    public string UpdatedDisplay
    {
        get
        {
            var delta = DateTime.UtcNow - UpdatedAt;
            if (delta.TotalSeconds < 60) return "now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h";
            if (delta.TotalDays < 30) return $"{(int)delta.TotalDays}d";
            if (delta.TotalDays < 365) return $"{(int)(delta.TotalDays / 30)}mo";
            return $"{(int)(delta.TotalDays / 365)}y";
        }
    }
}

public enum RuleSourceFilter
{
    All,
    Local,
    Marketplace,
    Channel,
    Enforced
}

public enum RuleSortBy
{
    Name,
    Source,
    Updated
}
