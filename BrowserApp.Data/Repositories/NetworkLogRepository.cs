using Microsoft.EntityFrameworkCore;
using BrowserApp.Core.Models;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository implementation for network log operations.
/// Optimized for high-volume logging with batch inserts.
/// </summary>
public class NetworkLogRepository : INetworkLogRepository
{
    private readonly BrowserDbContext _context;

    public NetworkLogRepository(BrowserDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(NetworkLogEntity log)
    {
        _context.NetworkLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task AddBatchAsync(IEnumerable<NetworkLogEntity> logs)
    {
        _context.NetworkLogs.AddRange(logs);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NetworkLogEntity>> GetRecentAsync(int count)
    {
        return await _context.NetworkLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<NetworkLogEntity>> GetByFilterAsync(
        NetworkRequestFilter filter,
        string? currentPageHost = null,
        int count = 1000)
    {
        IQueryable<NetworkLogEntity> query = _context.NetworkLogs;

        query = filter switch
        {
            NetworkRequestFilter.Blocked => query.Where(l => l.WasBlocked),
            NetworkRequestFilter.ThirdParty when currentPageHost != null =>
                query.Where(l => !l.Url.Contains(currentPageHost)),
            NetworkRequestFilter.Scripts => query.Where(l =>
                l.ResourceType == "Script" ||
                (l.ContentType != null && l.ContentType.Contains("javascript"))),
            NetworkRequestFilter.Images => query.Where(l =>
                l.ResourceType == "Image" ||
                (l.ContentType != null && l.ContentType.StartsWith("image/"))),
            _ => query
        };

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetCountAsync()
    {
        return await _context.NetworkLogs.CountAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetBlockedCountAsync()
    {
        return await _context.NetworkLogs.CountAsync(l => l.WasBlocked);
    }

    /// <inheritdoc/>
    public async Task<long> GetTotalSizeAsync()
    {
        // Sum only blocked requests - this represents actual data savings
        return await _context.NetworkLogs
            .Where(l => l.WasBlocked && l.Size.HasValue)
            .SumAsync(l => l.Size!.Value);
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync()
    {
        _context.NetworkLogs.RemoveRange(_context.NetworkLogs);
        await _context.SaveChangesAsync();
    }
}
