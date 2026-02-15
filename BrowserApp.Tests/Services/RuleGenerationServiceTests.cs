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
    public async Task GenerateRuleSuggestionsAsync_ReturnsRulesFromValidJson()
    {
        var rules = new List<Rule>
        {
            new()
            {
                Name = "Block Trackers",
                Description = "Blocks tracking scripts",
                Site = "*.example.com",
                Rules = new List<RuleAction>
                {
                    new() { Type = "block", Match = new RuleMatch { UrlPattern = "*tracker.com/*" } }
                }
            }
        };
        var json = JsonSerializer.Serialize(rules);

        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync(json);

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com", "Example");

        Assert.Single(result);
        Assert.Equal("Block Trackers", result[0].Name);
        Assert.Equal("ai", result[0].Source);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_HandlesMarkdownWrappedJson()
    {
        var rules = new List<Rule>
        {
            new()
            {
                Name = "Block Ads",
                Description = "Blocks ad scripts",
                Site = "*",
                Rules = new List<RuleAction>
                {
                    new() { Type = "block", Match = new RuleMatch { UrlPattern = "*ads.com/*" } }
                }
            }
        };
        var json = "```json\n" + JsonSerializer.Serialize(rules) + "\n```";

        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync(json);

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.Single(result);
        Assert.Equal("Block Ads", result[0].Name);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_ReturnsEmptyOnInvalidJson()
    {
        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync("This is not valid JSON at all");

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_ReturnsEmptyOnException()
    {
        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_IncludesUrlInPrompt()
    {
        List<OllamaChatMessage>? capturedMessages = null;
        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .Callback<List<OllamaChatMessage>, string?>((msgs, _) => capturedMessages = msgs)
            .ReturnsAsync("[]");

        await _service.GenerateRuleSuggestionsAsync("https://test.com", "Test Page");

        Assert.NotNull(capturedMessages);
        var userMsg = capturedMessages!.Last();
        Assert.Equal("user", userMsg.Role);
        Assert.Contains("https://test.com", userMsg.Content);
        Assert.Contains("Test Page", userMsg.Content);
    }

    [Fact]
    public async Task GenerateRuleSuggestionsAsync_AssignsIdsToRulesWithoutIds()
    {
        var json = "[{\"Name\":\"Test\",\"Description\":\"test\",\"Site\":\"*\",\"Rules\":[]}]";

        _ollamaClientMock
            .Setup(x => x.ChatAsync(It.IsAny<List<OllamaChatMessage>>(), It.IsAny<string?>()))
            .ReturnsAsync(json);

        var result = await _service.GenerateRuleSuggestionsAsync("https://example.com");

        Assert.Single(result);
        Assert.False(string.IsNullOrEmpty(result[0].Id));
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
}
