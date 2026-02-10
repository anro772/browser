using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository implementation for download operations.
/// </summary>
public class DownloadRepository : IDownloadRepository
{
    private readonly BrowserDbContext _context;

    public DownloadRepository(BrowserDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DownloadEntity>> GetAllAsync()
    {
        return await _context.Downloads
            .OrderByDescending(d => d.StartedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<DownloadEntity?> GetByPathAsync(string destinationPath)
    {
        return await _context.Downloads
            .FirstOrDefaultAsync(d => d.DestinationPath == destinationPath);
    }

    /// <inheritdoc/>
    public async Task AddAsync(DownloadEntity download)
    {
        _context.Downloads.Add(download);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(DownloadEntity download)
    {
        _context.Downloads.Update(download);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Downloads.FindAsync(id);
        if (entity != null)
        {
            _context.Downloads.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task ClearCompletedAsync()
    {
        var completed = await _context.Downloads
            .Where(d => d.Status == "completed")
            .ToListAsync();

        _context.Downloads.RemoveRange(completed);
        await _context.SaveChangesAsync();
    }
}
