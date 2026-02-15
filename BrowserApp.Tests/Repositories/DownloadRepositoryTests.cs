using BrowserApp.Data;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BrowserApp.Tests.Repositories;

public class DownloadRepositoryTests : IDisposable
{
    private readonly BrowserDbContext _context;
    private readonly DownloadRepository _repository;

    public DownloadRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrowserDbContext(options);
        _repository = new DownloadRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllDownloads_OrderedByStartedAtDesc()
    {
        // Arrange
        var oldest = CreateTestDownload("old.zip", "C:\\Downloads\\old.zip");
        oldest.StartedAt = DateTime.UtcNow.AddHours(-3);

        var middle = CreateTestDownload("mid.zip", "C:\\Downloads\\mid.zip");
        middle.StartedAt = DateTime.UtcNow.AddHours(-1);

        var newest = CreateTestDownload("new.zip", "C:\\Downloads\\new.zip");
        newest.StartedAt = DateTime.UtcNow;

        _context.Downloads.AddRange(oldest, middle, newest);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].FileName.Should().Be("new.zip");
        result[1].FileName.Should().Be("mid.zip");
        result[2].FileName.Should().Be("old.zip");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoDownloadsExist()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByPathAsync

    [Fact]
    public async Task GetByPathAsync_ExistingPath_ReturnsDownload()
    {
        // Arrange
        var download = CreateTestDownload("report.pdf", "C:\\Downloads\\report.pdf");
        _context.Downloads.Add(download);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPathAsync("C:\\Downloads\\report.pdf");

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("report.pdf");
    }

    [Fact]
    public async Task GetByPathAsync_NonExistentPath_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByPathAsync("C:\\Downloads\\nonexistent.zip");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_AddsDownloadToDatabase()
    {
        // Arrange
        var download = CreateTestDownload("image.png", "C:\\Downloads\\image.png");

        // Act
        await _repository.AddAsync(download);

        // Assert
        var count = await _context.Downloads.CountAsync();
        count.Should().Be(1);

        var saved = await _context.Downloads.FirstAsync();
        saved.FileName.Should().Be("image.png");
        saved.DestinationPath.Should().Be("C:\\Downloads\\image.png");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingDownload_UpdatesFields()
    {
        // Arrange
        var download = CreateTestDownload("file.zip", "C:\\Downloads\\file.zip");
        _context.Downloads.Add(download);
        await _context.SaveChangesAsync();

        // Act
        download.Status = "completed";
        download.CompletedAt = DateTime.UtcNow;
        download.TotalBytes = 1024 * 1024;
        await _repository.UpdateAsync(download);

        // Assert
        var updated = await _context.Downloads.FindAsync(download.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be("completed");
        updated.CompletedAt.Should().NotBeNull();
        updated.TotalBytes.Should().Be(1024 * 1024);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesDownload()
    {
        // Arrange
        var download = CreateTestDownload("delete-me.zip", "C:\\Downloads\\delete-me.zip");
        _context.Downloads.Add(download);
        await _context.SaveChangesAsync();
        var savedId = download.Id;

        // Act
        await _repository.DeleteAsync(savedId);

        // Assert
        var remaining = await _context.Downloads.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNothing()
    {
        // Arrange
        var download = CreateTestDownload("survivor.zip", "C:\\Downloads\\survivor.zip");
        _context.Downloads.Add(download);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(99999);

        // Assert
        var count = await _context.Downloads.CountAsync();
        count.Should().Be(1);
    }

    #endregion

    #region ClearCompletedAsync

    [Fact]
    public async Task ClearCompletedAsync_RemovesOnlyCompletedDownloads()
    {
        // Arrange
        var completed1 = CreateTestDownload("done1.zip", "C:\\Downloads\\done1.zip", status: "completed");
        var completed2 = CreateTestDownload("done2.zip", "C:\\Downloads\\done2.zip", status: "completed");
        var downloading = CreateTestDownload("active.zip", "C:\\Downloads\\active.zip", status: "downloading");
        var failed = CreateTestDownload("failed.zip", "C:\\Downloads\\failed.zip", status: "failed");

        _context.Downloads.AddRange(completed1, completed2, downloading, failed);
        await _context.SaveChangesAsync();

        // Act
        await _repository.ClearCompletedAsync();

        // Assert
        var remaining = await _context.Downloads.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(d => d.Status == "completed");
        remaining.Select(d => d.FileName).Should().Contain("active.zip").And.Contain("failed.zip");
    }

    [Fact]
    public async Task ClearCompletedAsync_DoesNothing_WhenNoCompletedDownloads()
    {
        // Arrange
        var downloading = CreateTestDownload("active.zip", "C:\\Downloads\\active.zip", status: "downloading");
        _context.Downloads.Add(downloading);
        await _context.SaveChangesAsync();

        // Act
        await _repository.ClearCompletedAsync();

        // Assert
        var count = await _context.Downloads.CountAsync();
        count.Should().Be(1);
    }

    #endregion

    #region Helpers

    private static DownloadEntity CreateTestDownload(
        string fileName,
        string destinationPath,
        string sourceUrl = "https://example.com/file",
        long totalBytes = 1024,
        string status = "downloading")
    {
        return new DownloadEntity
        {
            FileName = fileName,
            SourceUrl = sourceUrl,
            DestinationPath = destinationPath,
            TotalBytes = totalBytes,
            Status = status,
            StartedAt = DateTime.UtcNow
        };
    }

    #endregion
}
