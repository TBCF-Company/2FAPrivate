using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// PrivacyIDEA Server federation API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class PrivacyIDEAServerController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PrivacyIDEAServerController> _logger;

    public PrivacyIDEAServerController(
        IUnitOfWork unitOfWork, 
        IHttpClientFactory httpClientFactory,
        ILogger<PrivacyIDEAServerController> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get all PrivacyIDEA servers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var servers = await _unitOfWork.Query<PrivacyIDEAServer>().ToListAsync();
        
        return Ok(new
        {
            result = new
            {
                value = servers.Select(s => new
                {
                    s.Id,
                    s.Identifier,
                    s.Url,
                    s.Description,
                    s.Tls
                })
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific PrivacyIDEA server
    /// </summary>
    [HttpGet("{identifier}")]
    public async Task<IActionResult> Get(string identifier)
    {
        var server = await _unitOfWork.Query<PrivacyIDEAServer>()
            .FirstOrDefaultAsync(s => s.Identifier == identifier);

        if (server == null)
            return NotFound(new { result = new { status = false }, detail = $"Server '{identifier}' not found" });

        return Ok(new
        {
            result = new { value = new { server.Id, server.Identifier, server.Url, server.Description, server.Tls } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Create or update a PrivacyIDEA server
    /// </summary>
    [HttpPost("{identifier}")]
    public async Task<IActionResult> CreateOrUpdate(string identifier, [FromBody] PrivacyIDEAServerRequest request)
    {
        var existing = await _unitOfWork.Query<PrivacyIDEAServer>()
            .FirstOrDefaultAsync(s => s.Identifier == identifier);

        if (existing != null)
        {
            existing.Url = request.Url;
            existing.Description = request.Description;
            existing.Tls = request.Tls;
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("PrivacyIDEA server updated: {Identifier}", identifier);
            
            return Ok(new
            {
                result = new { status = true, value = new { existing.Id, existing.Identifier, existing.Url, existing.Description, existing.Tls } },
                version = "1.0",
                id = 1
            });
        }

        var server = new PrivacyIDEAServer
        {
            Identifier = identifier,
            Url = request.Url,
            Description = request.Description,
            Tls = request.Tls
        };

        _unitOfWork.Add(server);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("PrivacyIDEA server created: {Identifier}", identifier);

        return Ok(new
        {
            result = new { status = true, value = new { server.Id, server.Identifier, server.Url, server.Description, server.Tls } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a PrivacyIDEA server
    /// </summary>
    [HttpDelete("{identifier}")]
    public async Task<IActionResult> Delete(string identifier)
    {
        var server = await _unitOfWork.Query<PrivacyIDEAServer>()
            .FirstOrDefaultAsync(s => s.Identifier == identifier);

        if (server == null)
            return NotFound(new { result = new { status = false }, detail = $"Server '{identifier}' not found" });

        _unitOfWork.Delete(server);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("PrivacyIDEA server deleted: {Identifier}", identifier);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Test connection to a PrivacyIDEA server
    /// </summary>
    [HttpPost("{identifier}/test")]
    public async Task<IActionResult> TestConnection(string identifier)
    {
        var server = await _unitOfWork.Query<PrivacyIDEAServer>()
            .FirstOrDefaultAsync(s => s.Identifier == identifier);

        if (server == null)
            return NotFound(new { result = new { status = false }, detail = $"Server '{identifier}' not found" });

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{server.Url.TrimEnd('/')}/");
            
            return Ok(new
            {
                result = new
                {
                    status = true,
                    value = new
                    {
                        reachable = response.IsSuccessStatusCode,
                        status_code = (int)response.StatusCode
                    }
                },
                version = "1.0",
                id = 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to PrivacyIDEA server: {Identifier}", identifier);
            
            return Ok(new
            {
                result = new
                {
                    status = false,
                    value = new { reachable = false, error = ex.Message }
                },
                version = "1.0",
                id = 1
            });
        }
    }
}

public class PrivacyIDEAServerRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Tls { get; set; } = true;
}
