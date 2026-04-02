using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// RADIUS Server configuration API
/// Maps to Python: privacyidea/api/radiusserver.py
/// </summary>
[ApiController]
[Route("radiusserver")]
[Authorize(Policy = "Admin")]
public class RadiusController : ControllerBase
{
    private readonly IRadiusService _radiusService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RadiusController> _logger;

    public RadiusController(
        IRadiusService radiusService,
        IAuditService auditService,
        ILogger<RadiusController> logger)
    {
        _radiusService = radiusService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /radiusserver/ - List all RADIUS servers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListServers()
    {
        var servers = await _radiusService.GetAllServersAsync();

        var result = servers.Select(s => new
        {
            id = s.Id,
            identifier = s.Identifier,
            server = s.Server,
            port = s.Port,
            timeout = s.Timeout,
            retries = s.Retries,
            description = s.Description,
            dictionary = s.Dictionary
        });

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
    /// GET /radiusserver/{identifier} - Get specific RADIUS server
    /// </summary>
    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetServer(string identifier)
    {
        var server = await _radiusService.GetServerAsync(identifier);
        if (server == null)
        {
            return NotFound(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "RADIUS server not found" }
                }
            });
        }

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    id = server.Id,
                    identifier = server.Identifier,
                    server = server.Server,
                    port = server.Port,
                    timeout = server.Timeout,
                    retries = server.Retries,
                    description = server.Description,
                    dictionary = server.Dictionary
                }
            }
        });
    }

    /// <summary>
    /// POST /radiusserver - Create or update RADIUS server
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateServer([FromBody] RadiusServerRequest request)
    {
        await _auditService.LogAsync("radiusserver_create", true, User.Identity?.Name ?? "admin",
            null, null, null, $"Creating RADIUS server: {request.Identifier}");

        var serverId = await _radiusService.CreateServerAsync(new RadiusServerConfig
        {
            Identifier = request.Identifier,
            Server = request.Server,
            Port = request.Port,
            Secret = request.Secret,
            Timeout = request.Timeout ?? 5,
            Retries = request.Retries ?? 3,
            Description = request.Description,
            Dictionary = request.Dictionary
        });

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = serverId
            }
        });
    }

    /// <summary>
    /// DELETE /radiusserver/{identifier} - Delete RADIUS server
    /// </summary>
    [HttpDelete("{identifier}")]
    public async Task<IActionResult> DeleteServer(string identifier)
    {
        var deleted = await _radiusService.DeleteServerAsync(identifier);

        if (deleted)
        {
            await _auditService.LogAsync("radiusserver_delete", true, User.Identity?.Name ?? "admin",
                null, null, null, $"Deleted RADIUS server: {identifier}");
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
    /// POST /radiusserver/test - Test RADIUS server
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestServer([FromBody] RadiusTestRequest request)
    {
        var result = await _radiusService.TestConnectionAsync(
            request.Identifier ?? "",
            request.Username,
            request.Password);

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
}

public class RadiusServerRequest
{
    [Required]
    public string Identifier { get; set; } = string.Empty;
    
    [Required]
    public string Server { get; set; } = string.Empty;
    
    public int Port { get; set; } = 1812;
    
    [Required]
    public string Secret { get; set; } = string.Empty;
    
    public int? Timeout { get; set; }
    
    public int? Retries { get; set; }
    
    public string? Description { get; set; }
    
    public string? Dictionary { get; set; }
}

public class RadiusTestRequest
{
    public string? Identifier { get; set; }
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}
