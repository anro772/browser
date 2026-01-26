using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BrowserApp.Tests.Repositories;

public class BrowsingHistoryRepositoryTests : IDisposable
{
    private readonly BrowserDbContext _context;
    private readonly BrowsingHistoryRepository _repository;

    public BrowsingHistoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrowserDbContext(options);
        _repository = new BrowsingHistoryRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsEntityToDatabase()
    {
        var entity = CreateTestEntity("https://example.com", "Example");

        await _repository.AddAsync(entity);

        var count = await _context.BrowsingHistory.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsRequestedCount()
    {
        for (int i = 0; i < 10; i++)
        {
            await _repository.AddAsync(CreateTestEntity($"https://example{i}.com", $"Example {i}"));
        }

        var recent = await _repository.GetRecentAsync(5);

        Assert.Equal(5, recent.Count());
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsInDescendingTimestampOrder()
    {
        var timestamps = new[]
        {
            DateTime.UtcNow.AddMinutes(-3),
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(-2)
        };

        for (int i = 0; i < 3; i++)
        {
            var entity = CreateTestEntity($"https://example{i}.com", $"Example {i}");
            entity.VisitedAt = timestamps[i];
            await _repository.AddAsync(entity);
        }

        var recent = (await _repository.GetRecentAsync(3)).ToList();

        // Should be ordered by timestamp descending (most recent first)
        Assert.True(recent[0].VisitedAt >= recent[1].VisitedAt);
        Assert.True(recent[1].VisitedAt >= recent[2].VisitedAt);
    }

    [Fact]
    public async Task SearchAsync_FindsByUrl()
    {
        await _repository.AddAsync(CreateTestEntity("https://google.com", "Google"));
        await _repository.AddAsync(CreateTestEntity("https://github.com", "GitHub"));
        await _repository.AddAsync(CreateTestEntity("https://example.com", "Example"));

        var results = await _repository.SearchAsync("github");

        Assert.Single(results);
        Assert.Equal("https://github.com", results.First().Url);
    }

    [Fact]
    public async Task SearchAsync_FindsByTitle()
    {
        await _repository.AddAsync(CreateTestEntity("https://google.com", "Google Search"));
        await _repository.AddAsync(CreateTestEntity("https://github.com", "GitHub - Code"));
        await _repository.AddAsync(CreateTestEntity("https://example.com", "Example Site"));

        var results = await _repository.SearchAsync("code");

        Assert.Single(results);
        Assert.Equal("GitHub - Code", results.First().Title);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        await _repository.AddAsync(CreateTestEntity("https://GOOGLE.com", "GOOGLE Search"));

        var results = await _repository.SearchAsync("google");

        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForEmptyQuery()
    {
        await _repository.AddAsync(CreateTestEntity("https://google.com", "Google"));

        var results = await _repository.SearchAsync("");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_ForWhitespaceQuery()
    {
        await _repository.AddAsync(CreateTestEntity("https://google.com", "Google"));

        var results = await _repository.SearchAsync("   ");

        Assert.Empty(results);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllRecords()
    {
        await _repository.AddAsync(CreateTestEntity("https://google.com", "Google"));
        await _repository.AddAsync(CreateTestEntity("https://github.com", "GitHub"));
        await _repository.AddAsync(CreateTestEntity("https://example.com", "Example"));

        var countBefore = await _context.BrowsingHistory.CountAsync();
        Assert.Equal(3, countBefore);

        await _repository.ClearAllAsync();

        var countAfter = await _context.BrowsingHistory.CountAsync();
        Assert.Equal(0, countAfter);
    }

    [Fact]
    public async Task ClearAllAsync_DoesNotThrow_WhenEmpty()
    {
        // Should not throw when clearing an empty database
        await _repository.ClearAllAsync();

        var count = await _context.BrowsingHistory.CountAsync();
        Assert.Equal(0, count);
    }

    private static BrowsingHistoryEntity CreateTestEntity(string url, string? title = null)
    {
        return new BrowsingHistoryEntity
        {
            Url = url,
            Title = title,
            VisitedAt = DateTime.UtcNow
        };
    }
}
