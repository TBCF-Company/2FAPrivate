using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// CA Connector management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class CAConnectorController : ControllerBase
{
    private readonly ICAConnectorService _caConnectorService;
    private readonly ILogger<CAConnectorController> _logger;

    public CAConnectorController(ICAConnectorService caConnectorService, ILogger<CAConnectorController> logger)
    {
        _caConnectorService = caConnectorService;
        _logger = logger;
    }

    /// <summary>
    /// Get all CA connectors
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var connectors = await _caConnectorService.GetConnectorsAsync();
        return Ok(new
        {
            result = new { value = connectors.ToList() },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific CA connector
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var connector = await _caConnectorService.GetConnectorAsync(name);
        if (connector == null)
            return NotFound(new { result = new { status = false }, detail = $"CA connector '{name}' not found" });

        return Ok(new
        {
            result = new { value = connector },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Create or update a CA connector
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> CreateOrUpdate(string name, [FromBody] CAConnectorRequest request)
    {
        var connector = await _caConnectorService.CreateOrUpdateConnectorAsync(
            name,
            request.Type,
            request.Config ?? new Dictionary<string, string>()
        );

        _logger.LogInformation("CA connector created/updated: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = connector },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a CA connector
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var deleted = await _caConnectorService.DeleteConnectorAsync(name);
        if (!deleted)
            return NotFound(new { result = new { status = false }, detail = $"CA connector '{name}' not found" });

        _logger.LogInformation("CA connector deleted: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get available CA connector types
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetTypes()
    {
        var types = _caConnectorService.GetAvailableTypes();
        return Ok(new
        {
            result = new { value = types },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get CA certificates
    /// </summary>
    [HttpGet("{name}/cacerts")]
    public async Task<IActionResult> GetCACertificates(string name)
    {
        var certificates = await _caConnectorService.GetCACertificatesAsync(name);
        return Ok(new
        {
            result = new { value = certificates.ToList() },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Request a certificate
    /// </summary>
    [HttpPost("{name}/request")]
    public async Task<IActionResult> RequestCertificate(string name, [FromBody] CertificateRequest request)
    {
        var result = await _caConnectorService.RequestCertificateAsync(name, request);
        
        if (!result.Success)
        {
            return BadRequest(new
            {
                result = new { status = false },
                detail = result.ErrorMessage
            });
        }

        return Ok(new
        {
            result = new { status = true, value = result },
            version = "1.0",
            id = 1
        });
    }
}

public class CAConnectorRequest
{
    public string Type { get; set; } = "local";
    public Dictionary<string, string>? Config { get; set; }
}
