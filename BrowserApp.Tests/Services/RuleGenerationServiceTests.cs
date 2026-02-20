using System.Text.Json;
using BrowserApp.Core.DTOs.Ollama;
using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.Services;

public class RuleGenerationServiceTests
{
    private readonly Mock<IOllamaClient> _ollamaClientMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IRuleRepository> _ruleRepoMock;
    private readonly Mock<IRuleEngine> _ruleEngineMock;
    private readonly RuleGenerationService _service;

    public RuleGenerationServiceTests()
    {
        _ollamaClientMock = new Mock<IOllamaClient>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _ruleRepoMock = new Mock<IRuleRepository>();
        _ruleEngineMock = new Mock<IRuleEngine>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRuleRepository)))
            .Returns(_ruleRepoMock.Object);

        _service = new RuleGenerationService(_ollamaClientMock.Object, _scopeFactoryMock.Object, _ruleEngineMock.Object);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_ReturnsStandardRules()
    {
        // AI domain suggestions - return empty so we just get standard rules
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com", "Example");

        // Should have at least 3 standard rules: block networks, hide ads, remove popups
        Assert.True(result.Count >= 3);
        Assert.Contains(result, r => r.Name.Contains("Block Ad Networks"));
        Assert.Contains(result, r => r.Name.Contains("Hide Ad Elements"));
        Assert.Contains(result, r => r.Name.Contains("Remove Popups"));
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_SetsAiSource()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.All(result, r => Assert.Equal("ai", r.Source));
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_AssignsIds()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.All(result, r => Assert.False(string.IsNullOrEmpty(r.Id)));
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_EmptyUrl_ReturnsEmpty()
    {
        var result = await _service.GenerateRuleSuggestionsAsync("");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_BlockRuleContainsAdDomains()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");
        var blockRule = result.First(r => r.Name.Contains("Block Ad Networks"));

        // Should contain standard ad network block actions
        Assert.Contains(blockRule.Rules, a => a.Match.UrlPattern!.Contains("doubleclick.net"));
        Assert.Contains(blockRule.Rules, a => a.Match.UrlPattern!.Contains("googlesyndication.com"));
        Assert.Contains(blockRule.Rules, a => a.Match.UrlPattern!.Contains("google-analytics.com"));
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_CssRuleHidesAdElements()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");
        var cssRule = result.First(r => r.Name.Contains("Hide Ad Elements"));

        Assert.Single(cssRule.Rules);
        Assert.Equal("inject_css", cssRule.Rules[0].Type);
        Assert.Contains("display: none", cssRule.Rules[0].Css);
        Assert.Contains(".ad-container", cssRule.Rules[0].Css);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_JsRuleRemovesPopups()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");
        var jsRule = result.First(r => r.Name.Contains("Remove Popups"));

        Assert.Single(jsRule.Rules);
        Assert.Equal("inject_js", jsRule.Rules[0].Type);
        Assert.Contains("overflow", jsRule.Rules[0].Js);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_AiDomainSuggestions_AddsExtraBlockRule()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[\"siteads.example.net\",\"tracker.example.net\"]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        // Should have 4 rules: 3 standard + 1 AI-suggested
        Assert.Equal(4, result.Count);
        var aiRule = result.First(r => r.Name.Contains("Site-Specific"));
        Assert.Contains(aiRule.Rules, a => a.Match.UrlPattern!.Contains("siteads.example.net"));
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_AiFailure_StillReturnsStandardRules()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        // Standard rules should still be generated even if AI fails
        Assert.True(result.Count >= 3);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_SetsSitePattern()
    {
        _ollamaClientMock
            .Setup(x => x.ChatJsonAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("{\"domains\":[]}");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.All(result, r => Assert.Equal("*.example.com", r.Site));
    }

    [Fact]
    public async Task ApplyRuleAsync_SavesRuleAndReloadsEngine()
    {
        var rule = new Rule
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Rule",
            Description = "A test rule",
            Site = "*.example.com",
            Rules = new List<RuleAction>
            {
                new() { Type = "block", Match = new RuleMatch { UrlPattern = "*tracker*" } }
            }
        };

        await _service.ApplyRuleAsync(rule);

        _ruleRepoMock.Verify(x => x.AddAsync(It.Is<RuleEntity>(e =>
            e.Id == rule.Id &&
            e.Name == rule.Name &&
            e.Source == "ai"
        )), Times.Once);

        _ruleEngineMock.Verify(x => x.ReloadRulesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApplyRuleAsync_SetsSourceToAi()
    {
        RuleEntity? capturedEntity = null;
        _ruleRepoMock
            .Setup(x => x.AddAsync(It.IsAny<RuleEntity>()))
            .Callback<RuleEntity>(e => capturedEntity = e)
            .Returns(Task.CompletedTask);

        var rule = new Rule
        {
            Name = "AI Rule",
            Source = "local", // Should be overridden
            Rules = new List<RuleAction>()
        };

        await _service.ApplyRuleAsync(rule);

        Assert.NotNull(capturedEntity);
        Assert.Equal("ai", capturedEntity!.Source);
    }

    [Fact]
    public async Task ApplyRuleAsync_WhenRepoThrows_PropagatesException()
    {
        _ruleRepoMock
            .Setup(x => x.AddAsync(It.IsAny<RuleEntity>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));
        var rule = new Rule { Name = "Test", Rules = new List<RuleAction>() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyRuleAsync(rule));
    }

    [Fact]
    public async Task ApplyRuleAsync_SerializesRulesJsonCorrectly()
    {
        RuleEntity? capturedEntity = null;
        _ruleRepoMock
            .Setup(x => x.AddAsync(It.IsAny<RuleEntity>()))
            .Callback<RuleEntity>(e => capturedEntity = e)
            .Returns(Task.CompletedTask);
        var rule = new Rule
        {
            Name = "JSON Test",
            Rules = new List<RuleAction>
            {
                new() { Type = "block", Match = new RuleMatch { UrlPattern = "*test*" } }
            }
        };
        await _service.ApplyRuleAsync(rule);
        Assert.NotNull(capturedEntity);
        Assert.Contains("block", capturedEntity!.RulesJson);
        Assert.Contains("*test*", capturedEntity.RulesJson);
    }
}
