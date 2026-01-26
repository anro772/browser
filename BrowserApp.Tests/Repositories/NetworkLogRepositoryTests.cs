using BrowserApp.Core.Models;
using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BrowserApp.Tests.Repositories;

public class NetworkLogRepositoryTests : IDisposable
{
    private readonly BrowserDbContext _context;
    private readonly NetworkLogRepository _repository;

    public NetworkLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrowserDbContext(options);
        _repository = new NetworkLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_AddsEntityToDatabase()
    {
        var entity = CreateTestEntity("https://example.com");

        await _repository.AddAsync(entity);

        var count = await _context.NetworkLogs.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddBatchAsync_AddsMultipleEntities()
    {
        var entities = new[]
        {
            CreateTestEntity("https://example1.com"),
            CreateTestEntity("https://example2.com"),
            CreateTestEntity("https://example3.com")
        };

        await _repository.AddBatchAsync(entities);

        var count = await _context.NetworkLogs.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsRequestedCount()
    {
        for (int i = 0; i < 10; i++)
        {
            await _repository.AddAsync(CreateTestEntity($"https://example{i}.com"));
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
            var entity = CreateTestEntity($"https://example{i}.com");
            entity.Timestamp = timestamps[i];
            await _repository.AddAsync(entity);
        }

        var recent = (await _repository.GetRecentAsync(3)).ToList();

        // Should be ordered by timestamp descending (most recent first)
        Assert.True(recent[0].Timestamp >= recent[1].Timestamp);
        Assert.True(recent[1].Timestamp >= recent[2].Timestamp);
    }

    [Fact]
    public async Task GetByFilterAsync_All_ReturnsAllRequests()
    {
        await SeedTestData();

        var results = await _repository.GetByFilterAsync(NetworkRequestFilter.All);

        Assert.Equal(5, results.Count());
    }

    [Fact]
    public async Task GetByFilterAsync_Blocked_ReturnsOnlyBlockedRequests()
    {
        await SeedTestData();

        var results = await _repository.GetByFilterAsync(NetworkRequestFilter.Blocked);

        Assert.All(results, r => Assert.True(r.WasBlocked));
    }

    [Fact]
    public async Task GetByFilterAsync_Scripts_ReturnsOnlyScriptRequests()
    {
        await SeedTestData();

        var results = await _repository.GetByFilterAsync(NetworkRequestFilter.Scripts);

        Assert.All(results, r => Assert.Equal("Script", r.ResourceType));
    }

    [Fact]
    public async Task GetByFilterAsync_Images_ReturnsOnlyImageRequests()
    {
        await SeedTestData();

        var results = await _repository.GetByFilterAsync(NetworkRequestFilter.Images);

        Assert.All(results, r => Assert.Equal("Image", r.ResourceType));
    }

    [Fact]
    public async Task GetByFilterAsync_ThirdParty_ReturnsThirdPartyRequests()
    {
        await SeedTestData();

        var results = await _repository.GetByFilterAsync(NetworkRequestFilter.ThirdParty, "example.com");
        var resultsList = results.ToList();

        // Should include tracker.com but not example.com or cdn.example.com
        Assert.Contains(resultsList, r => r.Url.Contains("tracker.com"));
        Assert.DoesNotContain(resultsList, r => r.Url.Contains("example.com") && !r.Url.Contains("tracker"));
    }

    [Fact]
    public async Task GetCountAsync_ReturnsCorrectCount()
    {
        await SeedTestData();

        var count = await _repository.GetCountAsync();

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task GetBlockedCountAsync_ReturnsCorrectCount()
    {
        await SeedTestData();

        var count = await _repository.GetBlockedCountAsync();

        Assert.Equal(1, count); // Only one blocked request in test data
    }

    [Fact]
    public async Task GetTotalSizeAsync_ReturnsSumOfBlockedSizes()
    {
        // GetTotalSizeAsync sums only blocked requests (represents data savings)
        await _repository.AddAsync(CreateTestEntity("https://a.com", size: 100, wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://b.com", size: 200, wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://c.com", size: null, wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://d.com", size: 500, wasBlocked: false)); // Not counted

        var totalSize = await _repository.GetTotalSizeAsync();

        Assert.Equal(300, totalSize);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllRecords()
    {
        await SeedTestData();
        Assert.Equal(5, await _repository.GetCountAsync());

        await _repository.ClearAllAsync();

        Assert.Equal(0, await _repository.GetCountAsync());
    }

    private async Task SeedTestData()
    {
        var entities = new[]
        {
            CreateTestEntity("https://example.com/page.html", resourceType: "Document"),
            CreateTestEntity("https://example.com/script.js", resourceType: "Script"),
            CreateTestEntity("https://cdn.example.com/image.png", resourceType: "Image"),
            CreateTestEntity("https://tracker.com/track.js", resourceType: "Script", wasBlocked: true),
            CreateTestEntity("https://example.com/style.css", resourceType: "Stylesheet")
        };

        await _repository.AddBatchAsync(entities);
    }

    private static NetworkLogEntity CreateTestEntity(
        string url,
        string method = "GET",
        int? statusCode = 200,
        string resourceType = "Document",
        string? contentType = "text/html",
        long? size = 1024,
        bool wasBlocked = false,
        string? blockedByRuleId = null)
    {
        return new NetworkLogEntity
        {
            Url = url,
            Method = method,
            StatusCode = statusCode,
            ResourceType = resourceType,
            ContentType = contentType,
            Size = size,
            WasBlocked = wasBlocked,
            BlockedByRuleId = blockedByRuleId,
            Timestamp = DateTime.UtcNow
        };
    }
}
