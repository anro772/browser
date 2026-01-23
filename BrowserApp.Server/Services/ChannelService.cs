using BrowserApp.Server.Data.Entities;
using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;
using BrowserApp.Server.Interfaces;
using BrowserApp.Server.Mappers;
using BrowserApp.Server.Utilities;

namespace BrowserApp.Server.Services;

/// <summary>
/// Service for channel business logic.
/// </summary>
public class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository channelRepository,
        IUserRepository userRepository,
        ILogger<ChannelService> logger)
    {
        _channelRepository = channelRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    #region Channel CRUD

    public async Task<ChannelResponse> CreateChannelAsync(CreateChannelRequest request)
    {
        // Get or create owner
        var owner = await _userRepository.GetOrCreateAsync(request.OwnerUsername);

        // Hash password
        var passwordHash = PasswordHasher.HashPassword(request.Password);

        // Create channel and add owner as first member in a transaction
        var entity = ChannelMapper.ToEntity(request, owner.Id, passwordHash);
        var savedEntity = await _channelRepository.AddAsync(entity);

        // Add owner as first member
        var membership = new ChannelMemberEntity
        {
            ChannelId = savedEntity.Id,
            UserId = owner.Id,
            JoinedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        await _channelRepository.AddMemberAsync(membership);

        // Increment member count for the owner
        await _channelRepository.IncrementMemberCountAsync(savedEntity.Id);

        // Reload with owner navigation
        savedEntity = await _channelRepository.GetByIdAsync(savedEntity.Id);
        if (savedEntity == null)
        {
            throw new InvalidOperationException("Channel was created but could not be retrieved");
        }

        _logger.LogInformation("Channel '{Name}' created by {Username}", request.Name, request.OwnerUsername);
        return ChannelMapper.ToDto(savedEntity);
    }

    public async Task<ChannelResponse?> GetChannelByIdAsync(Guid id)
    {
        var entity = await _channelRepository.GetByIdWithRulesAsync(id);
        return entity == null ? null : ChannelMapper.ToDto(entity);
    }

    public async Task<ChannelListResponse> GetChannelsAsync(int page, int pageSize)
    {
        var channels = await _channelRepository.GetPagedAsync(page, pageSize);
        var totalCount = await _channelRepository.GetCountAsync();
        return ChannelMapper.ToListDto(channels, totalCount, page, pageSize);
    }

    public async Task<ChannelListResponse> SearchChannelsAsync(string? query, int page, int pageSize)
    {
        var channels = await _channelRepository.SearchAsync(query, page, pageSize);
        var totalCount = await _channelRepository.GetSearchCountAsync(query);
        return ChannelMapper.ToListDto(channels, totalCount, page, pageSize);
    }

    public async Task<bool> DeleteChannelAsync(Guid channelId, string username)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null) return false;

        // Verify ownership
        if (channel.Owner.Username != username)
        {
            _logger.LogWarning("User {Username} attempted to delete channel {ChannelId} without ownership",
                username, channelId);
            return false;
        }

        await _channelRepository.DeleteAsync(channelId);
        _logger.LogInformation("Channel {ChannelId} deleted by {Username}", channelId, username);
        return true;
    }

    #endregion

    #region Membership

    public async Task<(bool success, string message)> JoinChannelAsync(Guid channelId, JoinChannelRequest request)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
        {
            return (false, "Channel not found");
        }

        // Verify password
        if (!PasswordHasher.VerifyPassword(request.Password, channel.PasswordHash))
        {
            _logger.LogWarning("Invalid password attempt to join channel {ChannelId} by {Username}",
                channelId, request.Username);
            return (false, "Invalid password");
        }

        // Get or create user
        var user = await _userRepository.GetOrCreateAsync(request.Username);

        // Check if already a member
        var existingMember = await _channelRepository.GetMemberAsync(channelId, user.Id);
        if (existingMember != null)
        {
            return (true, "Already a member");
        }

        // Add membership
        var membership = new ChannelMemberEntity
        {
            ChannelId = channelId,
            UserId = user.Id,
            JoinedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
        await _channelRepository.AddMemberAsync(membership);
        await _channelRepository.IncrementMemberCountAsync(channelId);

        _logger.LogInformation("User {Username} joined channel {ChannelId}", request.Username, channelId);
        return (true, "Successfully joined channel");
    }

    public async Task<(bool success, string message)> LeaveChannelAsync(Guid channelId, string username)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
        {
            return (false, "Channel not found");
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Can't leave if you're the owner
        if (channel.OwnerId == user.Id)
        {
            return (false, "Channel owner cannot leave. Delete the channel instead.");
        }

        var member = await _channelRepository.GetMemberAsync(channelId, user.Id);
        if (member == null)
        {
            return (false, "Not a member of this channel");
        }

        await _channelRepository.RemoveMemberAsync(channelId, user.Id);
        await _channelRepository.DecrementMemberCountAsync(channelId);

        _logger.LogInformation("User {Username} left channel {ChannelId}", username, channelId);
        return (true, "Successfully left channel");
    }

    public async Task<ChannelListResponse> GetUserChannelsAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return new ChannelListResponse { Channels = new(), TotalCount = 0, Page = 1, PageSize = 100 };
        }

        var channels = await _channelRepository.GetUserChannelsAsync(user.Id);
        var channelList = channels.ToList();
        return ChannelMapper.ToListDto(channelList, channelList.Count, 1, 100);
    }

    #endregion

    #region Rules

    public async Task<ChannelRuleListResponse?> GetChannelRulesAsync(Guid channelId, string username)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
        {
            return null;
        }

        // Verify membership
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return null;
        }

        var member = await _channelRepository.GetMemberAsync(channelId, user.Id);
        if (member == null)
        {
            _logger.LogWarning("User {Username} attempted to get rules for channel {ChannelId} without membership",
                username, channelId);
            return null;
        }

        var rules = await _channelRepository.GetChannelRulesAsync(channelId);
        return ChannelMapper.ToRuleListDto(channel, rules);
    }

    public async Task<ChannelRuleResponse?> AddChannelRuleAsync(Guid channelId, AddChannelRuleRequest request)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
        {
            return null;
        }

        // Verify ownership
        if (channel.Owner.Username != request.Username)
        {
            _logger.LogWarning("User {Username} attempted to add rule to channel {ChannelId} without ownership",
                request.Username, channelId);
            return null;
        }

        var entity = ChannelMapper.ToRuleEntity(request, channelId);
        var savedEntity = await _channelRepository.AddChannelRuleAsync(entity);

        _logger.LogInformation("Rule '{RuleName}' added to channel {ChannelId} by {Username}",
            request.Name, channelId, request.Username);
        return ChannelMapper.ToRuleDto(savedEntity);
    }

    public async Task<bool> DeleteChannelRuleAsync(Guid channelId, Guid ruleId, string username)
    {
        var channel = await _channelRepository.GetByIdAsync(channelId);
        if (channel == null)
        {
            return false;
        }

        // Verify ownership
        if (channel.Owner.Username != username)
        {
            _logger.LogWarning("User {Username} attempted to delete rule from channel {ChannelId} without ownership",
                username, channelId);
            return false;
        }

        var rule = await _channelRepository.GetChannelRuleByIdAsync(ruleId);
        if (rule == null || rule.ChannelId != channelId)
        {
            return false;
        }

        await _channelRepository.DeleteChannelRuleAsync(ruleId);
        _logger.LogInformation("Rule {RuleId} deleted from channel {ChannelId} by {Username}",
            ruleId, channelId, username);
        return true;
    }

    #endregion

    #region Sync

    public async Task UpdateMemberSyncTimeAsync(Guid channelId, string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user != null)
        {
            await _channelRepository.UpdateMemberSyncTimeAsync(channelId, user.Id);
        }
    }

    #endregion
}
