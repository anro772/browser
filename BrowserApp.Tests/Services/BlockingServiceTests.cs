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
}
