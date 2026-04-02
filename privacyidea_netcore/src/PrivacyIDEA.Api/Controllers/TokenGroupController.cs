using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Token Group management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class TokenGroupController : ControllerBase
{
    private readonly ITokenGroupService _tokenGroupService;
    private readonly ILogger<TokenGroupController> _logger;

    public TokenGroupController(ITokenGroupService tokenGroupService, ILogger<TokenGroupController> logger)
    {
        _tokenGroupService = tokenGroupService;
        _logger = logger;
    }

    /// <summary>
    /// Get all token groups
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _tokenGroupService.GetGroupsAsync();
        return Ok(new
        {
            result = new { value = groups.ToList() },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific token group
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var group = await _tokenGroupService.GetGroupAsync(name);
        if (group == null)
            return NotFound(new { result = new { status = false }, detail = $"Token group '{name}' not found" });

        return Ok(new
        {
            result = new { value = group },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Create a token group
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> Create(string name, [FromBody] TokenGroupRequest? request)
    {
        try
        {
            var group = await _tokenGroupService.CreateGroupAsync(name, request?.Description);
            _logger.LogInformation("Token group created: {Name}", name);

            return Ok(new
            {
                result = new { status = true, value = group },
                version = "1.0",
                id = 1
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { result = new { status = false }, detail = ex.Message });
        }
    }

    /// <summary>
    /// Update a token group
    /// </summary>
    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name, [FromBody] TokenGroupRequest request)
    {
        var group = await _tokenGroupService.UpdateGroupAsync(name, request.Description);
        if (group == null)
            return NotFound(new { result = new { status = false }, detail = $"Token group '{name}' not found" });

        return Ok(new
        {
            result = new { status = true, value = group },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a token group
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var deleted = await _tokenGroupService.DeleteGroupAsync(name);
        if (!deleted)
            return NotFound(new { result = new { status = false }, detail = $"Token group '{name}' not found" });

        _logger.LogInformation("Token group deleted: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Add a token to a group
    /// </summary>
    [HttpPost("{name}/token/{serial}")]
    public async Task<IActionResult> AddToken(string name, string serial)
    {
        var added = await _tokenGroupService.AddTokenToGroupAsync(name, serial);
        if (!added)
            return BadRequest(new { result = new { status = false }, detail = "Failed to add token to group" });

        _logger.LogInformation("Token {Serial} added to group {Group}", serial, name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Remove a token from a group
    /// </summary>
    [HttpDelete("{name}/token/{serial}")]
    public async Task<IActionResult> RemoveToken(string name, string serial)
    {
        var removed = await _tokenGroupService.RemoveTokenFromGroupAsync(name, serial);
        if (!removed)
            return BadRequest(new { result = new { status = false }, detail = "Failed to remove token from group" });

        _logger.LogInformation("Token {Serial} removed from group {Group}", serial, name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get all tokens in a group
    /// </summary>
    [HttpGet("{name}/tokens")]
    public async Task<IActionResult> GetTokens(string name)
    {
        var tokens = await _tokenGroupService.GetTokensInGroupAsync(name);
        return Ok(new
        {
            result = new { value = tokens.ToList() },
            version = "1.0",
            id = 1
        });
    }
}

public class TokenGroupRequest
{
    public string? Description { get; set; }
}
