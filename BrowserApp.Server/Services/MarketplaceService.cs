using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;
using BrowserApp.Server.Interfaces;
using BrowserApp.Server.Mappers;

namespace BrowserApp.Server.Services;

/// <summary>
/// Service for marketplace operations.
/// </summary>
public class MarketplaceService : IMarketplaceService
{
    private readonly IMarketplaceRuleRepository _ruleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<MarketplaceService> _logger;

    public MarketplaceService(
        IMarketplaceRuleRepository ruleRepository,
        IUserRepository userRepository,
        ILogger<MarketplaceService> logger)
    {
        _ruleRepository = ruleRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<RuleListResponse> GetRulesAsync(int page, int pageSize)
    {
        var rules = await _ruleRepository.GetPagedAsync(page, pageSize);
        var totalCount = await _ruleRepository.GetTotalCountAsync();

        return MarketplaceRuleMapper.ToListDto(rules, totalCount, page, pageSize);
    }

    public async Task<RuleResponse?> GetRuleByIdAsync(Guid id)
    {
        var rule = await _ruleRepository.GetByIdAsync(id);
        return rule != null ? MarketplaceRuleMapper.ToDto(rule) : null;
    }

    public async Task<RuleResponse> UploadRuleAsync(RuleUploadRequest request)
    {
        _logger.LogInformation("Uploading rule '{Name}' by user '{Username}'",
            request.Name, request.AuthorUsername);

        // Get or create the user
        var user = await _userRepository.GetOrCreateAsync(request.AuthorUsername);

        // Create the rule entity
        var entity = MarketplaceRuleMapper.ToEntity(request, user.Id);

        // Save to database
        var savedEntity = await _ruleRepository.AddAsync(entity);

        // Reload with author data
        savedEntity = await _ruleRepository.GetByIdAsync(savedEntity.Id);

        _logger.LogInformation("Rule '{Name}' uploaded successfully with ID {Id}",
            request.Name, savedEntity!.Id);

        return MarketplaceRuleMapper.ToDto(savedEntity);
    }

    public async Task<RuleListResponse> SearchRulesAsync(string? query, string[]? tags, int page, int pageSize)
    {
        var rules = await _ruleRepository.SearchAsync(query, tags, page, pageSize);
        var totalCount = await _ruleRepository.GetSearchCountAsync(query, tags);

        return MarketplaceRuleMapper.ToListDto(rules, totalCount, page, pageSize);
    }

    public async Task<RuleResponse?> IncrementDownloadAsync(Guid id)
    {
        var rule = await _ruleRepository.IncrementDownloadCountAsync(id);
        return rule != null ? MarketplaceRuleMapper.ToDto(rule) : null;
    }

    public async Task<bool> DeleteRuleAsync(Guid id)
    {
        var rule = await _ruleRepository.GetByIdAsync(id);
        if (rule == null)
        {
            return false;
        }

        await _ruleRepository.DeleteAsync(id);
        _logger.LogInformation("Rule '{Name}' (ID: {Id}) deleted", rule.Name, id);
        return true;
    }
}
