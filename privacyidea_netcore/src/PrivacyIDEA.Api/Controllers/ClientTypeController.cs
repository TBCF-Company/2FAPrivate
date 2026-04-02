using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Client type management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ClientTypeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClientTypeController> _logger;

    public ClientTypeController(IUnitOfWork unitOfWork, ILogger<ClientTypeController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all client applications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _unitOfWork.Query<ClientApplication>()
            .OrderByDescending(c => c.LastSeen)
            .ToListAsync();

        return Ok(new
        {
            result = new
            {
                value = clients.Select(c => new
                {
                    c.Id,
                    c.IP,
                    c.Hostname,
                    clienttype = c.ClientType,
                    lastseen = c.LastSeen,
                    c.Node
                })
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Register a client application
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] ClientTypeRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? request.IP;

        var existing = await _unitOfWork.Query<ClientApplication>()
            .FirstOrDefaultAsync(c => c.IP == clientIp);

        if (existing != null)
        {
            existing.Hostname = request.Hostname;
            existing.ClientType = request.ClientType;
            existing.LastSeen = DateTime.UtcNow;
            existing.Node = Environment.MachineName;
            await _unitOfWork.SaveChangesAsync();

            return Ok(new
            {
                result = new { status = true, value = existing.Id },
                version = "1.0",
                id = 1
            });
        }

        var client = new ClientApplication
        {
            IP = clientIp,
            Hostname = request.Hostname,
            ClientType = request.ClientType,
            LastSeen = DateTime.UtcNow,
            Node = Environment.MachineName
        };

        _unitOfWork.Add(client);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Client registered: {IP} ({Type})", clientIp, request.ClientType);

        return Ok(new
        {
            result = new { status = true, value = client.Id },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a client registration
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _unitOfWork.Query<ClientApplication>()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return NotFound(new { result = new { status = false }, detail = "Client not found" });

        _unitOfWork.Delete(client);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Client deleted: {Id}", id);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }
}

public class ClientTypeRequest
{
    public string IP { get; set; } = string.Empty;
    public string? Hostname { get; set; }
    public string? ClientType { get; set; }
}
