using Microsoft.AspNetCore.Mvc;
using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;
using BrowserApp.Server.Interfaces;

namespace BrowserApp.Server.Controllers;

/// <summary>
/// API controller for channel operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChannelController : ControllerBase
{
    private readonly IChannelService _channelService;
    private readonly ILogger<ChannelController> _logger;

    public ChannelController(
        IChannelService channelService,
        ILogger<ChannelController> logger)
    {
        _channelService = channelService;
        _logger = logger;
    }

    /// <summary>
    /// Validates and normalizes pagination parameters.
    /// </summary>
    private static (int page, int pageSize) ValidatePagination(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        return (page, pageSize);
    }

    /// <summary>
    /// Gets a paginated list of public channels.
    /// </summary>
    [HttpGet("channels")]
    [ProducesResponseType(typeof(ChannelListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChannelListResponse>> GetChannels(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        (page, pageSize) = ValidatePagination(page, pageSize);
        var result = await _channelService.GetChannelsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific channel by ID.
    /// </summary>
    [HttpGet("channels/{id:guid}")]
    [ProducesResponseType(typeof(ChannelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelResponse>> GetChannelById(Guid id)
    {
        var channel = await _channelService.GetChannelByIdAsync(id);
        if (channel == null)
        {
            return NotFound(new ErrorResponse($"Channel with ID {id} not found"));
        }
        return Ok(channel);
    }

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    [HttpPost("channels")]
    [ProducesResponseType(typeof(ChannelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChannelResponse>> CreateChannel([FromBody] CreateChannelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _channelService.CreateChannelAsync(request);
            return CreatedAtAction(
                nameof(GetChannelById),
                new { id = result.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create channel '{Name}'", request.Name);
            return BadRequest(new ErrorResponse("Failed to create channel", ex.Message));
        }
    }

    /// <summary>
    /// Searches public channels.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ChannelListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChannelListResponse>> SearchChannels(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        (page, pageSize) = ValidatePagination(page, pageSize);
        var result = await _channelService.SearchChannelsAsync(q, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Joins a channel with password.
    /// </summary>
    [HttpPost("channels/{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinChannel(Guid id, [FromBody] JoinChannelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message) = await _channelService.JoinChannelAsync(id, request);
        if (!success)
        {
            if (message == "Channel not found")
            {
                return NotFound(new ErrorResponse(message));
            }
            if (message == "Invalid password")
            {
                return Unauthorized(new ErrorResponse(message));
            }
            return BadRequest(new ErrorResponse(message));
        }

        return Ok(new { message });
    }

    /// <summary>
    /// Leaves a channel.
    /// </summary>
    [HttpPost("channels/{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveChannel(Guid id, [FromBody] LeaveChannelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (success, message) = await _channelService.LeaveChannelAsync(id, request.Username);
        if (!success)
        {
            if (message == "Channel not found" || message == "User not found")
            {
                return NotFound(new ErrorResponse(message));
            }
            return BadRequest(new ErrorResponse(message));
        }

        return NoContent();
    }

    /// <summary>
    /// Gets channels the user has joined.
    /// </summary>
    [HttpGet("user/{username}/channels")]
    [ProducesResponseType(typeof(ChannelListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChannelListResponse>> GetUserChannels(string username)
    {
        var result = await _channelService.GetUserChannelsAsync(username);
        return Ok(result);
    }

    /// <summary>
    /// Gets rules for a channel (requires membership).
    /// </summary>
    [HttpGet("channels/{id:guid}/rules")]
    [ProducesResponseType(typeof(ChannelRuleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelRuleListResponse>> GetChannelRules(
        Guid id,
        [FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new ErrorResponse("Username is required"));
        }

        var result = await _channelService.GetChannelRulesAsync(id, username);
        if (result == null)
        {
            return Forbid();
        }

        // Update sync time
        await _channelService.UpdateMemberSyncTimeAsync(id, username);
        return Ok(result);
    }

    /// <summary>
    /// Adds a rule to a channel (requires ownership).
    /// </summary>
    [HttpPost("channels/{id:guid}/rules")]
    [ProducesResponseType(typeof(ChannelRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChannelRuleResponse>> AddChannelRule(
        Guid id,
        [FromBody] AddChannelRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _channelService.AddChannelRuleAsync(id, request);
        if (result == null)
        {
            return Forbid();
        }

        return CreatedAtAction(
            nameof(GetChannelRules),
            new { id, username = request.Username },
            result);
    }

    /// <summary>
    /// Removes a rule from a channel (requires ownership).
    /// </summary>
    [HttpDelete("channels/{channelId:guid}/rules/{ruleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChannelRule(
        Guid channelId,
        Guid ruleId,
        [FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new ErrorResponse("Username is required"));
        }

        var success = await _channelService.DeleteChannelRuleAsync(channelId, ruleId, username);
        if (!success)
        {
            return NotFound(new ErrorResponse("Rule not found or access denied"));
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a channel (requires ownership).
    /// </summary>
    [HttpDelete("channels/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChannel(Guid id, [FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new ErrorResponse("Username is required"));
        }

        var success = await _channelService.DeleteChannelAsync(id, username);
        if (!success)
        {
            return NotFound(new ErrorResponse("Channel not found or access denied"));
        }

        return NoContent();
    }
}
