using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for bookmark operations.
/// </summary>
public interface IBookmarkRepository
{
    Task<IEnumerable<BookmarkEntity>> GetAllAsync();
    Task<BookmarkEntity?> GetByUrlAsync(string url);
    Task AddAsync(BookmarkEntity bookmark);
    Task RemoveAsync(int id);
    Task<bool> ExistsAsync(string url);
}
