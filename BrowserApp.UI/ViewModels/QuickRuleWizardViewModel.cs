using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.ViewModels;

public partial class QuickRuleWizardViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private string _wizardType = string.Empty;

    [ObservableProperty]
    private string _inputDomain = string.Empty;

    [ObservableProperty]
    private string _inputDescription = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSaving;

    public bool WasSaved { get; private set; }
    public bool OpenAdvancedEditor { get; private set; }
    public Action? CloseAction { get; set; }

    public QuickRuleWizardViewModel(IServiceScopeFactory scopeFactory, IRuleEngine ruleEngine)
    {
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    [RelayCommand]
    private void SelectType(string type)
    {
        if (type == "advanced")
        {
            OpenAdvancedEditor = true;
            CloseAction?.Invoke();
            return;
        }

        WizardType = type;
        CurrentStep = 1;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void GoBack()
    {
        CurrentStep = 0;
        WizardType = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task CreateRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(InputDomain))
        {
            ErrorMessage = "Please enter a domain or site.";
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var domain = InputDomain.Trim().ToLowerInvariant();
            // Strip protocol and trailing slash
            if (domain.StartsWith("http://")) domain = domain[7..];
            if (domain.StartsWith("https://")) domain = domain[8..];
            domain = domain.TrimEnd('/');

            RuleEntity? entity = WizardType switch
            {
                "block_domain" => CreateBlockDomainRule(domain),
                "block_ads" => CreateBlockAdsRule(domain),
                "hide_element" => CreateHideElementRule(domain),
                _ => null
            };

            if (entity == null)
            {
                ErrorMessage = "Could not create rule for this type.";
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            await repository.AddAsync(entity);
            await _ruleEngine.ReloadRulesAsync();

            WasSaved = true;
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private RuleEntity CreateBlockDomainRule(string domain)
    {
        var actions = new List<RuleAction>
        {
            new()
            {
                Type = "block",
                Match = new RuleMatch { UrlPattern = $"*{domain}*" }
            }
        };

        return new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Block {domain}",
            Description = $"Blocks all requests to {domain}",
            Site = "*",
            Enabled = true,
            Priority = 10,
            RulesJson = JsonSerializer.Serialize(actions),
            Source = "local",
            IsEnforced = false
        };
    }

    private RuleEntity CreateBlockAdsRule(string domain)
    {
        var actions = RuleGenerationService.GetStandardBlockActions();

        return new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Block Ads on {domain}",
            Description = $"Blocks common ad networks and trackers on {domain}",
            Site = $"*.{domain}",
            Enabled = true,
            Priority = 50,
            RulesJson = JsonSerializer.Serialize(actions),
            Source = "local",
            IsEnforced = false
        };
    }

    private RuleEntity CreateHideElementRule(string domain)
    {
        var description = string.IsNullOrWhiteSpace(InputDescription)
            ? string.Empty
            : InputDescription.Trim().ToLowerInvariant();

        // Map common user descriptions to CSS selectors
        var css = description switch
        {
            _ when description.Contains("cookie") => "[class*=\"cookie\"],[id*=\"cookie\"],[class*=\"consent\"],[id*=\"consent\"],.cookie-banner,.cookie-notice { display: none !important; }",
            _ when description.Contains("banner") => "[class*=\"banner\"],[id*=\"banner\"],.site-banner,.top-banner,.bottom-banner { display: none !important; }",
            _ when description.Contains("popup") || description.Contains("modal") => "[class*=\"popup\"],[class*=\"modal\"],[class*=\"overlay\"],.popup,.modal,.overlay { display: none !important; } body { overflow: auto !important; }",
            _ when description.Contains("newsletter") || description.Contains("subscribe") => "[class*=\"newsletter\"],[class*=\"subscribe\"],[id*=\"newsletter\"],[id*=\"subscribe\"] { display: none !important; }",
            _ when description.Contains("sidebar") => "[class*=\"sidebar\"],[id*=\"sidebar\"],.sidebar,aside { display: none !important; }",
            _ when description.Contains("footer") => "footer,[class*=\"footer\"],[id*=\"footer\"] { display: none !important; }",
            _ when description.Contains("header") || description.Contains("sticky") => "[class*=\"sticky\"],[class*=\"fixed-header\"],.sticky-header { position: relative !important; }",
            _ when description.Contains("ad") => RuleGenerationService.GetStandardAdHidingCss(),
            _ => $"[class*=\"{description}\"],[id*=\"{description}\"] {{ display: none !important; }}"
        };

        var actions = new List<RuleAction>
        {
            new()
            {
                Type = "inject_css",
                Match = new RuleMatch { UrlPattern = "*" },
                Css = css,
                Timing = "dom_ready"
            }
        };

        var displayName = string.IsNullOrWhiteSpace(InputDescription) ? "elements" : InputDescription.Trim();

        return new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Hide {displayName} on {domain}",
            Description = $"Hides {displayName} on {domain} using CSS",
            Site = $"*.{domain}",
            Enabled = true,
            Priority = 40,
            RulesJson = JsonSerializer.Serialize(actions),
            Source = "local",
            IsEnforced = false
        };
    }

    // Step display helpers
    public string StepTitle => WizardType switch
    {
        "block_domain" => "Block a Domain",
        "block_ads" => "Block Ads on a Site",
        "hide_element" => "Hide an Element",
        _ => "Quick Add Rule"
    };

    public string StepDescription => WizardType switch
    {
        "block_domain" => "Enter the domain you want to block completely (e.g. tracker.com)",
        "block_ads" => "Enter the site where you want ads blocked (e.g. example.com)",
        "hide_element" => "Enter the site and describe what to hide (e.g. \"cookie banner\")",
        _ => ""
    };

    public bool ShowDescriptionField => WizardType == "hide_element";
}
