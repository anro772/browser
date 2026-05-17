using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using BrowserApp.Core.Models;
using BrowserApp.UI.ViewModels;
using Wpf.Ui.Controls;

namespace BrowserApp.UI.Views;

public partial class MarketplaceRuleDetailDialog : FluentWindow, INotifyPropertyChanged
{
    public MarketplaceRuleItemViewModel Rule { get; }
    private readonly MarketplaceViewModel _parentVm;

    public List<ActionBullet> ActionBullets { get; }

    public string CreatedDisplay =>
        Rule.CreatedAt == default ? string.Empty : $"Published {Rule.CreatedAt:yyyy-MM-dd}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MarketplaceRuleDetailDialog(MarketplaceRuleItemViewModel rule, MarketplaceViewModel parentVm)
    {
        Rule = rule;
        _parentVm = parentVm;
        ActionBullets = BuildBullets(rule.RulesJson);
        DataContext = this;
        InitializeComponent();

        Rule.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MarketplaceRuleItemViewModel.IsInstalled))
            {
                // Trigger Visibility binding refresh — Rule is the source.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rule)));
            }
        };
    }

    /// <summary>Public for unit testing; pure function over a RulesJson string.</summary>
    public static List<ActionBullet> BuildBullets(string rulesJson)
    {
        var bullets = new List<ActionBullet>();
        if (string.IsNullOrWhiteSpace(rulesJson))
        {
            bullets.Add(new ActionBullet(SymbolRegular.ErrorCircle24, new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)), "Pack has no actions."));
            return bullets;
        }

        Rule? parsed = null;
        try
        {
            // Try the full Rule shape first.
            parsed = JsonSerializer.Deserialize<Rule>(rulesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            // Fall through.
        }

        // If the JSON is just a List<RuleAction>, wrap it.
        if (parsed == null || parsed.Rules == null || parsed.Rules.Count == 0)
        {
            try
            {
                var actions = JsonSerializer.Deserialize<List<RuleAction>>(rulesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (actions != null)
                {
                    parsed = new Rule { Rules = actions };
                }
            }
            catch
            {
                bullets.Add(new ActionBullet(SymbolRegular.Warning24, new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                    "Could not parse rule actions. View raw JSON below."));
                return bullets;
            }
        }

        if (parsed == null || parsed.Rules.Count == 0)
        {
            bullets.Add(new ActionBullet(SymbolRegular.Info24, new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)),
                "This pack contains no actions."));
            return bullets;
        }

        var blockBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0x70, 0x8A));
        var cssBrush = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));
        var jsBrush = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
        var hdrBrush = new SolidColorBrush(Color.FromRgb(0x5E, 0xEA, 0xD4));

        var blocks = parsed.GetBlockActions().ToList();
        if (blocks.Count > 0)
        {
            var samples = blocks
                .Select(b => b.Match?.UrlPattern)
                .Where(p => !string.IsNullOrEmpty(p))
                .Take(3)
                .ToArray();
            var samplePart = samples.Length > 0 ? $" — e.g. {string.Join(", ", samples)}" : string.Empty;
            var more = blocks.Count > samples.Length ? "…" : string.Empty;
            bullets.Add(new ActionBullet(SymbolRegular.ShieldDismiss24, blockBrush,
                $"Blocks {blocks.Count} URL pattern{(blocks.Count == 1 ? "" : "s")}{samplePart}{more}"));
        }

        var css = parsed.GetCssInjections().ToList();
        if (css.Count > 0)
        {
            bullets.Add(new ActionBullet(SymbolRegular.Color24, cssBrush,
                $"Injects {css.Count} CSS snippet{(css.Count == 1 ? "" : "s")} to restyle or hide page elements."));
        }

        var js = parsed.GetJsInjections().ToList();
        if (js.Count > 0)
        {
            var timing = js.FirstOrDefault()?.Timing ?? "dom_ready";
            bullets.Add(new ActionBullet(SymbolRegular.Code24, jsBrush,
                $"Runs {js.Count} JS snippet{(js.Count == 1 ? "" : "s")} at {timing}."));
        }

        var headerActions = parsed.Rules.Count(r => r.Type == "modify_headers");
        if (headerActions > 0)
        {
            bullets.Add(new ActionBullet(SymbolRegular.DocumentEdit24, hdrBrush,
                $"Modifies HTTP headers on {headerActions} request type{(headerActions == 1 ? "" : "s")}."));
        }

        if (bullets.Count == 0)
        {
            bullets.Add(new ActionBullet(SymbolRegular.Info24, new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD)),
                $"{parsed.Rules.Count} custom action{(parsed.Rules.Count == 1 ? "" : "s")}. See raw JSON for details."));
        }

        return bullets;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (_parentVm.InstallRuleCommand.CanExecute(Rule))
        {
            InstallBtn.IsEnabled = false;
            await _parentVm.InstallRuleCommand.ExecuteAsync(Rule);
            InstallBtn.IsEnabled = true;
        }
    }
}

public record ActionBullet(SymbolRegular Icon, Brush IconBrush, string Text);
