using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BrowserApp.Tests.Repositories;

public class RuleRepositoryTests : IDisposable
{
    private readonly BrowserDbContext _context;
    private readonly RuleRepository _repository;

    public RuleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrowserDbContext(options);
        _repository = new RuleRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllRules_OrderedByPriorityDescThenName()
    {
        // Arrange
        var ruleA = CreateTestRule(name: "Beta Rule", priority: 5);
        var ruleB = CreateTestRule(name: "Alpha Rule", priority: 10);
        var ruleC = CreateTestRule(name: "Gamma Rule", priority: 10);

        _context.Rules.AddRange(ruleA, ruleB, ruleC);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha Rule");
        result[1].Name.Should().Be("Gamma Rule");
        result[2].Name.Should().Be("Beta Rule");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoRulesExist()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEnabledAsync

    [Fact]
    public async Task GetEnabledAsync_ReturnsOnlyEnabledRules_OrderedByPriorityDesc()
    {
        // Arrange
        var enabledHigh = CreateTestRule(name: "High Priority", priority: 20, enabled: true);
        var enabledLow = CreateTestRule(name: "Low Priority", priority: 5, enabled: true);
        var disabled = CreateTestRule(name: "Disabled Rule", priority: 50, enabled: false);

        _context.Rules.AddRange(enabledHigh, enabledLow, disabled);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetEnabledAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Priority.Should().Be(20);
        result[1].Priority.Should().Be(5);
        result.Should().NotContain(r => r.Name == "Disabled Rule");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule(name: "Test Rule");
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(rule.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Rule");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid().ToString());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_AddsRuleAndSetsTimestamps()
    {
        // Arrange
        var rule = CreateTestRule(name: "New Rule");
        var beforeAdd = DateTime.UtcNow;

        // Act
        await _repository.AddAsync(rule);

        // Assert
        var saved = await _context.Rules.FindAsync(rule.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Rule");
        saved.CreatedAt.Should().BeOnOrAfter(beforeAdd);
        saved.UpdatedAt.Should().BeOnOrAfter(beforeAdd);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingRule_UpdatesFieldsAndTimestamp()
    {
        // Arrange
        var rule = CreateTestRule(name: "Original Name", priority: 5);
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = rule.UpdatedAt;
        await Task.Delay(10); // Ensure timestamp difference

        var updatePayload = new RuleEntity
        {
            Id = rule.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            Site = "https://updated.com",
            Enabled = false,
            Priority = 99,
            RulesJson = "[{\"type\":\"block\"}]",
            Source = "marketplace",
            ChannelId = "channel-1",
            IsEnforced = true
        };

        // Act
        await _repository.UpdateAsync(updatePayload);

        // Assert
        var updated = await _context.Rules.FindAsync(rule.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
        updated.Site.Should().Be("https://updated.com");
        updated.Enabled.Should().BeFalse();
        updated.Priority.Should().Be(99);
        updated.RulesJson.Should().Be("[{\"type\":\"block\"}]");
        updated.Source.Should().Be("marketplace");
        updated.ChannelId.Should().Be("channel-1");
        updated.IsEnforced.Should().BeTrue();
        updated.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentRule_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentRule = CreateTestRule(name: "Ghost Rule");

        // Act
        Func<Task> act = () => _repository.UpdateAsync(nonExistentRule);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentRule.Id}*");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesRule()
    {
        // Arrange
        var rule = CreateTestRule(name: "To Delete");
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(rule.Id);

        // Assert
        var deleted = await _context.Rules.FindAsync(rule.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNothing()
    {
        // Arrange
        var rule = CreateTestRule(name: "Survivor");
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(Guid.NewGuid().ToString());

        // Assert
        var count = await _context.Rules.CountAsync();
        count.Should().Be(1);
    }

    #endregion

    #region GetCountAsync

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _context.Rules.AddRange(
            CreateTestRule(name: "Rule 1"),
            CreateTestRule(name: "Rule 2"),
            CreateTestRule(name: "Rule 3")
        );
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        count.Should().Be(3);
    }

    #endregion

    #region GetBySourceAsync

    [Fact]
    public async Task GetBySourceAsync_FiltersAndOrdersByPriority()
    {
        // Arrange
        var local1 = CreateTestRule(name: "Local Low", source: "local", priority: 5);
        var local2 = CreateTestRule(name: "Local High", source: "local", priority: 20);
        var marketplace = CreateTestRule(name: "Marketplace", source: "marketplace", priority: 100);

        _context.Rules.AddRange(local1, local2, marketplace);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetBySourceAsync("local")).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Priority.Should().Be(20);
        result[1].Priority.Should().Be(5);
        result.Should().NotContain(r => r.Source == "marketplace");
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var rule = CreateTestRule(name: "Existing Rule");
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(rule.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentId_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid().ToString());

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region DeleteByChannelIdAsync

    [Fact]
    public async Task DeleteByChannelIdAsync_RemovesAllRulesWithMatchingChannelId()
    {
        // Arrange
        var channelRule1 = CreateTestRule(name: "Channel Rule 1", channelId: "ch-42");
        var channelRule2 = CreateTestRule(name: "Channel Rule 2", channelId: "ch-42");
        var otherRule = CreateTestRule(name: "Other Rule", channelId: "ch-99");
        var localRule = CreateTestRule(name: "Local Rule");

        _context.Rules.AddRange(channelRule1, channelRule2, otherRule, localRule);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteByChannelIdAsync("ch-42");

        // Assert
        var remaining = await _context.Rules.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(r => r.ChannelId == "ch-42");
    }

    #endregion

    #region GetByMarketplaceIdAsync

    [Fact]
    public async Task GetByMarketplaceIdAsync_ExistingMarketplaceId_ReturnsRule()
    {
        // Arrange
        var rule = CreateTestRule(name: "Marketplace Rule", marketplaceId: "mp-123");
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByMarketplaceIdAsync("mp-123");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Marketplace Rule");
    }

    [Fact]
    public async Task GetByMarketplaceIdAsync_NonExistentMarketplaceId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByMarketplaceIdAsync("mp-nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helpers

    private static RuleEntity CreateTestRule(
        string name = "Test Rule",
        string description = "Test Description",
        string site = "*",
        bool enabled = true,
        int priority = 10,
        string source = "local",
        string? channelId = null,
        string? marketplaceId = null)
    {
        return new RuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Site = site,
            Enabled = enabled,
            Priority = priority,
            RulesJson = "[]",
            Source = source,
            ChannelId = channelId,
            MarketplaceId = marketplaceId,
            IsEnforced = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
