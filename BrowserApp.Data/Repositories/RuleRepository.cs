using Microsoft.EntityFrameworkCore;
using BrowserApp.Data.Entities;
using BrowserApp.Data.Interfaces;

namespace BrowserApp.Data.Repositories;

/// <summary>
/// Repository for rule CRUD operations using Entity Framework Core.
/// </summary>
public class RuleRepository : IRuleRepository
{
    private readonly BrowserDbContext _context;

    public RuleRepository(BrowserDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RuleEntity>> GetAllAsync()
    {
        return await _context.Rules
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<RuleEntity>> GetEnabledAsync()
    {
        return await _context.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
    }

    public async Task<RuleEntity?> GetByIdAsync(string id)
    {
        return await _context.Rules.FindAsync(id);
    }

    public async Task AddAsync(RuleEntity rule)
    {
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RuleEntity rule)
    {
        var existing = await _context.Rules.FindAsync(rule.Id);
        if (existing == null)
            throw new InvalidOperationException($"Rule with ID {rule.Id} not found.");

        existing.Name = rule.Name;
        existing.Description = rule.Description;
        existing.Site = rule.Site;
        existing.Enabled = rule.Enabled;
        existing.Priority = rule.Priority;
        existing.RulesJson = rule.RulesJson;
        existing.Source = rule.Source;
        existing.ChannelId = rule.ChannelId;
        existing.IsEnforced = rule.IsEnforced;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule != null)
        {
            _context.Rules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Rules.CountAsync();
    }

    public async Task<IEnumerable<RuleEntity>> GetBySourceAsync(string source)
    {
        return await _context.Rules
            .Where(r => r.Source == source)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Rules.AnyAsync(r => r.Id == id);
    }

    public async Task DeleteByChannelIdAsync(string channelId)
    {
        var rules = await _context.Rules
            .Where(r => r.ChannelId == channelId)
            .ToListAsync();

        if (rules.Any())
        {
            _context.Rules.RemoveRange(rules);
            await _context.SaveChangesAsync();
        }
    }
}
