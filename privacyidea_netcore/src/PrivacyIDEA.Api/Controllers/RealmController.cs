using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Api.Models;
using PrivacyIDEA.Domain.Entities;
using PrivacyIDEA.Infrastructure.Data;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Realm API - Realm management endpoints
/// Maps to Python: privacyidea/api/realm.py
/// </summary>
[ApiController]
[Route("[controller]")]
public class RealmController : ControllerBase
{
    private readonly PrivacyIdeaDbContext _context;
    private readonly ILogger<RealmController> _logger;

    public RealmController(PrivacyIdeaDbContext context, ILogger<RealmController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// List all realms
    /// GET /realm/
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRealms()
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var realms = await _context.Realms
                .Include(r => r.ResolverRealms)
                    .ThenInclude(rr => rr.Resolver)
                .ToListAsync();

            var realmDtos = realms.Select(r => new RealmDto
            {
                Id = r.Id,
                Name = r.Name,
                IsDefault = r.IsDefault,
                Resolvers = r.ResolverRealms?.Select(rr => new ResolverInRealmDto
                {
                    Name = rr.Resolver?.Name ?? "",
                    Type = rr.Resolver?.Type ?? "",
                    Priority = rr.Priority
                })
            });

            return Ok(new ApiResponse<IEnumerable<RealmDto>>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<IEnumerable<RealmDto>>
                {
                    Status = true,
                    Value = realmDtos
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realms");
            return Ok(new ApiResponse<IEnumerable<RealmDto>>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<IEnumerable<RealmDto>>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Get a specific realm
    /// GET /realm/{name}
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetRealm(string name)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var realm = await _context.Realms
                .Include(r => r.ResolverRealms)
                    .ThenInclude(rr => rr.Resolver)
                .FirstOrDefaultAsync(r => r.Name == name);

            if (realm == null)
            {
                return NotFound(new ApiResponse<RealmDto>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<RealmDto>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Realm {name} not found" }
                    }
                });
            }

            var realmDto = new RealmDto
            {
                Id = realm.Id,
                Name = realm.Name,
                IsDefault = realm.IsDefault,
                Resolvers = realm.ResolverRealms?.Select(rr => new ResolverInRealmDto
                {
                    Name = rr.Resolver?.Name ?? "",
                    Type = rr.Resolver?.Type ?? "",
                    Priority = rr.Priority
                })
            };

            return Ok(new ApiResponse<RealmDto>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<RealmDto>
                {
                    Status = true,
                    Value = realmDto
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realm {Name}", name);
            return Ok(new ApiResponse<RealmDto>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<RealmDto>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Create or update a realm
    /// POST /realm/{name}
    /// </summary>
    [HttpPost("{name}")]
    public async Task<IActionResult> SetRealm(string name, [FromForm] string? resolvers = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var realm = await _context.Realms.FirstOrDefaultAsync(r => r.Name == name);
            if (realm == null)
            {
                realm = new Realm { Name = name };
                _context.Realms.Add(realm);
            }

            await _context.SaveChangesAsync();

            // TODO: Handle resolvers assignment

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { name = realm.Name, id = realm.Id }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting realm");
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Delete a realm
    /// DELETE /realm/{name}
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteRealm(string name)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var realm = await _context.Realms.FirstOrDefaultAsync(r => r.Name == name);
            if (realm == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<object>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Realm {name} not found" }
                    }
                });
            }

            _context.Realms.Remove(realm);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { name }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting realm");
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Set default realm
    /// POST /defaultrealm/{name}
    /// </summary>
    [HttpPost("/defaultrealm/{name}")]
    public async Task<IActionResult> SetDefaultRealm(string name)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Clear existing default
            var currentDefault = await _context.Realms.Where(r => r.IsDefault).ToListAsync();
            foreach (var r in currentDefault)
            {
                r.IsDefault = false;
            }

            // Set new default
            var realm = await _context.Realms.FirstOrDefaultAsync(r => r.Name == name);
            if (realm == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Time = (DateTime.UtcNow - startTime).TotalSeconds,
                    Result = new ResultWrapper<object>
                    {
                        Status = false,
                        Error = new ErrorInfo { Code = 404, Message = $"Realm {name} not found" }
                    }
                });
            }

            realm.IsDefault = true;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { name, isDefault = true }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default realm");
            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }
}
