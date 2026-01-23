using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.Interfaces;

namespace BrowserApp.Server.Data.Repositories;

/// <summary>
/// Repository for channel operations using Entity Framework Core.
/// </summary>
public class ChannelRepository : IChannelRepository
{
    private readonly ServerDbContext _context;

    public ChannelRepository(ServerDbContext context)
    {
        _context = context;
    }

    #region Channel CRUD

    public async Task<ChannelEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Channels
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<ChannelEntity?> GetByIdWithRulesAsync(Guid id)
    {
        return await _context.Channels
            .Include(c => c.Owner)
            .Include(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<IEnumerable<ChannelEntity>> GetPagedAsync(int page, int pageSize)
    {
        return await _context.Channels
            .Include(c => c.Owner)
            .Where(c => c.IsActive && c.IsPublic)
            .OrderByDescending(c => c.MemberCount)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Channels
            .Where(c => c.IsActive && c.IsPublic)
            .CountAsync();
    }

    public async Task<IEnumerable<ChannelEntity>> SearchAsync(string? query, int page, int pageSize)
    {
        var queryable = BuildSearchQuery(query);
        return await queryable
            .OrderByDescending(c => c.MemberCount)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetSearchCountAsync(string? query)
    {
        var queryable = BuildSearchQuery(query);
        return await queryable.CountAsync();
    }

    private IQueryable<ChannelEntity> BuildSearchQuery(string? query)
    {
        var queryable = _context.Channels
            .Include(c => c.Owner)
            .Where(c => c.IsActive && c.IsPublic);

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Escape SQL wildcards to prevent wildcard injection
            var escapedQuery = EscapeSqlWildcards(query);
            queryable = queryable.Where(c =>
                EF.Functions.ILike(c.Name, $"%{escapedQuery}%") ||
                EF.Functions.ILike(c.Description, $"%{escapedQuery}%"));
        }

        return queryable;
    }

    /// <summary>
    /// Escapes SQL wildcard characters in a search query.
    /// </summary>
    private static string EscapeSqlWildcards(string query)
    {
        return query
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    public async Task<ChannelEntity> AddAsync(ChannelEntity entity)
    {
        _context.Channels.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(ChannelEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Channels.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Channels.FindAsync(id);
        if (entity != null)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Channel Rules

    public async Task<IEnumerable<ChannelRuleEntity>> GetChannelRulesAsync(Guid channelId)
    {
        return await _context.ChannelRules
            .Where(r => r.ChannelId == channelId)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChannelRuleEntity?> GetChannelRuleByIdAsync(Guid ruleId)
    {
        return await _context.ChannelRules.FindAsync(ruleId);
    }

    public async Task<ChannelRuleEntity> AddChannelRuleAsync(ChannelRuleEntity rule)
    {
        _context.ChannelRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task UpdateChannelRuleAsync(ChannelRuleEntity rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.ChannelRules.Update(rule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteChannelRuleAsync(Guid ruleId)
    {
        var rule = await _context.ChannelRules.FindAsync(ruleId);
        if (rule != null)
        {
            _context.ChannelRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Membership

    public async Task<ChannelMemberEntity?> GetMemberAsync(Guid channelId, Guid userId)
    {
        return await _context.ChannelMembers
            .Include(m => m.Channel)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId);
    }

    public async Task<IEnumerable<ChannelMemberEntity>> GetMembersAsync(Guid channelId)
    {
        return await _context.ChannelMembers
            .Include(m => m.User)
            .Where(m => m.ChannelId == channelId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChannelEntity>> GetUserChannelsAsync(Guid userId)
    {
        return await _context.ChannelMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.Channel)
            .Include(c => c.Owner)
            .Where(c => c.IsActive)
            .ToListAsync();
    }

    public async Task<ChannelMemberEntity> AddMemberAsync(ChannelMemberEntity member)
    {
        _context.ChannelMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task RemoveMemberAsync(Guid channelId, Guid userId)
    {
        var member = await _context.ChannelMembers
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId);
        if (member != null)
        {
            _context.ChannelMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateMemberSyncTimeAsync(Guid channelId, Guid userId)
    {
        var member = await _context.ChannelMembers
            .FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId);
        if (member != null)
        {
            member.LastSyncedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Counters

    public async Task IncrementMemberCountAsync(Guid channelId)
    {
        // Use raw SQL for atomic update to prevent race conditions
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Channels""
               SET ""MemberCount"" = ""MemberCount"" + 1,
                   ""UpdatedAt"" = {DateTime.UtcNow}
               WHERE ""Id"" = {channelId}");
    }

    public async Task DecrementMemberCountAsync(Guid channelId)
    {
        // Use raw SQL for atomic update to prevent race conditions
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Channels""
               SET ""MemberCount"" = GREATEST(""MemberCount"" - 1, 0),
                   ""UpdatedAt"" = {DateTime.UtcNow}
               WHERE ""Id"" = {channelId}");
    }

    #endregion
}
