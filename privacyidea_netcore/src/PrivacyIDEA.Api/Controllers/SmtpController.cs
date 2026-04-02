using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// SMTP Server configuration API
/// Maps to Python: privacyidea/api/smtpserver.py
/// </summary>
[ApiController]
[Route("smtpserver")]
[Authorize(Policy = "Admin")]
public class SmtpController : ControllerBase
{
    private readonly ISmtpService _smtpService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SmtpController> _logger;

    public SmtpController(
        ISmtpService smtpService,
        IAuditService auditService,
        ILogger<SmtpController> logger)
    {
        _smtpService = smtpService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /smtpserver/ - List all SMTP servers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListServers()
    {
        var servers = await _smtpService.GetAllServersAsync();

        var result = servers.Select(s => new
        {
            id = s.Id,
            identifier = s.Identifier,
            server = s.Server,
            port = s.Port,
            username = s.Username,
            sender = s.Sender,
            tls = s.Tls,
            timeout = s.Timeout,
            description = s.Description
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
    /// GET /smtpserver/{identifier} - Get specific SMTP server
    /// </summary>
    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetServer(string identifier)
    {
        var server = await _smtpService.GetServerAsync(identifier);
        if (server == null)
        {
            return NotFound(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "SMTP server not found" }
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
                    username = server.Username,
                    sender = server.Sender,
                    tls = server.Tls,
                    timeout = server.Timeout,
                    description = server.Description
                }
            }
        });
    }

    /// <summary>
    /// POST /smtpserver - Create or update SMTP server
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateServer([FromBody] SmtpServerRequest request)
    {
        await _auditService.LogAsync("smtpserver_create", true, User.Identity?.Name ?? "admin",
            null, null, null, $"Creating SMTP server: {request.Identifier}");

        var serverId = await _smtpService.CreateServerAsync(new SmtpServerConfig
        {
            Identifier = request.Identifier,
            Server = request.Server,
            Port = request.Port,
            Username = request.Username,
            Password = request.Password,
            Sender = request.Sender,
            Tls = request.Tls,
            Timeout = request.Timeout ?? 10,
            Description = request.Description
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
    /// DELETE /smtpserver/{identifier} - Delete SMTP server
    /// </summary>
    [HttpDelete("{identifier}")]
    public async Task<IActionResult> DeleteServer(string identifier)
    {
        var deleted = await _smtpService.DeleteServerAsync(identifier);

        if (deleted)
        {
            await _auditService.LogAsync("smtpserver_delete", true, User.Identity?.Name ?? "admin",
                null, null, null, $"Deleted SMTP server: {identifier}");
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
    /// POST /smtpserver/test - Test SMTP server
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestServer([FromBody] SmtpTestRequest request)
    {
        var result = await _smtpService.TestConnectionAsync(
            request.Identifier ?? "",
            request.Recipient);

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

public class SmtpServerRequest
{
    [Required]
    public string Identifier { get; set; } = string.Empty;
    
    [Required]
    public string Server { get; set; } = string.Empty;
    
    public int Port { get; set; } = 25;
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    [Required]
    public string Sender { get; set; } = string.Empty;
    
    public bool Tls { get; set; } = false;
    
    public int? Timeout { get; set; }
    
    public string? Description { get; set; }
}

public class SmtpTestRequest
{
    public string? Identifier { get; set; }
    
    [Required]
    public string Recipient { get; set; } = string.Empty;
}
