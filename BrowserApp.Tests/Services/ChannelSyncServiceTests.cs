using BrowserApp.Core.DTOs;
using BrowserApp.Core.Interfaces;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;
using BrowserApp.UI.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BrowserApp.Tests.Services;

public class ChannelSyncServiceTests
{
    private readonly Mock<IChannelApiClient> _mockApiClient;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IRuleEngine> _mockRuleEngine;
    private readonly Mock<IRuleRepository> _mockRuleRepository;
    private readonly Mock<IChannelMembershipRepository> _mockMembershipRepository;
    private readonly ChannelSyncService _sut;

    private readonly Guid _testChannelId = Guid.NewGuid();
    private const string TestUsername = "testuser";
    private const string TestPassword = "testpass";
    private const string TestChannelName = "Test Channel";
    private const string TestChannelDescription = "A test channel";

    public ChannelSyncServiceTests()
    {
        _mockApiClient = new Mock<IChannelApiClient>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockRuleEngine = new Mock<IRuleEngine>();
        _mockRuleRepository = new Mock<IRuleRepository>();
        _mockMembershipRepository = new Mock<IChannelMembershipRepository>();

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
        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IChannelMembershipRepository)))
            .Returns(_mockMembershipRepository.Object);

        _sut = new ChannelSyncService(
            _mockApiClient.Object,
            _mockScopeFactory.Object,
            _mockRuleEngine.Object);
    }

    [Fact]
    public async Task JoinChannelAsync_Success_SavesMembershipAndSyncsRules()
    {
        // Arrange
        _mockApiClient
            .Setup(c => c.JoinChannelAsync(_testChannelId, TestUsername, TestPassword))
            .ReturnsAsync(true);

        _mockMembershipRepository
            .Setup(r => r.GetByChannelIdAsync(_testChannelId.ToString()))
            .ReturnsAsync((ChannelMembershipEntity?)null);

        _mockMembershipRepository
            .Setup(r => r.AddAsync(It.IsAny<ChannelMembershipEntity>()))
            .Returns(Task.CompletedTask);

        var rulesResponse = new ChannelRuleListResponse
        {
            ChannelId = _testChannelId,
            ChannelName = TestChannelName,
            Rules = new List<ChannelRuleResponse>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ChannelId = _testChannelId,
                    Name = "Block Ads",
                    Description = "Blocks ad trackers",
                    Site = "*",
                    Priority = 10,
                    RulesJson = "[{\"Type\":\"block\",\"Match\":{\"UrlPattern\":\"*ads.com/*\"}}]",
                    IsEnforced = true,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetChannelRulesAsync(_testChannelId, TestUsername))
            .ReturnsAsync(rulesResponse);

        _mockRuleRepository
            .Setup(r => r.DeleteByChannelIdAsync(_testChannelId.ToString()))
            .Returns(Task.CompletedTask);

        _mockRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEntity>()))
            .Returns(Task.CompletedTask);

        _mockMembershipRepository
            .Setup(r => r.UpdateLastSyncedAsync(_testChannelId.ToString(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _mockRuleEngine
            .Setup(r => r.ReloadRulesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.JoinChannelAsync(
            _testChannelId, TestChannelName, TestChannelDescription, TestUsername, TestPassword);

        // Assert
        result.Should().BeTrue();
        _mockMembershipRepository.Verify(
            r => r.AddAsync(It.Is<ChannelMembershipEntity>(e =>
                e.ChannelId == _testChannelId.ToString() &&
                e.ChannelName == TestChannelName &&
                e.Username == TestUsername)),
            Times.Once);
        _mockRuleEngine.Verify(r => r.ReloadRulesAsync(), Times.Once);
    }

    [Fact]
    public async Task JoinChannelAsync_ServerRejects_ReturnsFalse()
    {
        // Arrange
        _mockApiClient
            .Setup(c => c.JoinChannelAsync(_testChannelId, TestUsername, TestPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.JoinChannelAsync(
            _testChannelId, TestChannelName, TestChannelDescription, TestUsername, TestPassword);

        // Assert
        result.Should().BeFalse();
        _mockMembershipRepository.Verify(
            r => r.AddAsync(It.IsAny<ChannelMembershipEntity>()),
            Times.Never,
            "membership should not be saved when server rejects the join");
    }

    [Fact]
    public async Task JoinChannelAsync_Exception_ReturnsFalse()
    {
        // Arrange
        _mockApiClient
            .Setup(c => c.JoinChannelAsync(_testChannelId, TestUsername, TestPassword))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.JoinChannelAsync(
            _testChannelId, TestChannelName, TestChannelDescription, TestUsername, TestPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LeaveChannelAsync_Success_DeletesRulesAndMembership()
    {
        // Arrange
        var channelIdStr = _testChannelId.ToString();

        _mockApiClient
            .Setup(c => c.LeaveChannelAsync(_testChannelId, TestUsername))
            .ReturnsAsync(true);

        _mockRuleRepository
            .Setup(r => r.DeleteByChannelIdAsync(channelIdStr))
            .Returns(Task.CompletedTask);

        _mockMembershipRepository
            .Setup(r => r.DeleteByChannelIdAsync(channelIdStr))
            .Returns(Task.CompletedTask);

        _mockRuleEngine
            .Setup(r => r.ReloadRulesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LeaveChannelAsync(channelIdStr, TestUsername);

        // Assert
        result.Should().BeTrue();
        _mockRuleRepository.Verify(r => r.DeleteByChannelIdAsync(channelIdStr), Times.Once);
        _mockMembershipRepository.Verify(r => r.DeleteByChannelIdAsync(channelIdStr), Times.Once);
        _mockRuleEngine.Verify(r => r.ReloadRulesAsync(), Times.Once);
    }

    [Fact]
    public async Task LeaveChannelAsync_ServerFails_ReturnsFalse()
    {
        // Arrange
        var channelIdStr = _testChannelId.ToString();

        _mockApiClient
            .Setup(c => c.LeaveChannelAsync(_testChannelId, TestUsername))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.LeaveChannelAsync(channelIdStr, TestUsername);

        // Assert
        result.Should().BeFalse();
        _mockRuleRepository.Verify(
            r => r.DeleteByChannelIdAsync(It.IsAny<string>()),
            Times.Never,
            "local data should not be deleted when server call fails");
        _mockMembershipRepository.Verify(
            r => r.DeleteByChannelIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncChannelRulesAsync_SavesRulesFromServer()
    {
        // Arrange
        var channelIdStr = _testChannelId.ToString();
        var ruleId1 = Guid.NewGuid();
        var ruleId2 = Guid.NewGuid();

        var rulesResponse = new ChannelRuleListResponse
        {
            ChannelId = _testChannelId,
            ChannelName = TestChannelName,
            Rules = new List<ChannelRuleResponse>
            {
                new()
                {
                    Id = ruleId1,
                    ChannelId = _testChannelId,
                    Name = "Block Trackers",
                    Description = "Block tracking scripts",
                    Site = "*",
                    Priority = 10,
                    RulesJson = "[{\"Type\":\"block\",\"Match\":{\"UrlPattern\":\"*tracker.com/*\"}}]",
                    IsEnforced = true,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = ruleId2,
                    ChannelId = _testChannelId,
                    Name = "Block Ads",
                    Description = "Block ad networks",
                    Site = "*",
                    Priority = 20,
                    RulesJson = "[{\"Type\":\"block\",\"Match\":{\"UrlPattern\":\"*ads.net/*\"}}]",
                    IsEnforced = false,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockApiClient
            .Setup(c => c.GetChannelRulesAsync(_testChannelId, TestUsername))
            .ReturnsAsync(rulesResponse);

        _mockRuleRepository
            .Setup(r => r.DeleteByChannelIdAsync(channelIdStr))
            .Returns(Task.CompletedTask);

        _mockRuleRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEntity>()))
            .Returns(Task.CompletedTask);

        _mockMembershipRepository
            .Setup(r => r.UpdateLastSyncedAsync(channelIdStr, 2))
            .Returns(Task.CompletedTask);

        _mockRuleEngine
            .Setup(r => r.ReloadRulesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SyncChannelRulesAsync(channelIdStr, TestUsername);

        // Assert
        result.Should().BeTrue();

        // Verify old rules deleted first (server-wins strategy)
        _mockRuleRepository.Verify(r => r.DeleteByChannelIdAsync(channelIdStr), Times.Once);

        // Verify both rules were added
        _mockRuleRepository.Verify(r => r.AddAsync(It.IsAny<RuleEntity>()), Times.Exactly(2));

        // Verify sync metadata updated
        _mockMembershipRepository.Verify(r => r.UpdateLastSyncedAsync(channelIdStr, 2), Times.Once);

        // Verify rule engine was reloaded
        _mockRuleEngine.Verify(r => r.ReloadRulesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncAllChannelsAsync_ServerUnavailable_SkipsSync()
    {
        // Arrange
        _mockApiClient
            .Setup(c => c.CheckConnectionAsync())
            .ReturnsAsync(false);

        // Act
        await _sut.SyncAllChannelsAsync(TestUsername);

        // Assert
        _mockMembershipRepository.Verify(
            r => r.GetActiveAsync(),
            Times.Never,
            "should not attempt to load memberships when server is unavailable");
    }

    [Fact]
    public async Task IsServerAvailableAsync_DelegatesToApiClient()
    {
        // Arrange
        _mockApiClient
            .Setup(c => c.CheckConnectionAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsServerAvailableAsync();

        // Assert
        result.Should().BeTrue();
        _mockApiClient.Verify(c => c.CheckConnectionAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncChannelRulesAsync_EmptyRuleList_DeletesOldAndAddsNone()
    {
        var channelIdStr = _testChannelId.ToString();
        var rulesResponse = new ChannelRuleListResponse
        {
            ChannelId = _testChannelId,
            ChannelName = TestChannelName,
            Rules = new List<ChannelRuleResponse>() // empty list
        };

        _mockApiClient.Setup(c => c.GetChannelRulesAsync(_testChannelId, TestUsername)).ReturnsAsync(rulesResponse);
        _mockRuleRepository.Setup(r => r.DeleteByChannelIdAsync(channelIdStr)).Returns(Task.CompletedTask);
        _mockMembershipRepository.Setup(r => r.UpdateLastSyncedAsync(channelIdStr, 0)).Returns(Task.CompletedTask);
        _mockRuleEngine.Setup(r => r.ReloadRulesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.SyncChannelRulesAsync(channelIdStr, TestUsername);

        result.Should().BeTrue();
        _mockRuleRepository.Verify(r => r.DeleteByChannelIdAsync(channelIdStr), Times.Once);
        _mockRuleRepository.Verify(r => r.AddAsync(It.IsAny<RuleEntity>()), Times.Never);
        _mockMembershipRepository.Verify(r => r.UpdateLastSyncedAsync(channelIdStr, 0), Times.Once);
    }

    [Fact]
    public async Task SyncChannelRulesAsync_WhenApiReturnsNull_ReturnsFalse()
    {
        var channelIdStr = _testChannelId.ToString();
        _mockApiClient.Setup(c => c.GetChannelRulesAsync(_testChannelId, TestUsername))
            .ReturnsAsync((ChannelRuleListResponse?)null);

        var result = await _sut.SyncChannelRulesAsync(channelIdStr, TestUsername);

        result.Should().BeFalse();
        _mockRuleRepository.Verify(r => r.DeleteByChannelIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncAllChannelsAsync_WithMultipleChannels_SyncsAll()
    {
        var channel1Id = Guid.NewGuid();
        var channel2Id = Guid.NewGuid();

        _mockApiClient.Setup(c => c.CheckConnectionAsync()).ReturnsAsync(true);
        _mockMembershipRepository.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<ChannelMembershipEntity>
        {
            new() { ChannelId = channel1Id.ToString(), ChannelName = "Ch1", Username = TestUsername, IsActive = true },
            new() { ChannelId = channel2Id.ToString(), ChannelName = "Ch2", Username = TestUsername, IsActive = true }
        });

        // Both channels return empty rules (simplest valid response)
        _mockApiClient.Setup(c => c.GetChannelRulesAsync(It.IsAny<Guid>(), TestUsername))
            .ReturnsAsync(new ChannelRuleListResponse { Rules = new List<ChannelRuleResponse>() });
        _mockRuleRepository.Setup(r => r.DeleteByChannelIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockMembershipRepository.Setup(r => r.UpdateLastSyncedAsync(It.IsAny<string>(), 0)).Returns(Task.CompletedTask);
        _mockRuleEngine.Setup(r => r.ReloadRulesAsync()).Returns(Task.CompletedTask);

        await _sut.SyncAllChannelsAsync(TestUsername);

        _mockApiClient.Verify(c => c.GetChannelRulesAsync(It.IsAny<Guid>(), TestUsername), Times.Exactly(2));
    }

    [Fact]
    public async Task JoinChannelAsync_ExistingMembership_DoesNotDuplicate()
    {
        _mockApiClient.Setup(c => c.JoinChannelAsync(_testChannelId, TestUsername, TestPassword)).ReturnsAsync(true);

        // Membership already exists
        _mockMembershipRepository.Setup(r => r.GetByChannelIdAsync(_testChannelId.ToString()))
            .ReturnsAsync(new ChannelMembershipEntity
            {
                ChannelId = _testChannelId.ToString(),
                ChannelName = TestChannelName,
                Username = TestUsername
            });

        // Sync returns valid response
        _mockApiClient.Setup(c => c.GetChannelRulesAsync(_testChannelId, TestUsername))
            .ReturnsAsync(new ChannelRuleListResponse { ChannelId = _testChannelId, ChannelName = TestChannelName, Rules = new() });
        _mockRuleRepository.Setup(r => r.DeleteByChannelIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _mockMembershipRepository.Setup(r => r.UpdateLastSyncedAsync(It.IsAny<string>(), 0)).Returns(Task.CompletedTask);
        _mockRuleEngine.Setup(r => r.ReloadRulesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.JoinChannelAsync(_testChannelId, TestChannelName, TestChannelDescription, TestUsername, TestPassword);

        result.Should().BeTrue();
        _mockMembershipRepository.Verify(r => r.AddAsync(It.IsAny<ChannelMembershipEntity>()), Times.Never,
            "should not add duplicate membership");
    }

    [Fact]
    public async Task LeaveChannelAsync_InvalidGuid_ReturnsFalse()
    {
        // An invalid GUID string should cause Guid.Parse to throw, caught by the outer try/catch
        var result = await _sut.LeaveChannelAsync("not-a-valid-guid", TestUsername);

        result.Should().BeFalse();
        _mockRuleRepository.Verify(r => r.DeleteByChannelIdAsync(It.IsAny<string>()), Times.Never);
    }
}
