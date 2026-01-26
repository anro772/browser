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

    [Fact]
    public async Task GetTopBlockedDomainsAsync_ReturnsCorrectDomainsAndCounts()
    {
        // Add blocked requests from different domains
        await _repository.AddAsync(CreateTestEntity("https://tracker.com/script1.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://tracker.com/script2.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://tracker.com/script3.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://ads.com/ad1.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://ads.com/ad2.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://analytics.com/track.js", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://example.com/page.html", wasBlocked: false)); // Not blocked

        var topDomains = await _repository.GetTopBlockedDomainsAsync(3);

        Assert.Equal(3, topDomains.Count);
        Assert.Equal("tracker.com", topDomains[0].Domain);
        Assert.Equal(3, topDomains[0].Count);
        Assert.Equal("ads.com", topDomains[1].Domain);
        Assert.Equal(2, topDomains[1].Count);
        Assert.Equal("analytics.com", topDomains[2].Domain);
        Assert.Equal(1, topDomains[2].Count);
    }

    [Fact]
    public async Task GetTopBlockedDomainsAsync_ReturnsEmptyList_WhenNoBlockedRequests()
    {
        await _repository.AddAsync(CreateTestEntity("https://example.com/page.html", wasBlocked: false));

        var topDomains = await _repository.GetTopBlockedDomainsAsync(5);

        Assert.Empty(topDomains);
    }

    [Fact]
    public async Task GetResourceTypeBreakdownAsync_ReturnsCorrectBreakdown()
    {
        await _repository.AddAsync(CreateTestEntity("https://a.com/1.js", resourceType: "Script"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/2.js", resourceType: "Script"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/3.js", resourceType: "Script"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/1.png", resourceType: "Image"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/2.png", resourceType: "Image"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/page.html", resourceType: "Document"));

        var breakdown = await _repository.GetResourceTypeBreakdownAsync();

        Assert.Equal(3, breakdown.Count);
        Assert.Equal("Script", breakdown[0].Type);
        Assert.Equal(3, breakdown[0].Count);
        Assert.Equal("Image", breakdown[1].Type);
        Assert.Equal(2, breakdown[1].Count);
        Assert.Equal("Document", breakdown[2].Type);
        Assert.Equal(1, breakdown[2].Count);
    }

    [Fact]
    public async Task GetResourceTypeBreakdownAsync_GroupsAllResourceTypes()
    {
        // All entries have resource types since it's a required field
        await _repository.AddAsync(CreateTestEntity("https://a.com/1.js", resourceType: "Script"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/2.js", resourceType: "Script"));
        await _repository.AddAsync(CreateTestEntity("https://a.com/page.html", resourceType: "Document"));

        var breakdown = await _repository.GetResourceTypeBreakdownAsync();

        Assert.Equal(2, breakdown.Count);
        // Ordered by count descending
        Assert.Equal("Script", breakdown[0].Type);
        Assert.Equal(2, breakdown[0].Count);
        Assert.Equal("Document", breakdown[1].Type);
        Assert.Equal(1, breakdown[1].Count);
    }

    [Fact]
    public async Task GetBlockedTodayCountAsync_ReturnsOnlyTodaysBlocked()
    {
        // Add requests from today
        await _repository.AddAsync(CreateTestEntity("https://today1.com", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://today2.com", wasBlocked: true));
        await _repository.AddAsync(CreateTestEntity("https://today3.com", wasBlocked: false)); // Not blocked

        // Add a request from yesterday
        var yesterday = CreateTestEntity("https://yesterday.com", wasBlocked: true);
        yesterday.Timestamp = DateTime.Today.AddDays(-1);
        await _repository.AddAsync(yesterday);

        var blockedToday = await _repository.GetBlockedTodayCountAsync();

        Assert.Equal(2, blockedToday);
    }

    [Fact]
    public async Task GetBlockedTodayCountAsync_ReturnsZero_WhenNoBlockedToday()
    {
        // Add only non-blocked requests
        await _repository.AddAsync(CreateTestEntity("https://example.com", wasBlocked: false));

        var blockedToday = await _repository.GetBlockedTodayCountAsync();

        Assert.Equal(0, blockedToday);
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
