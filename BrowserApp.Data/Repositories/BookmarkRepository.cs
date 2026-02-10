using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository implementation for bookmark operations.
/// </summary>
public class BookmarkRepository : IBookmarkRepository
{
    private readonly BrowserDbContext _context;

    public BookmarkRepository(BrowserDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BookmarkEntity>> GetAllAsync()
    {
        return await _context.Bookmarks
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<BookmarkEntity?> GetByUrlAsync(string url)
    {
        return await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.Url == url);
    }

    public async Task AddAsync(BookmarkEntity bookmark)
    {
        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(int id)
    {
        var bookmark = await _context.Bookmarks.FindAsync(id);
        if (bookmark != null)
        {
            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string url)
    {
        return await _context.Bookmarks.AnyAsync(b => b.Url == url);
    }
}
