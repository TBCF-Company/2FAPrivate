using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Core.EventHandlers;
using System.ComponentModel.DataAnnotations;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// SMS Gateway configuration API
/// Maps to Python: privacyidea/api/smsgw.py
/// </summary>
[ApiController]
[Route("smsgw")]
[Authorize(Policy = "Admin")]
public class SmsgwController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<SmsgwController> _logger;

    public SmsgwController(
        ISmsService smsService,
        IAuditService auditService,
        ILogger<SmsgwController> logger)
    {
        _smsService = smsService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /smsgw/ - List all SMS gateways
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListGateways()
    {
        var gateways = await _smsService.GetAllGatewaysAsync();

        var result = gateways.Select(g => new
        {
            id = g.Id,
            name = g.Name,
            description = g.Description,
            provider = g.Provider,
            options = g.Options
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
    /// GET /smsgw/{id} - Get specific SMS gateway
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGateway(int id)
    {
        var gateway = await _smsService.GetGatewayAsync(id);
        if (gateway == null)
        {
            return NotFound(new
            {
                jsonrpc = "2.0",
                result = new
                {
                    status = false,
                    error = new { message = "SMS Gateway not found" }
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
                    id = gateway.Id,
                    name = gateway.Name,
                    description = gateway.Description,
                    provider = gateway.Provider,
                    options = gateway.Options
                }
            }
        });
    }

    /// <summary>
    /// POST /smsgw - Create or update SMS gateway
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateGateway([FromBody] SmsgwRequest request)
    {
        await _auditService.LogAsync("smsgw_create", true, User.Identity?.Name ?? "admin",
            null, null, null, $"Creating SMS gateway: {request.Name}");

        var gatewayId = await _smsService.CreateGatewayAsync(
            request.Name,
            request.Provider,
            request.Options ?? new Dictionary<string, object>(),
            request.Description);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = gatewayId
            }
        });
    }

    /// <summary>
    /// DELETE /smsgw/{name} - Delete SMS gateway
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteGateway(string name)
    {
        var deleted = await _smsService.DeleteGatewayAsync(name);

        if (deleted)
        {
            await _auditService.LogAsync("smsgw_delete", true, User.Identity?.Name ?? "admin",
                null, null, null, $"Deleted SMS gateway: {name}");
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
    /// GET /smsgw/option - Get all SMS provider definitions
    /// </summary>
    [HttpGet("option")]
    public IActionResult GetProviderOptions()
    {
        var providers = new Dictionary<string, object>
        {
            ["HttpSmsProvider"] = new
            {
                description = "HTTP SMS Provider for generic HTTP gateways",
                options = new
                {
                    URL = new { type = "str", required = true, desc = "Gateway URL" },
                    HTTP_METHOD = new { type = "str", required = false, desc = "GET or POST" },
                    TIMEOUT = new { type = "int", required = false, desc = "Timeout in seconds" },
                    USERNAME = new { type = "str", required = false, desc = "Auth username" },
                    PASSWORD = new { type = "password", required = false, desc = "Auth password" },
                    RETURN_SUCCESS = new { type = "str", required = false, desc = "Success pattern" },
                    RETURN_FAIL = new { type = "str", required = false, desc = "Fail pattern" }
                }
            },
            ["SmppSmsProvider"] = new
            {
                description = "SMPP SMS Provider",
                options = new
                {
                    SMSC_HOST = new { type = "str", required = true, desc = "SMSC host" },
                    SMSC_PORT = new { type = "int", required = true, desc = "SMSC port" },
                    SYSTEM_ID = new { type = "str", required = true, desc = "System ID" },
                    PASSWORD = new { type = "password", required = true, desc = "Password" },
                    SOURCE_ADDR = new { type = "str", required = false, desc = "Source address" }
                }
            },
            ["TwilioSmsProvider"] = new
            {
                description = "Twilio SMS Provider",
                options = new
                {
                    ACCOUNT_SID = new { type = "str", required = true, desc = "Account SID" },
                    AUTH_TOKEN = new { type = "password", required = true, desc = "Auth Token" },
                    FROM_NUMBER = new { type = "str", required = true, desc = "From number" }
                }
            },
            ["AwsSnsSmsProvider"] = new
            {
                description = "AWS SNS SMS Provider",
                options = new
                {
                    ACCESS_KEY_ID = new { type = "str", required = true, desc = "AWS Access Key ID" },
                    SECRET_ACCESS_KEY = new { type = "password", required = true, desc = "AWS Secret Key" },
                    REGION = new { type = "str", required = true, desc = "AWS Region" }
                }
            }
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = providers
            }
        });
    }
}

public class SmsgwRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string Provider { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public Dictionary<string, object>? Options { get; set; }
}
