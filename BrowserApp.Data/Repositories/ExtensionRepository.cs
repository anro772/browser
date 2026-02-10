using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository implementation for extension operations.
/// </summary>
public class ExtensionRepository : IExtensionRepository
{
    private readonly BrowserDbContext _context;

    public ExtensionRepository(BrowserDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExtensionEntity>> GetAllAsync()
    {
        return await _context.Extensions
            .OrderByDescending(e => e.InstalledAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExtensionEntity>> GetEnabledAsync()
    {
        return await _context.Extensions
            .Where(e => e.IsEnabled)
            .OrderByDescending(e => e.InstalledAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task AddAsync(ExtensionEntity extension)
    {
        _context.Extensions.Add(extension);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ExtensionEntity extension)
    {
        _context.Extensions.Update(extension);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Extensions.FindAsync(id);
        if (entity != null)
        {
            _context.Extensions.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
