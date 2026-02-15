using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BrowserApp.Tests.Repositories;

public class BookmarkRepositoryTests : IDisposable
{
    private readonly BrowserDbContext _context;
    private readonly BookmarkRepository _repository;

    public BookmarkRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrowserDbContext(options);
        _repository = new BookmarkRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllBookmarks_OrderedByCreatedAtDesc()
    {
        // Arrange
        var oldest = CreateTestBookmark("https://oldest.com", "Oldest");
        oldest.CreatedAt = DateTime.UtcNow.AddDays(-3);

        var middle = CreateTestBookmark("https://middle.com", "Middle");
        middle.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var newest = CreateTestBookmark("https://newest.com", "Newest");
        newest.CreatedAt = DateTime.UtcNow;

        _context.Bookmarks.AddRange(oldest, middle, newest);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Url.Should().Be("https://newest.com");
        result[1].Url.Should().Be("https://middle.com");
        result[2].Url.Should().Be("https://oldest.com");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoBookmarksExist()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByUrlAsync

    [Fact]
    public async Task GetByUrlAsync_ExistingUrl_ReturnsBookmark()
    {
        // Arrange
        var bookmark = CreateTestBookmark("https://example.com", "Example");
        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUrlAsync("https://example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Example");
    }

    [Fact]
    public async Task GetByUrlAsync_NonExistentUrl_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUrlAsync("https://nonexistent.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_AddsBookmarkToDatabase()
    {
        // Arrange
        var bookmark = CreateTestBookmark("https://new-bookmark.com", "New Bookmark");

        // Act
        await _repository.AddAsync(bookmark);

        // Assert
        var count = await _context.Bookmarks.CountAsync();
        count.Should().Be(1);

        var saved = await _context.Bookmarks.FirstAsync();
        saved.Title.Should().Be("New Bookmark");
        saved.Url.Should().Be("https://new-bookmark.com");
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ExistingId_RemovesBookmark()
    {
        // Arrange
        var bookmark = CreateTestBookmark("https://to-remove.com", "Remove Me");
        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();
        var savedId = bookmark.Id;

        // Act
        await _repository.RemoveAsync(savedId);

        // Assert
        var remaining = await _context.Bookmarks.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentId_DoesNothing()
    {
        // Arrange
        var bookmark = CreateTestBookmark("https://survivor.com", "Survivor");
        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        // Act
        await _repository.RemoveAsync(99999);

        // Assert
        var count = await _context.Bookmarks.CountAsync();
        count.Should().Be(1);
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_ExistingUrl_ReturnsTrue()
    {
        // Arrange
        var bookmark = CreateTestBookmark("https://exists.com", "Exists");
        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync("https://exists.com");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentUrl_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync("https://doesnotexist.com");

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private static BookmarkEntity CreateTestBookmark(string url, string title, string? faviconUrl = null)
    {
        return new BookmarkEntity
        {
            Title = title,
            Url = url,
            FaviconUrl = faviconUrl,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
