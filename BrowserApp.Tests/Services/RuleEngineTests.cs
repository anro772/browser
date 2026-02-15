using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.Services;

public class RuleEngineTests : IDisposable
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly RuleEngine _sut;

    public RuleEngineTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockRuleRepository = new Mock<IRuleRepository>();

        // Wire up the scope factory chain
        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(_mockScope.Object);
        _mockScope
            .Setup(s => s.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IRuleRepository)))
            .Returns(_mockRuleRepository.Object);

        _sut = new RuleEngine(_mockScopeFactory.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    private static RuleEntity CreateBlockRuleEntity(
        string urlPattern,
        string name = "Test Rule",
        string site = "*",
        int priority = 10,
        string? id = null)
    {
        return new RuleEntity
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            Description = "Test rule",
            Site = site,
            Enabled = true,
            Priority = priority,
            RulesJson = $"[{{\"Type\":\"block\",\"Match\":{{\"UrlPattern\":\"{urlPattern}\"}}}}]",
            Source = "local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static RuleEntity CreateInjectionRuleEntity(
        string type,
        string site = "*",
        string urlPattern = "*",
        string? css = null,
        string? js = null,
        int priority = 10)
    {
        var cssField = css != null ? $",\"Css\":\"{css}\"" : "";
        var jsField = js != null ? $",\"Js\":\"{js}\"" : "";

        return new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"Injection Rule ({type})",
            Description = "Injection test rule",
            Site = site,
            Enabled = true,
            Priority = priority,
            RulesJson = $"[{{\"Type\":\"{type}\",\"Match\":{{\"UrlPattern\":\"{urlPattern}\"}}{cssField}{jsField}}}]",
            Source = "local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task InitializeAsync_LoadsRulesFromRepository()
    {
        // Arrange
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Block Trackers"),
            CreateBlockRuleEntity("*ads.example.com/*", "Block Ads")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.GetRuleCount().Should().Be(2);
        _mockRuleRepository.Verify(r => r.GetEnabledAsync(), Times.Once);
    }

    [Fact]
    public async Task ReloadRulesAsync_ClearsAndReloadsRules()
    {
        // Arrange - load initial rules
        var initialEntities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*old-tracker.com/*", "Old Rule")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(initialEntities);

        await _sut.InitializeAsync();
        _sut.GetRuleCount().Should().Be(1, "precondition: one rule loaded initially");

        // Arrange - update repository to return different rules
        var updatedEntities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*new-tracker.com/*", "New Rule 1"),
            CreateBlockRuleEntity("*new-ads.com/*", "New Rule 2"),
            CreateBlockRuleEntity("*analytics.com/*", "New Rule 3")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(updatedEntities);

        // Act
        await _sut.ReloadRulesAsync();

        // Assert
        _sut.GetRuleCount().Should().Be(3);
        _sut.GetActiveRules().Select(r => r.Name).Should().Contain("New Rule 1");
    }

    [Fact]
    public async Task Evaluate_WithMatchingBlockRule_ReturnsBlock()
    {
        // Arrange
        var ruleId = Guid.NewGuid().ToString();
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Block Trackers", id: ruleId)
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };

        // Act
        var result = _sut.Evaluate(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeTrue();
        result.BlockedByRuleId.Should().Be(ruleId);
        result.BlockedByRuleName.Should().Be("Block Trackers");
    }

    [Fact]
    public async Task Evaluate_WithNoMatchingRules_ReturnsAllow()
    {
        // Arrange
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Block Trackers")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://safe-site.com/page.html" };

        // Act
        var result = _sut.Evaluate(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_WithDisabledRules_IgnoresThem()
    {
        // Arrange - GetEnabledAsync only returns enabled rules,
        // so disabled rules are filtered at the repository level
        var entities = new List<RuleEntity>();  // empty = no enabled rules

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };

        // Act
        var result = _sut.Evaluate(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeFalse("disabled rules are not returned by GetEnabledAsync");
        _sut.GetRuleCount().Should().Be(0);
    }

    [Fact]
    public async Task GetInjectionsForPage_ReturnsCssAndJsInjections()
    {
        // Arrange
        var entities = new List<RuleEntity>
        {
            CreateInjectionRuleEntity("inject_css", site: "*", urlPattern: "*", css: "body { display: none; }"),
            CreateInjectionRuleEntity("inject_js", site: "*", urlPattern: "*", js: "console.log('injected');")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        // Act
        var injections = _sut.GetInjectionsForPage("https://example.com/page").ToList();

        // Assert
        injections.Should().HaveCount(2);
        injections.Should().Contain(a => a.Type == "inject_css");
        injections.Should().Contain(a => a.Type == "inject_js");
    }

    [Fact]
    public async Task GetInjectionsForPage_NoMatch_ReturnsEmpty()
    {
        // Arrange - injection rule with a specific site pattern that won't match
        var entities = new List<RuleEntity>
        {
            CreateInjectionRuleEntity("inject_css", site: "*.specific-site.com", urlPattern: "*", css: "body { color: red; }")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        // Act
        var injections = _sut.GetInjectionsForPage("https://different-site.com/page").ToList();

        // Assert
        injections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveRules_ReturnsLoadedRules()
    {
        // Arrange
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Rule A"),
            CreateBlockRuleEntity("*ads.com/*", "Rule B")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        // Act
        var activeRules = _sut.GetActiveRules().ToList();

        // Assert
        activeRules.Should().HaveCount(2);
        activeRules.Select(r => r.Name).Should().Contain("Rule A");
        activeRules.Select(r => r.Name).Should().Contain("Rule B");
    }

    [Fact]
    public async Task GetRuleCount_ReturnsCorrectCount()
    {
        // Arrange
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*a.com/*", "Rule 1"),
            CreateBlockRuleEntity("*b.com/*", "Rule 2"),
            CreateBlockRuleEntity("*c.com/*", "Rule 3")
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        // Act
        var count = _sut.GetRuleCount();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task Evaluate_HigherPriorityRuleWins()
    {
        // Arrange - two rules match; higher priority rule should win
        var lowPriorityId = Guid.NewGuid().ToString();
        var highPriorityId = Guid.NewGuid().ToString();

        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Low Priority Block", priority: 5, id: lowPriorityId),
            CreateBlockRuleEntity("*tracker.com/*", "High Priority Block", priority: 100, id: highPriorityId)
        };

        _mockRuleRepository
            .Setup(r => r.GetEnabledAsync())
            .ReturnsAsync(entities);

        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };

        // Act
        var result = _sut.Evaluate(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeTrue();
        result.BlockedByRuleId.Should().Be(highPriorityId, "the higher priority rule should be evaluated first and win");
        result.BlockedByRuleName.Should().Be("High Priority Block");
    }

    [Fact]
    public async Task Evaluate_WithNullPageUrl_StillEvaluates()
    {
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Block Trackers")
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(entities);
        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var result = _sut.Evaluate(request, null);

        result.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_WithWildcardSite_MatchesAll()
    {
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Block All Sites", site: "*")
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(entities);
        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var result = _sut.Evaluate(request, "https://any-page.com");

        result.ShouldBlock.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_WithSpecificSite_OnlyMatchesThatSite()
    {
        var entities = new List<RuleEntity>
        {
            CreateBlockRuleEntity("*tracker.com/*", "Site-Specific Rule", site: "*.example.com/*")
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(entities);
        await _sut.InitializeAsync();

        // Should match when page is on example.com
        var request1 = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var matchResult = _sut.Evaluate(request1, "https://www.example.com/page");
        matchResult.ShouldBlock.Should().BeTrue();

        // Should NOT match when page is on other-site.com (different URL to avoid cache)
        var request2 = new NetworkRequest { Url = "https://tracker.com/pixel2.gif" };
        var noMatchResult = _sut.Evaluate(request2, "https://other-site.com/page");
        noMatchResult.ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_CollectsInjectionsFromMultipleRules()
    {
        var entities = new List<RuleEntity>
        {
            CreateInjectionRuleEntity("inject_css", site: "*", css: "body { color: red; }"),
            CreateInjectionRuleEntity("inject_js", site: "*", js: "console.log('hi');"),
            CreateInjectionRuleEntity("inject_css", site: "*", css: ".ad { display: none; }")
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(entities);
        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://example.com/page.html" };
        var result = _sut.Evaluate(request, "https://example.com");

        result.ShouldBlock.Should().BeFalse();
        result.InjectionsToApply.Should().HaveCount(3);
    }

    [Fact]
    public async Task Evaluate_BlockRuleWithResourceTypeFilter_Works()
    {
        // Create a rule that blocks only scripts
        var entity = new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Block Scripts Only",
            Description = "Blocks scripts from tracker",
            Site = "*",
            Enabled = true,
            Priority = 10,
            RulesJson = "[{\"Type\":\"block\",\"Match\":{\"UrlPattern\":\"*tracker.com/*\",\"ResourceType\":\"Script\"}}]",
            Source = "local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<RuleEntity> { entity });
        await _sut.InitializeAsync();

        var scriptRequest = new NetworkRequest { Url = "https://tracker.com/script.js", ResourceType = "Script" };
        _sut.Evaluate(scriptRequest, "https://example.com").ShouldBlock.Should().BeTrue();

        // Use a different URL to avoid evaluation cache hit
        var imageRequest = new NetworkRequest { Url = "https://tracker.com/image.png", ResourceType = "Image" };
        _sut.Evaluate(imageRequest, "https://example.com").ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public async Task Evaluate_BlockRuleWithMethodFilter_Works()
    {
        var entity = new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Block POST only",
            Description = "Blocks POST requests to tracker",
            Site = "*",
            Enabled = true,
            Priority = 10,
            RulesJson = "[{\"Type\":\"block\",\"Match\":{\"UrlPattern\":\"*tracker.com/*\",\"Method\":\"POST\"}}]",
            Source = "local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<RuleEntity> { entity });
        await _sut.InitializeAsync();

        var postRequest = new NetworkRequest { Url = "https://tracker.com/collect", Method = "POST" };
        _sut.Evaluate(postRequest, "https://example.com").ShouldBlock.Should().BeTrue();

        // Use a different URL to avoid evaluation cache hit
        var getRequest = new NetworkRequest { Url = "https://tracker.com/collect?nocache=1", Method = "GET" };
        _sut.Evaluate(getRequest, "https://example.com").ShouldBlock.Should().BeFalse();
    }

    [Fact]
    public async Task ReloadRulesAsync_WhenRepoThrows_HandlesGracefully()
    {
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        // Should not throw
        var ex = await Record.ExceptionAsync(() => _sut.ReloadRulesAsync());
        ex.Should().BeNull();

        // Should have 0 rules (failed to load)
        _sut.GetRuleCount().Should().Be(0);
    }

    [Fact]
    public async Task Evaluate_WithMalformedRulesJson_HandlesGracefully()
    {
        var entity = new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Broken Rule",
            Description = "Has malformed JSON",
            Site = "*",
            Enabled = true,
            Priority = 10,
            RulesJson = "this is not valid json at all {{{",
            Source = "local",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _mockRuleRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<RuleEntity> { entity });

        await _sut.InitializeAsync();

        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var result = _sut.Evaluate(request, "https://example.com");

        // Malformed rules should not block (rule loaded with empty actions list)
        result.ShouldBlock.Should().BeFalse();
    }
}
