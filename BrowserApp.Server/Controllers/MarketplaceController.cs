using Microsoft.AspNetCore.Mvc;
using BrowserApp.Server.DTOs.Requests;
using BrowserApp.Server.DTOs.Responses;
using BrowserApp.Server.Interfaces;

namespace BrowserApp.Server.Controllers;

/// <summary>
/// API controller for marketplace operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly ILogger<MarketplaceController> _logger;

    public MarketplaceController(
        IMarketplaceService marketplaceService,
        ILogger<MarketplaceController> logger)
    {
        _marketplaceService = marketplaceService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of all marketplace rules.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(RuleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleListResponse>> GetRules(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _marketplaceService.GetRulesAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific rule by its ID.
    /// </summary>
    /// <param name="id">Rule ID.</param>
    [HttpGet("rules/{id:guid}")]
    [ProducesResponseType(typeof(RuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RuleResponse>> GetRuleById(Guid id)
    {
        var rule = await _marketplaceService.GetRuleByIdAsync(id);
        if (rule == null)
        {
            return NotFound(new { error = $"Rule with ID {id} not found" });
        }

        return Ok(rule);
    }

    /// <summary>
    /// Uploads a new rule to the marketplace.
    /// </summary>
    /// <param name="request">Rule upload request.</param>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(RuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RuleResponse>> UploadRule([FromBody] RuleUploadRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _marketplaceService.UploadRuleAsync(request);
            return CreatedAtAction(
                nameof(GetRuleById),
                new { id = result.Id },
                result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload rule '{Name}'", request.Name);
            return BadRequest(new { error = "Failed to upload rule" });
        }
    }

    /// <summary>
    /// Searches marketplace rules.
    /// </summary>
    /// <param name="q">Search query (name or description).</param>
    /// <param name="tags">Filter by tags (comma-separated).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(RuleListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleListResponse>> SearchRules(
        [FromQuery] string? q = null,
        [FromQuery] string? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        string[]? tagArray = null;
        if (!string.IsNullOrWhiteSpace(tags))
        {
            tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var result = await _marketplaceService.SearchRulesAsync(q, tagArray, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Increments the download count for a rule.
    /// </summary>
    /// <param name="id">Rule ID.</param>
    [HttpPost("rules/{id:guid}/download")]
    [ProducesResponseType(typeof(RuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RuleResponse>> IncrementDownload(Guid id)
    {
        var rule = await _marketplaceService.IncrementDownloadAsync(id);
        if (rule == null)
        {
            return NotFound(new { error = $"Rule with ID {id} not found" });
        }

        return Ok(rule);
    }

    /// <summary>
    /// Deletes a rule from the marketplace.
    /// </summary>
    /// <param name="id">Rule ID.</param>
    [HttpDelete("rules/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var deleted = await _marketplaceService.DeleteRuleAsync(id);
        if (!deleted)
        {
            return NotFound(new { error = $"Rule with ID {id} not found" });
        }

        return NoContent();
    }
}
