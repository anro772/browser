using BrowserApp.Core.Interfaces;
using BrowserApp.Core.Models;
using BrowserApp.UI.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BrowserApp.Tests.Services;

public class BlockingServiceTests
{
    private readonly Mock<IRuleEngine> _mockRuleEngine;
    private readonly BlockingService _sut;

    public BlockingServiceTests()
    {
        _mockRuleEngine = new Mock<IRuleEngine>();
        _sut = new BlockingService(_mockRuleEngine.Object);
    }

    [Fact]
    public async Task InitializeAsync_CallsRuleEngineInitialize()
    {
        // Arrange
        _mockRuleEngine
            .Setup(r => r.InitializeAsync())
            .Returns(Task.CompletedTask);
        _mockRuleEngine
            .Setup(r => r.GetActiveRules())
            .Returns(Enumerable.Empty<Rule>());

        // Act
        await _sut.InitializeAsync();

        // Assert
        _mockRuleEngine.Verify(r => r.InitializeAsync(), Times.Once);
    }

    [Fact]
    public void ShouldBlockRequest_WhenRuleBlocks_ReturnsBlockResult()
    {
        // Arrange
        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var blockResult = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        _mockRuleEngine
            .Setup(r => r.Evaluate(request, It.IsAny<string?>()))
            .Returns(blockResult);

        // Act
        var result = _sut.ShouldBlockRequest(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeTrue();
        result.BlockedByRuleId.Should().Be("rule-1");
        result.BlockedByRuleName.Should().Be("Block Trackers");
    }

    [Fact]
    public void ShouldBlockRequest_WhenRuleAllows_ReturnsAllowResult()
    {
        // Arrange
        var request = new NetworkRequest { Url = "https://example.com/page.html" };
        var allowResult = RuleEvaluationResult.Allow();

        _mockRuleEngine
            .Setup(r => r.Evaluate(request, It.IsAny<string?>()))
            .Returns(allowResult);

        // Act
        var result = _sut.ShouldBlockRequest(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeFalse();
        result.BlockedByRuleId.Should().BeNull();
    }

    [Fact]
    public void ShouldBlockRequest_WhenBlocked_IncrementsBlockedCount()
    {
        // Arrange
        var request = new NetworkRequest { Url = "https://tracker.com/script.js" };
        var blockResult = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        _mockRuleEngine
            .Setup(r => r.Evaluate(request, It.IsAny<string?>()))
            .Returns(blockResult);

        // Act
        _sut.ShouldBlockRequest(request, "https://example.com");
        _sut.ShouldBlockRequest(request, "https://example.com");
        _sut.ShouldBlockRequest(request, "https://example.com");

        // Assert
        _sut.GetBlockedCount().Should().Be(3);
    }

    [Fact]
    public void ShouldBlockRequest_WhenBlocked_AddsBytesSaved()
    {
        // Arrange
        var requestWithSize = new NetworkRequest
        {
            Url = "https://tracker.com/large.js",
            Size = 12345
        };
        var blockResult = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        _mockRuleEngine
            .Setup(r => r.Evaluate(requestWithSize, It.IsAny<string?>()))
            .Returns(blockResult);

        // Act
        _sut.ShouldBlockRequest(requestWithSize, "https://example.com");

        // Assert
        _sut.GetBytesSaved().Should().Be(12345);
    }

    [Fact]
    public void ShouldBlockRequest_WhenBlockedWithNoSize_UsesDefaultEstimate()
    {
        // Arrange
        var requestNoSize = new NetworkRequest
        {
            Url = "https://tracker.com/pixel.gif",
            Size = null
        };
        var blockResult = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        _mockRuleEngine
            .Setup(r => r.Evaluate(requestNoSize, It.IsAny<string?>()))
            .Returns(blockResult);

        // Act
        _sut.ShouldBlockRequest(requestNoSize, "https://example.com");

        // Assert
        _sut.GetBytesSaved().Should().Be(5000); // Default 5KB estimate
    }

    [Fact]
    public void ShouldBlockRequest_WhenEvaluateThrows_ReturnsAllow()
    {
        // Arrange
        var request = new NetworkRequest { Url = "https://example.com/page.html" };

        _mockRuleEngine
            .Setup(r => r.Evaluate(request, It.IsAny<string?>()))
            .Throws(new InvalidOperationException("Engine error"));

        // Act
        var result = _sut.ShouldBlockRequest(request, "https://example.com");

        // Assert
        result.ShouldBlock.Should().BeFalse("service should fail-open when evaluation throws");
    }

    [Fact]
    public void GetBlockedCount_ReturnsZeroInitially()
    {
        // Arrange - fresh service, no requests processed

        // Act
        var count = _sut.GetBlockedCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void ResetStats_ResetsCountAndBytes()
    {
        // Arrange
        var request = new NetworkRequest
        {
            Url = "https://tracker.com/script.js",
            Size = 8000
        };
        var blockResult = RuleEvaluationResult.Block("rule-1", "Block Trackers");

        _mockRuleEngine
            .Setup(r => r.Evaluate(request, It.IsAny<string?>()))
            .Returns(blockResult);

        _sut.ShouldBlockRequest(request, "https://example.com");
        _sut.GetBlockedCount().Should().BeGreaterThan(0, "precondition: count should be non-zero before reset");
        _sut.GetBytesSaved().Should().BeGreaterThan(0, "precondition: bytes should be non-zero before reset");

        // Act
        _sut.ResetStats();

        // Assert
        _sut.GetBlockedCount().Should().Be(0);
        _sut.GetBytesSaved().Should().Be(0);
    }

    [Fact]
    public void ShouldBlockRequest_WithNullPageUrl_DoesNotCrash()
    {
        var request = new NetworkRequest { Url = "https://tracker.com/pixel.gif" };
        var allowResult = RuleEvaluationResult.Allow();
        _mockRuleEngine.Setup(r => r.Evaluate(request, null)).Returns(allowResult);

        var ex = Record.Exception(() => _sut.ShouldBlockRequest(request, null));
        ex.Should().BeNull();
    }

    [Fact]
    public void ShouldBlockRequest_MultipleBlocks_AccumulatesStats()
    {
        var request1 = new NetworkRequest { Url = "https://tracker.com/a.js", Size = 1000 };
        var request2 = new NetworkRequest { Url = "https://tracker.com/b.js", Size = 2000 };
        var request3 = new NetworkRequest { Url = "https://tracker.com/c.js", Size = 3000 };

        _mockRuleEngine.Setup(r => r.Evaluate(It.IsAny<NetworkRequest>(), It.IsAny<string?>()))
            .Returns(RuleEvaluationResult.Block("rule-1", "Block"));

        _sut.ShouldBlockRequest(request1, "https://example.com");
        _sut.ShouldBlockRequest(request2, "https://example.com");
        _sut.ShouldBlockRequest(request3, "https://example.com");

        _sut.GetBlockedCount().Should().Be(3);
        _sut.GetBytesSaved().Should().Be(6000);
    }

    [Fact]
    public void GetBytesSaved_AfterMultipleBlocks_ReturnsTotalSize()
    {
        var request1 = new NetworkRequest { Url = "https://tracker.com/a.js", Size = 5000 };
        var request2 = new NetworkRequest { Url = "https://tracker.com/b.js", Size = 15000 };

        _mockRuleEngine.Setup(r => r.Evaluate(It.IsAny<NetworkRequest>(), It.IsAny<string?>()))
            .Returns(RuleEvaluationResult.Block("rule-1", "Block"));

        _sut.ShouldBlockRequest(request1, null);
        _sut.ShouldBlockRequest(request2, null);

        _sut.GetBytesSaved().Should().Be(20000);
    }

    [Fact]
    public void ResetStats_ThenBlock_StartsFromZero()
    {
        var request = new NetworkRequest { Url = "https://tracker.com/a.js", Size = 9000 };
        _mockRuleEngine.Setup(r => r.Evaluate(It.IsAny<NetworkRequest>(), It.IsAny<string?>()))
            .Returns(RuleEvaluationResult.Block("rule-1", "Block"));

        _sut.ShouldBlockRequest(request, null);
        _sut.GetBlockedCount().Should().Be(1);
        _sut.GetBytesSaved().Should().Be(9000);

        _sut.ResetStats();
        _sut.GetBlockedCount().Should().Be(0);
        _sut.GetBytesSaved().Should().Be(0);

        _sut.ShouldBlockRequest(request, null);
        _sut.GetBlockedCount().Should().Be(1);
        _sut.GetBytesSaved().Should().Be(9000);
    }

    [Fact]
    public void ShouldBlockRequest_WhenResultHasInjections_ReturnsResult()
    {
        var request = new NetworkRequest { Url = "https://example.com/page.html" };
        var resultWithInjections = new RuleEvaluationResult
        {
            ShouldBlock = false,
            InjectionsToApply = new List<RuleAction>
            {
                new() { Type = "inject_css", Css = "body { color: red; }" },
                new() { Type = "inject_js", Js = "console.log('hi');" }
            }
        };

        _mockRuleEngine.Setup(r => r.Evaluate(request, It.IsAny<string?>())).Returns(resultWithInjections);

        var result = _sut.ShouldBlockRequest(request, "https://example.com");

        result.ShouldBlock.Should().BeFalse();
        result.InjectionsToApply.Should().HaveCount(2);
        _sut.GetBlockedCount().Should().Be(0, "injections should not increment blocked count");
    }
}
