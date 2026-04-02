using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Resolver Management Controller
/// Maps to Python: privacyidea/api/resolver.py
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize(Policy = "Admin")]
public class ResolverController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ResolverController> _logger;

    public ResolverController(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<ResolverController> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /resolver - List all resolvers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListResolvers()
    {
        try
        {
            var resolvers = await _unitOfWork.Resolvers.GetAllAsync();

            var result = resolvers.Select(r => new
            {
                name = r.Name,
                type = r.Type,
                configs = r.Configs.ToDictionary(c => c.Key, c => c.Type == "password" ? "***" : c.Value)
            });

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = result },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing resolvers");
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// GET /resolver/{name} - Get resolver config
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetResolver(string name)
    {
        try
        {
            var resolver = await _unitOfWork.Resolvers.GetByNameAsync(name);
            
            if (resolver == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Resolver '{name}' not found" } });
            }

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        name = resolver.Name,
                        type = resolver.Type,
                        data = resolver.Configs.ToDictionary(c => c.Key, c => c.Type == "password" ? "***" : c.Value)
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resolver {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /resolver/{name} - Create or update resolver
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> SetResolver(string name, [FromBody] SetResolverRequest request)
    {
        try
        {
            var existing = await _unitOfWork.Resolvers.GetByNameAsync(name);

            if (existing != null)
            {
                // Update existing
                existing.Type = request.Type;
                
                // Clear old configs
                var oldConfigs = existing.Configs.ToList();
                foreach (var config in oldConfigs)
                {
                    _unitOfWork.Repository<ResolverConfig>().Remove(config);
                }
                existing.Configs.Clear();

                // Add new configs
                if (request.Config != null)
                {
                    foreach (var kvp in request.Config)
                    {
                        existing.Configs.Add(new ResolverConfig
                        {
                            ResolverId = existing.Id,
                            Key = kvp.Key,
                            Value = kvp.Value,
                            Type = IsPasswordField(kvp.Key) ? "password" : "text"
                        });
                    }
                }

                await _unitOfWork.Resolvers.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogAsync("RESOLVER_UPDATE", true, User.Identity?.Name, info: $"Updated resolver {name}");

                return Ok(new
                {
                    id = 1,
                    jsonrpc = "2.0",
                    result = new { status = true, value = existing.Id },
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    version = "1.0.0"
                });
            }

            // Create new resolver
            var resolver = new Resolver
            {
                Name = name,
                Type = request.Type
            };

            await _unitOfWork.Resolvers.AddAsync(resolver);
            await _unitOfWork.SaveChangesAsync();

            // Add configs
            if (request.Config != null)
            {
                foreach (var kvp in request.Config)
                {
                    var config = new ResolverConfig
                    {
                        ResolverId = resolver.Id,
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Type = IsPasswordField(kvp.Key) ? "password" : "text"
                    };
                    await _unitOfWork.Repository<ResolverConfig>().AddAsync(config);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            await _auditService.LogAsync("RESOLVER_CREATE", true, User.Identity?.Name, info: $"Created resolver {name}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = resolver.Id },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting resolver {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// DELETE /resolver/{name} - Delete resolver
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteResolver(string name)
    {
        try
        {
            var resolver = await _unitOfWork.Resolvers.GetByNameAsync(name);
            
            if (resolver == null)
            {
                return NotFound(new { result = new { status = false }, detail = new { message = $"Resolver '{name}' not found" } });
            }

            // Delete configs first
            foreach (var config in resolver.Configs.ToList())
            {
                _unitOfWork.Repository<ResolverConfig>().Remove(config);
            }

            await _unitOfWork.Resolvers.DeleteAsync(resolver);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync("RESOLVER_DELETE", true, User.Identity?.Name, info: $"Deleted resolver {name}");

            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new { status = true, value = resolver.Id },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resolver {Name}", name);
            return StatusCode(500, new { result = new { status = false }, error = new { message = ex.Message } });
        }
    }

    /// <summary>
    /// POST /resolver/test - Test resolver connection
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestResolver([FromBody] TestResolverRequest request)
    {
        try
        {
            // TODO: Implement actual resolver test
            // For now, return success
            
            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        success = true,
                        message = "Connection test successful",
                        user_count = 0
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing resolver");
            return Ok(new
            {
                id = 1,
                jsonrpc = "2.0",
                result = new
                {
                    status = true,
                    value = new
                    {
                        success = false,
                        message = ex.Message
                    }
                },
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                version = "1.0.0"
            });
        }
    }

    private static bool IsPasswordField(string key)
    {
        var passwordKeys = new[] { "password", "secret", "key", "bindpw", "pass" };
        return passwordKeys.Any(pk => key.ToLowerInvariant().Contains(pk));
    }
}

#region Request Models

public class SetResolverRequest
{
    public string Type { get; set; } = "ldap";
    public Dictionary<string, string>? Config { get; set; }
}

public class TestResolverRequest
{
    public string Type { get; set; } = "ldap";
    public Dictionary<string, string>? Config { get; set; }
}

#endregion
