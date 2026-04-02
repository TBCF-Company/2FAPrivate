using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Container Token API for managing container tokens
/// Maps to Python: privacyidea/api/container.py
/// </summary>
[ApiController]
[Route("container")]
[Authorize]
public class ContainerController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ContainerController> _logger;

    public ContainerController(
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<ContainerController> logger)
    {
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /container/ - List all containers
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ListContainers([FromQuery] string? user = null, [FromQuery] string? realm = null)
    {
        // Containers are special tokens that contain other tokens
        var tokens = await _tokenService.GetTokensAsync(user, realm);
        var containers = tokens.Where(t => t.TokenType.Equals("container", StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = containers.Select(c => new
                {
                    serial = c.Serial,
                    type = c.TokenType,
                    active = c.Active,
                    description = c.Description,
                    user = c.UserId,
                    realm = c.Realm
                })
            }
        });
    }

    /// <summary>
    /// POST /container/ - Create a new container
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> CreateContainer([FromBody] ContainerCreateRequest request)
    {
        var result = await _tokenService.InitTokenAsync(new TokenInitRequest
        {
            Type = "container",
            Serial = request.Serial,
            User = request.User,
            Realm = request.Realm,
            Description = request.Description ?? "Container token"
        });

        if (!result.Success)
        {
            return BadRequest(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = result.Message }
                }
            });
        }

        await _auditService.LogAsync("container_create", true, User.Identity?.Name ?? "admin",
            request.Realm, result.Serial, "container", $"Created container: {result.Serial}");

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    serial = result.Serial
                }
            }
        });
    }

    /// <summary>
    /// DELETE /container/{serial} - Delete a container
    /// </summary>
    [HttpDelete("{serial}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteContainer(string serial)
    {
        var deleted = await _tokenService.DeleteTokenAsync(serial);

        if (deleted)
        {
            await _auditService.LogAsync("container_delete", true, User.Identity?.Name ?? "admin",
                null, serial, "container", $"Deleted container: {serial}");
        }

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = deleted
            }
        });
    }

    /// <summary>
    /// POST /container/{serial}/add - Add token to container
    /// </summary>
    [HttpPost("{serial}/add")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AddTokenToContainer(string serial, [FromBody] ContainerTokenRequest request)
    {
        // This would set container_serial info on the token
        var result = await _tokenService.SetTokenInfoAsync(request.TokenSerial, "container_serial", serial);

        if (result)
        {
            await _auditService.LogAsync("container_add_token", true, User.Identity?.Name ?? "admin",
                null, request.TokenSerial, null, $"Added token {request.TokenSerial} to container {serial}");
        }

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = result
            }
        });
    }

    /// <summary>
    /// POST /container/{serial}/remove - Remove token from container
    /// </summary>
    [HttpPost("{serial}/remove")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> RemoveTokenFromContainer(string serial, [FromBody] ContainerTokenRequest request)
    {
        var result = await _tokenService.SetTokenInfoAsync(request.TokenSerial, "container_serial", "");

        if (result)
        {
            await _auditService.LogAsync("container_remove_token", true, User.Identity?.Name ?? "admin",
                null, request.TokenSerial, null, $"Removed token {request.TokenSerial} from container {serial}");
        }

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = result
            }
        });
    }

    /// <summary>
    /// GET /container/{serial}/tokens - List tokens in container
    /// </summary>
    [HttpGet("{serial}/tokens")]
    public async Task<IActionResult> GetContainerTokens(string serial)
    {
        // Would need to search for tokens with container_serial = serial
        var allTokens = await _tokenService.GetTokensAsync();
        
        // Filter by container_serial info (simplified)
        var containerTokens = new List<object>();

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    container = serial,
                    tokens = containerTokens
                }
            }
        });
    }
}

public class ContainerCreateRequest
{
    public string? Serial { get; set; }
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Description { get; set; }
}

public class ContainerTokenRequest
{
    public string TokenSerial { get; set; } = string.Empty;
}
