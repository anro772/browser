using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.Data.Repositories;
using BrowserApp.UI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.ViewModels;

/// <summary>
/// Covers the QOL additions to MarketplaceViewModel: tag chip filter wiring,
/// author: prefix search, hero pick by DownloadCount, offline state, pagination.
/// </summary>
public class MarketplaceViewModelTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly Mock<IMarketplaceApiClient> _api = new();
    private readonly Mock<IRuleEngine> _ruleEngine = new();
    private readonly MarketplaceViewModel _vm;

    public MarketplaceViewModelTests()
    {
        // Capture the DB name as a local so every DbContext resolution shares the
        // same in-memory store — otherwise the lambda re-evaluates Guid.NewGuid()
        // per resolution and the VM's scoped context sees an empty DB.
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<BrowserDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IRuleRepository, RuleRepository>();
        _provider = services.BuildServiceProvider();

        // Default: connected, returns no results unless overridden per-test.
        _api.Setup(a => a.CheckConnectionAsync()).ReturnsAsync(true);
        _api.Setup(a => a.GetRulesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new RuleListResponse { Rules = new(), TotalCount = 0, Page = 1, PageSize = 50 });

        _vm = new MarketplaceViewModel(
            _api.Object,
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _ruleEngine.Object);
    }

    public void Dispose() => _provider.Dispose();

    private static RuleResponse Resp(string name, string author = "alice", int downloads = 0,
                                     string[]? tags = null, string site = "*", Guid? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = "test",
            Site = site,
            Priority = 10,
            RulesJson = "[]",
            AuthorUsername = author,
            DownloadCount = downloads,
            Tags = tags ?? Array.Empty<string>(),
            CreatedAt = DateTime.UtcNow
        };

    private void SetupApiPage(IEnumerable<RuleResponse> rules, int totalCount = -1)
    {
        var list = rules.ToList();
        var resp = new RuleListResponse
        {
            Rules = list, TotalCount = totalCount < 0 ? list.Count : totalCount, Page = 1, PageSize = 50
        };
        _api.Setup(a => a.GetRulesAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(resp);
    }

    // ───── Offline detection ─────

    [Fact]
    public async Task LoadRules_ServerOffline_SetsIsOfflineAndSkipsFetch()
    {
        _api.Setup(a => a.CheckConnectionAsync()).ReturnsAsync(false);

        await _vm.LoadRulesCommand.ExecuteAsync(null);

        Assert.True(_vm.IsOffline);
        Assert.Empty(_vm.Rules);
        _api.Verify(a => a.GetRulesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LoadRules_ConnectionRecovers_ClearsOfflineFlag()
    {
        _api.SetupSequence(a => a.CheckConnectionAsync()).ReturnsAsync(false).ReturnsAsync(true);
        SetupApiPage(new[] { Resp("Pack") });

        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.True(_vm.IsOffline);

        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.False(_vm.IsOffline);
    }

    // ───── TopRule (hero card) ─────

    [Fact]
    public async Task TopRule_IsHighestDownloadCount()
    {
        SetupApiPage(new[]
        {
            Resp("Niche pack", downloads: 4),
            Resp("Popular pack", downloads: 9001),
            Resp("OK pack", downloads: 88)
        });

        await _vm.LoadRulesCommand.ExecuteAsync(null);

        Assert.NotNull(_vm.TopRule);
        Assert.Equal("Popular pack", _vm.TopRule!.Name);
    }

    [Fact]
    public async Task TopRule_NoRules_IsNull()
    {
        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.Null(_vm.TopRule);
    }

    // ───── AvailableTags ─────

    [Fact]
    public async Task AvailableTags_DedupedAndSorted()
    {
        SetupApiPage(new[]
        {
            Resp("A", tags: new[] { "privacy", "ads" }),
            Resp("B", tags: new[] { "ads", "video" }),
            Resp("C", tags: new[] { "privacy" })
        });

        await _vm.LoadRulesCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "ads", "privacy", "video" }, _vm.AvailableTags.ToArray());
    }

    [Fact]
    public async Task AvailableTags_IgnoresEmptyAndWhitespace()
    {
        SetupApiPage(new[] { Resp("A", tags: new[] { "tag", "", "   ", "tag" }) });
        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.Equal(new[] { "tag" }, _vm.AvailableTags.ToArray());
    }

    [Fact]
    public async Task AvailableTags_CaseInsensitiveDedup()
    {
        SetupApiPage(new[]
        {
            Resp("A", tags: new[] { "Privacy" }),
            Resp("B", tags: new[] { "privacy" }),
            Resp("C", tags: new[] { "PRIVACY" })
        });
        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.Single(_vm.AvailableTags);
    }

    // ───── Author chip / author: search prefix ─────

    [Fact]
    public async Task FilterByAuthor_SetsSearchPrefix()
    {
        await _vm.LoadRulesCommand.ExecuteAsync(null);
        _vm.FilterByAuthorCommand.Execute("alice");
        Assert.Equal("author:alice", _vm.SearchFilter);
    }

    [Fact]
    public void FilterByAuthor_IgnoresEmptyInput()
    {
        var before = _vm.SearchFilter;
        _vm.FilterByAuthorCommand.Execute("");
        _vm.FilterByAuthorCommand.Execute(null);
        Assert.Equal(before, _vm.SearchFilter);
    }

    [Fact]
    public async Task SearchFilter_AuthorPrefix_StrictMatchByAuthor()
    {
        SetupApiPage(new[]
        {
            Resp("By alice 1", author: "alice"),
            Resp("By bob",     author: "bob"),
            Resp("By alice 2", author: "alice")
        });
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        _vm.SearchFilter = "author:alice";

        Assert.Equal(2, _vm.Rules.Count);
        Assert.All(_vm.Rules, r => Assert.Equal("alice", r.AuthorUsername));
    }

    [Fact]
    public async Task SearchFilter_AuthorPrefix_IsCaseInsensitive()
    {
        SetupApiPage(new[] { Resp("x", author: "AliceBob") });
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        _vm.SearchFilter = "AUTHOR:alicebob";

        Assert.Single(_vm.Rules);
    }

    [Fact]
    public async Task SearchFilter_PlainQuery_MatchesNameDescriptionOrAuthor()
    {
        SetupApiPage(new[]
        {
            Resp("Privacy Pack",  author: "alice"),
            Resp("Generic",       author: "privacy_advocate"),
            Resp("Other",         author: "bob"),
        });
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        _vm.SearchFilter = "privacy";
        Assert.Equal(2, _vm.Rules.Count);
    }

    // ───── ClearTagFilter ─────

    [Fact]
    public async Task ClearTagFilter_ResetsToFullList()
    {
        SetupApiPage(new[] { Resp("A", tags: new[] { "x" }) });
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        _api.Setup(a => a.SearchRulesAsync("",  new[] { "x" }, 1, It.IsAny<int>()))
            .ReturnsAsync(new RuleListResponse { Rules = new(), TotalCount = 0 });

        _vm.SelectedTag = "x"; // triggers ReloadForTagAsync
        Assert.Equal("x", _vm.SelectedTag);

        _vm.ClearTagFilterCommand.Execute(null);
        Assert.Null(_vm.SelectedTag);
    }

    // ───── HasMore / pagination ─────

    [Fact]
    public async Task HasMore_TrueWhenServerHasExtraPages()
    {
        SetupApiPage(new[] { Resp("a"), Resp("b") }, totalCount: 87);
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        Assert.True(_vm.HasMore);
        Assert.Equal(87, _vm.ServerTotalCount);
    }

    [Fact]
    public async Task HasMore_FalseWhenAllLoaded()
    {
        SetupApiPage(new[] { Resp("a"), Resp("b") }, totalCount: 2);
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        Assert.False(_vm.HasMore);
    }

    [Fact]
    public async Task LoadMore_AppendsToExistingList()
    {
        // First page
        SetupApiPage(new[] { Resp("first") }, totalCount: 3);
        await _vm.LoadRulesCommand.ExecuteAsync(null);
        Assert.Single(_vm.Rules);

        // Subsequent fetch should append, not replace
        _api.Setup(a => a.GetRulesAsync(2, It.IsAny<int>()))
            .ReturnsAsync(new RuleListResponse
            {
                Rules = new() { Resp("second"), Resp("third") },
                TotalCount = 3
            });

        await _vm.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(3, _vm.Rules.Count);
        Assert.Equal("first", _vm.Rules[0].Name);
        Assert.Equal("third", _vm.Rules[2].Name);
    }

    [Fact]
    public async Task LoadMore_NoOpWhenNoMore()
    {
        SetupApiPage(new[] { Resp("a") }, totalCount: 1);
        await _vm.LoadRulesCommand.ExecuteAsync(null);

        await _vm.LoadMoreCommand.ExecuteAsync(null);

        Assert.Single(_vm.Rules);
        _api.Verify(a => a.GetRulesAsync(2, It.IsAny<int>()), Times.Never);
    }

    // ───── Installed-state cross-reference ─────

    [Fact]
    public async Task LoadRules_MarksPacksAlreadyInLocalDb()
    {
        var id = Guid.NewGuid();
        SetupApiPage(new[] { Resp("Installed Pack", id: id), Resp("Fresh Pack") });

        // Pre-seed a local rule pointing back to that marketplace id
        using (var scope = _provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<BrowserDbContext>();
            ctx.Rules.Add(new RuleEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Installed Pack",
                Site = "*",
                RulesJson = "[]",
                Source = "marketplace",
                MarketplaceId = id.ToString(),
                Enabled = true
            });
            await ctx.SaveChangesAsync();
        }

        await _vm.LoadRulesCommand.ExecuteAsync(null);

        var installed = _vm.Rules.First(r => r.Name == "Installed Pack");
        Assert.True(installed.IsInstalled);
        var fresh = _vm.Rules.First(r => r.Name == "Fresh Pack");
        Assert.False(fresh.IsInstalled);
    }
}

