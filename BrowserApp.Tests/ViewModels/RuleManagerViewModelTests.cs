using BrowserApp.Core.Interfaces;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Repositories;
using BrowserApp.UI.Services;
using BrowserApp.UI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

/// <summary>
/// Tests for the QOL additions to RuleManagerViewModel:
/// source-filter chips, column sort, duplicate/copy-id commands, and the new
/// origin-tooltip computed property on RuleItemViewModel.
///
/// The VM marshals back to the UI thread via UiThread.Invoke, which is no-op-safe
/// when no WPF Application exists — so these tests work in plain xUnit.
/// </summary>
public class RuleManagerViewModelTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly BrowserDbContext _ctx;
    private readonly Mock<IRuleEngine> _ruleEngine = new();
    private readonly Mock<IMarketplaceApiClient> _marketplaceApi = new();
    private readonly SettingsService _settings = new();
    private readonly RuleManagerViewModel _vm;

    public RuleManagerViewModelTests()
    {
        // Capture the DB name as a local so every DbContext resolution shares the
        // same in-memory store — otherwise the lambda re-evaluates Guid.NewGuid()
        // per resolution and the VM's scoped context sees an empty DB.
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<BrowserDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IRuleRepository, RuleRepository>();
        _provider = services.BuildServiceProvider();

        // One context for direct seeding; the VM gets its own scopes via the factory.
        _ctx = _provider.GetRequiredService<BrowserDbContext>();

        _vm = new RuleManagerViewModel(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _ruleEngine.Object,
            _marketplaceApi.Object,
            _settings);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _provider.Dispose();
    }

    private async Task SeedAsync(params RuleEntity[] entities)
    {
        _ctx.Rules.AddRange(entities);
        await _ctx.SaveChangesAsync();
        await _vm.LoadRulesCommand.ExecuteAsync(null);
    }

    private static RuleEntity Rule(string name, string source = "local", bool enforced = false, DateTime? updated = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = "",
            Site = "*",
            Priority = 10,
            RulesJson = "[]",
            Source = source,
            IsEnforced = enforced,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = updated ?? DateTime.UtcNow
        };

    // ───── SourceFilter chip strip ─────

    [Fact]
    public async Task SourceFilter_All_ShowsEverything()
    {
        await SeedAsync(Rule("A", "local"), Rule("B", "marketplace"), Rule("C", "channel"));

        _vm.SourceFilter = RuleSourceFilter.All;

        Assert.Equal(3, _vm.Rules.Count);
    }

    [Fact]
    public async Task SourceFilter_Local_IncludesTemplateAndAi()
    {
        // Per the plan: "Local" chip is the umbrella for local/template/ai —
        // those are all user-side rules without a server linkage.
        await SeedAsync(
            Rule("local_one", "local"),
            Rule("from_template", "template"),
            Rule("ai_gen", "ai"),
            Rule("market_pack", "marketplace"));

        _vm.SourceFilter = RuleSourceFilter.Local;

        Assert.Equal(3, _vm.Rules.Count);
        Assert.DoesNotContain(_vm.Rules, r => r.Source == "marketplace");
    }

    [Fact]
    public async Task SourceFilter_Marketplace_OnlyMarketplaceRules()
    {
        await SeedAsync(Rule("a", "local"), Rule("b", "marketplace"), Rule("c", "marketplace"));

        _vm.SourceFilter = RuleSourceFilter.Marketplace;

        Assert.Equal(2, _vm.Rules.Count);
        Assert.All(_vm.Rules, r => Assert.Equal("marketplace", r.Source));
    }

    [Fact]
    public async Task SourceFilter_Channel_OnlyChannelRules()
    {
        await SeedAsync(Rule("a", "local"), Rule("b", "channel"));

        _vm.SourceFilter = RuleSourceFilter.Channel;

        Assert.Single(_vm.Rules);
        Assert.Equal("channel", _vm.Rules[0].Source);
    }

    [Fact]
    public async Task SourceFilter_Enforced_OnlyEnforcedRules()
    {
        await SeedAsync(
            Rule("free", "channel", enforced: false),
            Rule("locked", "channel", enforced: true),
            Rule("local_locked", "local", enforced: true));

        _vm.SourceFilter = RuleSourceFilter.Enforced;

        Assert.Equal(2, _vm.Rules.Count);
        Assert.All(_vm.Rules, r => Assert.True(r.IsEnforced));
    }

    [Fact]
    public async Task SourceFilter_AndSearch_AreCombined()
    {
        await SeedAsync(
            Rule("alpha local", "local"),
            Rule("beta local",  "local"),
            Rule("alpha market", "marketplace"));

        _vm.SourceFilter = RuleSourceFilter.Local;
        _vm.SearchFilter = "alpha";

        Assert.Single(_vm.Rules);
        Assert.Equal("alpha local", _vm.Rules[0].Name);
    }

    [Fact]
    public async Task SetSourceFilterCommand_ParsesString()
    {
        await SeedAsync(Rule("a", "channel"));
        _vm.SetSourceFilterCommand.Execute("Channel");
        Assert.Equal(RuleSourceFilter.Channel, _vm.SourceFilter);
    }

    [Fact]
    public async Task SetSourceFilterCommand_IsCaseInsensitive()
    {
        await SeedAsync(Rule("a", "marketplace"));
        _vm.SetSourceFilterCommand.Execute("marketplace");
        Assert.Equal(RuleSourceFilter.Marketplace, _vm.SourceFilter);
    }

    [Fact]
    public async Task SetSourceFilterCommand_UnknownString_NoOp()
    {
        await SeedAsync(Rule("a"));
        var before = _vm.SourceFilter;
        _vm.SetSourceFilterCommand.Execute("garbage");
        Assert.Equal(before, _vm.SourceFilter);
    }

    // ───── Sort columns ─────

    [Fact]
    public async Task SortBy_Name_AscendingByDefault()
    {
        await SeedAsync(Rule("Charlie"), Rule("Alpha"), Rule("Bravo"));

        _vm.SortBy = RuleSortBy.Name;
        _vm.SortDescending = false;

        Assert.Equal(new[] { "Alpha", "Bravo", "Charlie" },
            _vm.Rules.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task SortBy_Name_Descending_ReversesOrder()
    {
        await SeedAsync(Rule("Charlie"), Rule("Alpha"), Rule("Bravo"));

        _vm.SortDescending = true;

        Assert.Equal(new[] { "Charlie", "Bravo", "Alpha" },
            _vm.Rules.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task SortBy_Updated_Descending_NewestFirst()
    {
        var now = DateTime.UtcNow;
        await SeedAsync(
            Rule("old", updated: now.AddDays(-30)),
            Rule("newest", updated: now),
            Rule("middle", updated: now.AddDays(-7)));

        _vm.SortBy = RuleSortBy.Updated;
        _vm.SortDescending = false; // for Updated, false maps to newest-first per VM

        Assert.Equal("newest", _vm.Rules[0].Name);
        Assert.Equal("old", _vm.Rules[2].Name);
    }

    [Fact]
    public async Task SortBy_Source_GroupsBySourceLabel()
    {
        await SeedAsync(
            Rule("z_market", "marketplace"),
            Rule("a_local", "local"),
            Rule("b_channel", "channel"));

        _vm.SortBy = RuleSortBy.Source;

        // Source labels sort: Channel, Local, Marketplace
        Assert.Equal("Channel", _vm.Rules[0].SourceDisplay);
        Assert.Equal("Local", _vm.Rules[1].SourceDisplay);
        Assert.Equal("Marketplace", _vm.Rules[2].SourceDisplay);
    }

    [Fact]
    public async Task SetSortCommand_SameColumn_TogglesDirection()
    {
        await SeedAsync(Rule("a"));

        // Move OFF the default (Name) first so we can observe the "select column" path.
        _vm.SetSortCommand.Execute("Updated");
        Assert.Equal(RuleSortBy.Updated, _vm.SortBy);
        Assert.False(_vm.SortDescending);

        // Clicking the active column flips direction.
        _vm.SetSortCommand.Execute("Updated");
        Assert.True(_vm.SortDescending);

        _vm.SetSortCommand.Execute("Updated");
        Assert.False(_vm.SortDescending);
    }

    [Fact]
    public async Task SetSortCommand_DifferentColumn_ResetsDirection()
    {
        await SeedAsync(Rule("a"));

        // Establish descending sort on Updated.
        _vm.SetSortCommand.Execute("Updated");
        _vm.SetSortCommand.Execute("Updated");   // now descending
        Assert.True(_vm.SortDescending);

        // Switching to a different column should reset direction to ascending.
        _vm.SetSortCommand.Execute("Source");

        Assert.Equal(RuleSortBy.Source, _vm.SortBy);
        Assert.False(_vm.SortDescending);
    }

    // ───── Duplicate ─────

    [Fact]
    public async Task DuplicateRule_CreatesCopyWithNewId_AndSuffix()
    {
        var original = Rule("My Rule", "local");
        await SeedAsync(original);

        await _vm.DuplicateRuleCommand.ExecuteAsync(_vm.Rules[0]);

        Assert.Equal(2, _vm.Rules.Count);
        var copy = _vm.Rules.First(r => r.Name == "My Rule (copy)");
        Assert.NotEqual(original.Id, copy.Id);
    }

    [Fact]
    public async Task DuplicateRule_OfMarketplaceRule_BecomesLocal()
    {
        // Duplicating a marketplace pack should detach it from the source —
        // otherwise the new rule would still be tagged as managed-by-server.
        await SeedAsync(Rule("Pack", "marketplace"));

        await _vm.DuplicateRuleCommand.ExecuteAsync(_vm.Rules[0]);

        var copy = _vm.Rules.First(r => r.Name == "Pack (copy)");
        Assert.Equal("local", copy.Source);
    }

    [Fact]
    public async Task DuplicateRule_OfChannelRule_DropsEnforcement()
    {
        // The copy is a *local* rule, so it cannot be "enforced" — enforcement
        // only makes sense in channel context.
        await SeedAsync(Rule("Enforced", "channel", enforced: true));

        await _vm.DuplicateRuleCommand.ExecuteAsync(_vm.Rules[0]);

        var copy = _vm.Rules.First(r => r.Name == "Enforced (copy)");
        Assert.False(copy.IsEnforced);
    }

    [Fact]
    public async Task DuplicateRule_TriggersRuleEngineReload()
    {
        await SeedAsync(Rule("x"));
        _ruleEngine.Reset();

        await _vm.DuplicateRuleCommand.ExecuteAsync(_vm.Rules[0]);

        _ruleEngine.Verify(r => r.ReloadRulesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DuplicateRule_NullInput_NoOp()
    {
        await SeedAsync(Rule("only"));
        await _vm.DuplicateRuleCommand.ExecuteAsync(null);
        Assert.Single(_vm.Rules);
    }

    // ───── Copy ID ─────

    [Fact]
    public void CopyRuleId_NullInput_DoesNotThrow()
    {
        // Just verifies the guard; can't reliably test Clipboard.SetText in xUnit
        // without an STA host (it would crash). Null path is the deterministic one.
        _vm.CopyRuleIdCommand.Execute(null);
    }
}

/// <summary>
/// OriginTooltip is a pure computed property added in the QOL sweep — easy to
/// pin down without standing up the full VM.
/// </summary>
public class RuleItemViewModel_OriginTooltipTests
{
    private static RuleEntity Make(string source, bool enforced) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "x",
            Description = "",
            Site = "*",
            Priority = 10,
            RulesJson = "[]",
            Source = source,
            IsEnforced = enforced,
            Enabled = true
        };

    [Theory]
    [InlineData("local", false, "Local rule")]
    [InlineData("template", false, "Loaded from template")]
    [InlineData("ai", false, "AI-generated rule")]
    [InlineData("marketplace", false, "Marketplace pack")]
    [InlineData("channel", false, "Channel rule")]
    [InlineData("anything_else", false, "Local rule")]
    public void OriginTooltip_ReflectsSource(string source, bool enforced, string expected)
    {
        var vm = new RuleItemViewModel(Make(source, enforced));
        Assert.Equal(expected, vm.OriginTooltip);
    }

    [Theory]
    [InlineData("channel", "Channel rule (enforced)")]
    [InlineData("local", "Local rule (enforced)")]
    public void OriginTooltip_AppendsEnforcedSuffix(string source, string expected)
    {
        var vm = new RuleItemViewModel(Make(source, enforced: true));
        Assert.Equal(expected, vm.OriginTooltip);
    }
}

public class RuleSourceFilterEnumTests
{
    [Fact]
    public void HasExpectedValues()
    {
        var values = Enum.GetValues<RuleSourceFilter>();
        Assert.Equal(5, values.Length);
        Assert.Contains(RuleSourceFilter.All, values);
        Assert.Contains(RuleSourceFilter.Local, values);
        Assert.Contains(RuleSourceFilter.Marketplace, values);
        Assert.Contains(RuleSourceFilter.Channel, values);
        Assert.Contains(RuleSourceFilter.Enforced, values);
    }

    [Fact]
    public void All_IsDefault()
    {
        // The chip-active state for "All" depends on this — if someone reorders
        // the enum without updating the XAML, the initial render would lose the
        // accent on the All chip. Catching it here.
        Assert.Equal(RuleSourceFilter.All, default(RuleSourceFilter));
    }
}

public class RuleSortByEnumTests
{
    [Fact]
    public void HasExpectedValues()
    {
        var values = Enum.GetValues<RuleSortBy>();
        Assert.Equal(3, values.Length);
        Assert.Contains(RuleSortBy.Name, values);
        Assert.Contains(RuleSortBy.Source, values);
        Assert.Contains(RuleSortBy.Updated, values);
    }

    [Fact]
    public void Name_IsDefault()
    {
        Assert.Equal(RuleSortBy.Name, default(RuleSortBy));
    }
}
