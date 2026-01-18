using Microsoft.EntityFrameworkCore;
using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.Interfaces;

namespace BrowserApp.Server.Data.Repositories;

/// <summary>
/// Repository for marketplace rule operations using Entity Framework Core.
/// </summary>
public class MarketplaceRuleRepository : IMarketplaceRuleRepository
{
    private readonly ServerDbContext _context;

    public MarketplaceRuleRepository(ServerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MarketplaceRuleEntity>> GetPagedAsync(int page, int pageSize)
    {
        return await _context.MarketplaceRules
            .Include(r => r.Author)
            .OrderByDescending(r => r.DownloadCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<MarketplaceRuleEntity?> GetByIdAsync(Guid id)
    {
        return await _context.MarketplaceRules
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<MarketplaceRuleEntity> AddAsync(MarketplaceRuleEntity rule)
    {
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        _context.MarketplaceRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<MarketplaceRuleEntity> UpdateAsync(MarketplaceRuleEntity rule)
    {
        var existing = await _context.MarketplaceRules.FindAsync(rule.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Rule with ID {rule.Id} not found.");
        }

        existing.Name = rule.Name;
        existing.Description = rule.Description;
        existing.Site = rule.Site;
        existing.Priority = rule.Priority;
        existing.RulesJson = rule.RulesJson;
        existing.Tags = rule.Tags;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _context.MarketplaceRules.FindAsync(id);
        if (rule != null)
        {
            _context.MarketplaceRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<MarketplaceRuleEntity>> SearchAsync(string? query, string[]? tags, int page, int pageSize)
    {
        var queryable = _context.MarketplaceRules
            .Include(r => r.Author)
            .AsQueryable();

        // Filter by search query (name or description)
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            queryable = queryable.Where(r =>
                r.Name.ToLower().Contains(lowerQuery) ||
                r.Description.ToLower().Contains(lowerQuery));
        }

        // Filter by tags (any match)
        if (tags != null && tags.Length > 0)
        {
            queryable = queryable.Where(r => r.Tags.Any(t => tags.Contains(t)));
        }

        return await queryable
            .OrderByDescending(r => r.DownloadCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.MarketplaceRules.CountAsync();
    }

    public async Task<int> GetSearchCountAsync(string? query, string[]? tags)
    {
        var queryable = _context.MarketplaceRules.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLower();
            queryable = queryable.Where(r =>
                r.Name.ToLower().Contains(lowerQuery) ||
                r.Description.ToLower().Contains(lowerQuery));
        }

        if (tags != null && tags.Length > 0)
        {
            queryable = queryable.Where(r => r.Tags.Any(t => tags.Contains(t)));
        }

        return await queryable.CountAsync();
    }

    public async Task<MarketplaceRuleEntity?> IncrementDownloadCountAsync(Guid id)
    {
        var rule = await _context.MarketplaceRules
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule != null)
        {
            rule.DownloadCount++;
            await _context.SaveChangesAsync();
        }

        return rule;
    }
}
