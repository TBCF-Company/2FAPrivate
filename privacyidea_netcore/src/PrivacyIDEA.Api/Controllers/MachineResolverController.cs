using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Machine Resolver management API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class MachineResolverController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MachineResolverController> _logger;

    public MachineResolverController(IUnitOfWork unitOfWork, ILogger<MachineResolverController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all machine resolvers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resolvers = await _unitOfWork.Query<MachineResolver>()
            .Include(r => r.Configs)
            .ToListAsync();

        return Ok(new
        {
            result = new
            {
                value = resolvers.Select(r => new
                {
                    r.Id,
                    r.Name,
                    type = r.ResolverType,
                    config = r.Configs.ToDictionary(c => c.Key, c => c.Value)
                })
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific machine resolver
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var resolver = await _unitOfWork.Query<MachineResolver>()
            .Include(r => r.Configs)
            .FirstOrDefaultAsync(r => r.Name == name);

        if (resolver == null)
            return NotFound(new { result = new { status = false }, detail = $"Machine resolver '{name}' not found" });

        return Ok(new
        {
            result = new
            {
                value = new
                {
                    resolver.Id,
                    resolver.Name,
                    type = resolver.ResolverType,
                    config = resolver.Configs.ToDictionary(c => c.Key, c => c.Value)
                }
            },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Create or update a machine resolver
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> CreateOrUpdate(string name, [FromBody] MachineResolverRequest request)
    {
        var existing = await _unitOfWork.Query<MachineResolver>()
            .Include(r => r.Configs)
            .FirstOrDefaultAsync(r => r.Name == name);

        if (existing != null)
        {
            existing.ResolverType = request.Type;
            
            // Clear existing configs
            foreach (var config in existing.Configs.ToList())
            {
                _unitOfWork.Delete(config);
            }

            // Add new configs
            if (request.Config != null)
            {
                foreach (var kvp in request.Config)
                {
                    _unitOfWork.Add(new MachineResolverConfig
                    {
                        MachineResolverId = existing.Id,
                        Key = kvp.Key,
                        Value = kvp.Value
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Machine resolver updated: {Name}", name);

            return Ok(new
            {
                result = new { status = true, value = existing.Id },
                version = "1.0",
                id = 1
            });
        }

        var resolver = new MachineResolver
        {
            Name = name,
            ResolverType = request.Type
        };

        _unitOfWork.Add(resolver);
        await _unitOfWork.SaveChangesAsync();

        // Add configs
        if (request.Config != null)
        {
            foreach (var kvp in request.Config)
            {
                _unitOfWork.Add(new MachineResolverConfig
                {
                    MachineResolverId = resolver.Id,
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation("Machine resolver created: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = resolver.Id },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Delete a machine resolver
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        var resolver = await _unitOfWork.Query<MachineResolver>()
            .FirstOrDefaultAsync(r => r.Name == name);

        if (resolver == null)
            return NotFound(new { result = new { status = false }, detail = $"Machine resolver '{name}' not found" });

        _unitOfWork.Delete(resolver);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Machine resolver deleted: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get available machine resolver types
    /// </summary>
    [HttpGet("types")]
    public IActionResult GetTypes()
    {
        var types = new[] { "hosts", "ldap", "sql" };
        return Ok(new
        {
            result = new { value = types },
            version = "1.0",
            id = 1
        });
    }
}

public class MachineResolverRequest
{
    public string Type { get; set; } = "hosts";
    public Dictionary<string, string>? Config { get; set; }
}