/// <summary>
/// MarketplaceRuleItemViewModel's display helpers were extended by the QOL pass;
/// pin down their formatting so XAML bindings stay stable.
/// </summary>
public class MarketplaceRuleItemViewModelTests
{
    private static RuleResponse Resp(string[]? tags = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "x",
        Description = "",
        Site = "*",
        Priority = 10,
        RulesJson = "[]",
        AuthorUsername = "a",
        DownloadCount = 0,
        Tags = tags ?? Array.Empty<string>(),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void TagsDisplay_EmptyTags_ReadsAsNoTags()
    {
        var vm = new MarketplaceRuleItemViewModel(Resp(Array.Empty<string>()));
        Assert.Equal("No tags", vm.TagsDisplay);
    }

    [Fact]
    public void TagsDisplay_JoinsWithCommaSpace()
    {
        var vm = new MarketplaceRuleItemViewModel(Resp(new[] { "a", "b", "c" }));
        Assert.Equal("a, b, c", vm.TagsDisplay);
    }

    [Fact]
    public void InstallButtonText_FlipsOnInstall()
    {
        var vm = new MarketplaceRuleItemViewModel(Resp());
        Assert.Equal("Install", vm.InstallButtonText);
        vm.IsInstalled = true;
        Assert.Equal("Installed", vm.InstallButtonText);
    }
}
