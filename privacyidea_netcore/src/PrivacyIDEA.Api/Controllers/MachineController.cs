using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Machine Token API - for SSH, RADIUS, LUKS tokens
/// Maps to Python: privacyidea/api/machineresolver.py and related
/// </summary>
[ApiController]
[Route("machine")]
[Authorize(Policy = "Admin")]
public class MachineController : ControllerBase
{
    private readonly IMachineService _machineService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MachineController> _logger;

    public MachineController(
        IMachineService machineService,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<MachineController> logger)
    {
        _machineService = machineService;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /machine/ - List all machines
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListMachines([FromQuery] string? hostname = null)
    {
        var machines = await _machineService.GetMachinesAsync(hostname);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = machines
            }
        });
    }

    /// <summary>
    /// POST /machine - Create a machine
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMachine([FromBody] MachineRequest request)
    {
        var machineId = await _machineService.CreateMachineAsync(new MachineInfo
        {
            Hostname = request.Hostname,
            Ip = request.Ip,
            Resolver = request.Resolver,
            Description = request.Description
        });

        await _auditService.LogAsync("machine_create", true, User.Identity?.Name ?? "admin",
            null, null, null, $"Created machine: {request.Hostname}");

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = machineId
            }
        });
    }

    /// <summary>
    /// DELETE /machine/{hostname} - Delete a machine
    /// </summary>
    [HttpDelete("{hostname}")]
    public async Task<IActionResult> DeleteMachine(string hostname)
    {
        var deleted = await _machineService.DeleteMachineAsync(hostname);

        if (deleted)
        {
            await _auditService.LogAsync("machine_delete", true, User.Identity?.Name ?? "admin",
                null, null, null, $"Deleted machine: {hostname}");
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
    /// POST /machine/token - Attach token to machine
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> AttachToken([FromBody] MachineTokenRequest request)
    {
        var result = await _machineService.AttachTokenAsync(
            request.Hostname,
            request.Serial,
            request.Application,
            request.Options);

        await _auditService.LogAsync("machine_token_attach", result, User.Identity?.Name ?? "admin",
            null, request.Serial, null, $"Attached token {request.Serial} to machine {request.Hostname}");

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
    /// DELETE /machine/token/{serial}/{hostname}/{application} - Detach token from machine
    /// </summary>
    [HttpDelete("token/{serial}/{hostname}/{application}")]
    public async Task<IActionResult> DetachToken(string serial, string hostname, string application)
    {
        var result = await _machineService.DetachTokenAsync(hostname, serial, application);

        await _auditService.LogAsync("machine_token_detach", result, User.Identity?.Name ?? "admin",
            null, serial, null, $"Detached token {serial} from machine {hostname}");

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
    /// GET /machine/token - Get machine tokens
    /// </summary>
    [HttpGet("token")]
    public async Task<IActionResult> GetMachineTokens([FromQuery] string? hostname = null, [FromQuery] string? serial = null)
    {
        var tokens = await _machineService.GetMachineTokensAsync(hostname, serial);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = tokens
            }
        });
    }

    /// <summary>
    /// GET /machine/authitem - Get authentication items (SSH keys, LUKS, etc.)
    /// </summary>
    [HttpGet("authitem/{application}")]
    public async Task<IActionResult> GetAuthItems(string application, [FromQuery] string? hostname = null)
    {
        var items = await _machineService.GetAuthItemsAsync(application, hostname);

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new
                {
                    application = application,
                    hostname = hostname,
                    items = items
                }
            }
        });
    }
}

public class MachineRequest
{
    public string Hostname { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? Resolver { get; set; }
    public string? Description { get; set; }
}

public class MachineTokenRequest
{
    public string Hostname { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public Dictionary<string, string>? Options { get; set; }
}
