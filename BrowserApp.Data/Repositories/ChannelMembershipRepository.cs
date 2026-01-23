using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository for channel membership operations using Entity Framework Core.
/// </summary>
public class ChannelMembershipRepository : IChannelMembershipRepository
{
    private readonly BrowserDbContext _context;

    public ChannelMembershipRepository(BrowserDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChannelMembershipEntity>> GetActiveAsync()
    {
        return await _context.ChannelMemberships
            .Where(m => m.IsActive)
            .OrderBy(m => m.ChannelName)
            .ToListAsync();
    }

    public async Task<ChannelMembershipEntity?> GetByIdAsync(string id)
    {
        return await _context.ChannelMemberships.FindAsync(id);
    }

    public async Task<ChannelMembershipEntity?> GetByChannelIdAsync(string channelId)
    {
        return await _context.ChannelMemberships
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.IsActive);
    }

    public async Task AddAsync(ChannelMembershipEntity entity)
    {
        _context.ChannelMemberships.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChannelMembershipEntity entity)
    {
        _context.ChannelMemberships.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.ChannelMemberships.FindAsync(id);
        if (entity != null)
        {
            _context.ChannelMemberships.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByChannelIdAsync(string channelId)
    {
        var entity = await _context.ChannelMemberships
            .FirstOrDefaultAsync(m => m.ChannelId == channelId);
        if (entity != null)
        {
            _context.ChannelMemberships.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastSyncedAsync(string channelId, int ruleCount)
    {
        var entity = await _context.ChannelMemberships
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.IsActive);
        if (entity != null)
        {
            entity.LastSyncedAt = DateTime.UtcNow;
            entity.RuleCount = ruleCount;
            await _context.SaveChangesAsync();
        }
    }
}
