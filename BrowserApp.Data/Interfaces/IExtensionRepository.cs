using BrowserApp.Data.Entities;

namespace BrowserApp.Data.Interfaces;

/// <summary>
/// Repository interface for extension operations.
/// </summary>
public interface IExtensionRepository
{
    Task<IEnumerable<ExtensionEntity>> GetAllAsync();
    Task<IEnumerable<ExtensionEntity>> GetEnabledAsync();
    Task AddAsync(ExtensionEntity extension);
    Task UpdateAsync(ExtensionEntity extension);
    Task DeleteAsync(int id);
}
