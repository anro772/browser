using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository implementation for browsing history operations.
/// </summary>
public class BrowsingHistoryRepository : IBrowsingHistoryRepository
{
    private readonly BrowserDbContext _context;

    public BrowsingHistoryRepository(BrowserDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(BrowsingHistoryEntity history)
    {
        _context.BrowsingHistory.Add(history);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BrowsingHistoryEntity>> GetRecentAsync(int count)
    {
        return await _context.BrowsingHistory
            .OrderByDescending(h => h.VisitedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BrowsingHistoryEntity>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<BrowsingHistoryEntity>();
        }

        string lowerQuery = query.ToLowerInvariant();

        return await _context.BrowsingHistory
            .Where(h => h.Url.ToLower().Contains(lowerQuery) ||
                       (h.Title != null && h.Title.ToLower().Contains(lowerQuery)))
            .OrderByDescending(h => h.VisitedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync()
    {
        _context.BrowsingHistory.RemoveRange(_context.BrowsingHistory);
        await _context.SaveChangesAsync();
    }
}
