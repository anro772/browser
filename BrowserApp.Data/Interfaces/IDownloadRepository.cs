using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for download operations.
/// </summary>
public interface IDownloadRepository
{
    Task<IEnumerable<DownloadEntity>> GetAllAsync();
    Task<DownloadEntity?> GetByPathAsync(string destinationPath);
    Task AddAsync(DownloadEntity download);
    Task UpdateAsync(DownloadEntity download);
    Task DeleteAsync(int id);
    Task ClearCompletedAsync();
}
