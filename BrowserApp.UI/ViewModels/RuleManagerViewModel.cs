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
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.UI.ViewModels;

/// <summary>
/// ViewModel for the Rule Manager panel.
/// Displays and manages blocking/injection rules.
/// </summary>
public partial class RuleManagerViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

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

    public RuleManagerViewModel(IServiceScopeFactory scopeFactory, IRuleEngine ruleEngine)
    {
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                Rules.Clear();
                foreach (var item in ruleItems)
                {
                    Rules.Add(item);
                }
                UpdateStats();
            });
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
            MessageBox.Show("This rule is enforced by a channel and cannot be disabled.", "Enforced Rule", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("This rule is enforced by a channel and cannot be deleted.", "Enforced Rule", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to delete the rule '{rule.Name}'?",
            "Delete Rule",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            await repository.DeleteAsync(rule.Id);
            Rules.Remove(rule);
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
            await _ruleEngine.ReloadRulesAsync();
            await LoadRulesAsync();

            MessageBox.Show($"Template '{rule.Name}' has been loaded and enabled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RuleManager] Error loading template: {ex.Message}");
            MessageBox.Show($"Error loading template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
    public int ActionCount { get; }

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

        // Count actions
        try
        {
            var actions = JsonSerializer.Deserialize<List<RuleAction>>(entity.RulesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ActionCount = actions?.Count ?? 0;
        }
        catch
        {
            ActionCount = 0;
        }
    }

    public string SourceDisplay => Source switch
    {
        "local" => "Local",
        "template" => "Template",
        "marketplace" => "Marketplace",
        "channel" => "Channel",
        _ => Source
    };
}
