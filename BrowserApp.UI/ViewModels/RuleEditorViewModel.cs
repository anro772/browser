using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BrowserApp.UI.ViewModels;

public partial class RuleEditorViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRuleEngine _ruleEngine;

    [ObservableProperty]
    private string _ruleName = string.Empty;

    [ObservableProperty]
    private string _ruleDescription = string.Empty;

    [ObservableProperty]
    private string _sitePattern = "*";

    [ObservableProperty]
    private int _priority = 10;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private ObservableCollection<RuleActionEditorItem> _actions = new();

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string? _editingRuleId;

    [ObservableProperty]
    private string? _validationError;

    public bool WasSaved { get; private set; }
    public Action? CloseAction { get; set; }

    public string DialogTitle => IsEditMode ? "Edit Rule" : "New Rule";

    public RuleEditorViewModel(IServiceScopeFactory scopeFactory, IRuleEngine ruleEngine)
    {
        _scopeFactory = scopeFactory;
        _ruleEngine = ruleEngine;
    }

    public void LoadFromEntity(RuleEntity entity)
    {
        IsEditMode = true;
        EditingRuleId = entity.Id;
        RuleName = entity.Name;
        RuleDescription = entity.Description ?? string.Empty;
        SitePattern = entity.Site;
        Priority = entity.Priority;
        IsEnabled = entity.Enabled;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var ruleActions = JsonSerializer.Deserialize<List<RuleAction>>(entity.RulesJson, options);
            if (ruleActions != null)
            {
                foreach (var action in ruleActions)
                {
                    Actions.Add(RuleActionEditorItem.FromRuleAction(action));
                }
            }
        }
        catch
        {
            // If JSON parse fails, start with empty actions
        }

        OnPropertyChanged(nameof(DialogTitle));
    }

    [RelayCommand]
    private void AddAction()
    {
        Actions.Add(new RuleActionEditorItem());
        ValidationError = null;
    }

    [RelayCommand]
    private void RemoveAction(RuleActionEditorItem item)
    {
        Actions.Remove(item);
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if (!Validate())
            return;

        try
        {
            var ruleActions = Actions.Select(a => a.ToRuleAction()).ToList();
            var rulesJson = JsonSerializer.Serialize(ruleActions);

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

            if (IsEditMode && !string.IsNullOrEmpty(EditingRuleId))
            {
                var entity = await repository.GetByIdAsync(EditingRuleId);
                if (entity != null)
                {
                    entity.Name = RuleName;
                    entity.Description = RuleDescription;
                    entity.Site = SitePattern;
                    entity.Priority = Priority;
                    entity.Enabled = IsEnabled;
                    entity.RulesJson = rulesJson;
                    await repository.UpdateAsync(entity);
                }
            }
            else
            {
                var entity = new RuleEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = RuleName,
                    Description = RuleDescription,
                    Site = SitePattern,
                    Enabled = IsEnabled,
                    Priority = Priority,
                    RulesJson = rulesJson,
                    Source = "local",
                    IsEnforced = false
                };
                await repository.AddAsync(entity);
            }

            await _ruleEngine.ReloadRulesAsync();
            WasSaved = true;
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            ValidationError = $"Error saving rule: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        WasSaved = false;
        CloseAction?.Invoke();
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(RuleName))
        {
            ValidationError = "Rule name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SitePattern))
        {
            ValidationError = "Site pattern is required.";
            return false;
        }

        if (Actions.Count == 0)
        {
            ValidationError = "At least one action is required.";
            return false;
        }

        foreach (var action in Actions)
        {
            if (action.ActionType == "block" && string.IsNullOrWhiteSpace(action.UrlPattern))
            {
                ValidationError = $"URL pattern is required for block actions.";
                return false;
            }
            if (action.ActionType == "inject_css" && string.IsNullOrWhiteSpace(action.CssContent))
            {
                ValidationError = $"CSS content is required for CSS injection actions.";
                return false;
            }
            if (action.ActionType == "inject_js" && string.IsNullOrWhiteSpace(action.JsContent))
            {
                ValidationError = $"JavaScript content is required for JS injection actions.";
                return false;
            }
        }

        ValidationError = null;
        return true;
    }
}

public partial class RuleActionEditorItem : ObservableObject
{
    [ObservableProperty]
    private string _actionType = "block";

    [ObservableProperty]
    private string _urlPattern = string.Empty;

    [ObservableProperty]
    private string? _resourceType;

    [ObservableProperty]
    private string? _method;

    [ObservableProperty]
    private string _cssContent = string.Empty;

    [ObservableProperty]
    private string _jsContent = string.Empty;

    [ObservableProperty]
    private string _timing = "dom_ready";

    [ObservableProperty]
    private bool _isBlockType = true;

    [ObservableProperty]
    private bool _isCssType;

    [ObservableProperty]
    private bool _isJsType;

    partial void OnActionTypeChanged(string value)
    {
        IsBlockType = value == "block";
        IsCssType = value == "inject_css";
        IsJsType = value == "inject_js";
    }

    public static List<string> ActionTypes => ["block", "inject_css", "inject_js"];

    public static List<string> ResourceTypes =>
        ["", "Script", "Stylesheet", "Image", "Media", "Font", "Document", "XHR", "Fetch", "WebSocket", "Other"];

    public static List<string> Methods => ["", "GET", "POST", "PUT", "DELETE"];

    public static List<string> Timings => ["dom_ready", "load"];

    public static List<string> ActionTypeDisplayNames => ["Block Request", "Inject CSS", "Inject JavaScript"];

    public string ActionTypeDisplay => ActionType switch
    {
        "block" => "Block Request",
        "inject_css" => "Inject CSS",
        "inject_js" => "Inject JavaScript",
        _ => ActionType
    };

    public RuleAction ToRuleAction()
    {
        return new RuleAction
        {
            Type = ActionType,
            Match = new RuleMatch
            {
                UrlPattern = string.IsNullOrWhiteSpace(UrlPattern) ? null : UrlPattern,
                ResourceType = string.IsNullOrWhiteSpace(ResourceType) ? null : ResourceType,
                Method = string.IsNullOrWhiteSpace(Method) ? null : Method
            },
            Css = ActionType == "inject_css" ? CssContent : null,
            Js = ActionType == "inject_js" ? JsContent : null,
            Timing = ActionType == "inject_js" ? Timing : (ActionType == "inject_css" ? "dom_ready" : null)
        };
    }

    public static RuleActionEditorItem FromRuleAction(RuleAction action)
    {
        var item = new RuleActionEditorItem
        {
            ActionType = action.Type ?? "block",
            UrlPattern = action.Match?.UrlPattern ?? string.Empty,
            ResourceType = action.Match?.ResourceType,
            Method = action.Match?.Method,
            CssContent = action.Css ?? string.Empty,
            JsContent = action.Js ?? string.Empty,
            Timing = action.Timing ?? "dom_ready"
        };
        return item;
    }
}
