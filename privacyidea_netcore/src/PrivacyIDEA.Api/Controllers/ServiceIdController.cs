using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Service ID management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class ServiceIdController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ServiceIdController> _logger;

    public ServiceIdController(IUnitOfWork unitOfWork, ILogger<ServiceIdController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all service IDs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var serviceIds = await _unitOfWork.Query<Domain.Entities.ServiceId>().ToListAsync();
        return Ok(new
        {
            result = new { value = serviceIds.Select(s => new { s.Id, s.Name, s.Description }) },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific service ID
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var serviceId = await _unitOfWork.Query<Domain.Entities.ServiceId>()
            .FirstOrDefaultAsync(s => s.Name == name);

        if (serviceId == null)
            return NotFound(new { result = new { status = false }, detail = $"Service ID '{name}' not found" });

        return Ok(new
        {
            result = new { value = new { serviceId.Id, serviceId.Name, serviceId.Description } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Create a service ID
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> Create(string name, [FromBody] ServiceIdRequest? request)
    {
        var existing = await _unitOfWork.Query<Domain.Entities.ServiceId>()
            .FirstOrDefaultAsync(s => s.Name == name);

        if (existing != null)
            return BadRequest(new { result = new { status = false }, detail = $"Service ID '{name}' already exists" });

        var serviceId = new Domain.Entities.ServiceId
        {
            Name = name,
            Description = request?.Description
        };

        _unitOfWork.Add(serviceId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Service ID created: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = new { serviceId.Id, serviceId.Name, serviceId.Description } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a service ID
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var serviceId = await _unitOfWork.Query<Domain.Entities.ServiceId>()
            .FirstOrDefaultAsync(s => s.Name == name);

        if (serviceId == null)
            return NotFound(new { result = new { status = false }, detail = $"Service ID '{name}' not found" });

        _unitOfWork.Delete(serviceId);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Service ID deleted: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }
}

public class ServiceIdRequest
{
    public string? Description { get; set; }
}
