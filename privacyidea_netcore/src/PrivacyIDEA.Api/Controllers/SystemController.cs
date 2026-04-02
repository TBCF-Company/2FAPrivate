using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Api.Models;
using PrivacyIDEA.Domain.Entities;
using PrivacyIDEA.Infrastructure.Data;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// System API - System configuration endpoints
/// Maps to Python: privacyidea/api/system.py
/// </summary>
[ApiController]
[Route("[controller]")]
public class SystemController : ControllerBase
{
    private readonly PrivacyIdeaDbContext _context;
    private readonly ILogger<SystemController> _logger;

    public SystemController(PrivacyIdeaDbContext context, ILogger<SystemController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all system configuration
    /// GET /system/
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var configs = await _context.Configs.ToListAsync();
            var configDict = configs.ToDictionary(c => c.Key, c => c.Value ?? "");

            return Ok(new ApiResponse<Dictionary<string, string>>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<Dictionary<string, string>>
                {
                    Status = true,
                    Value = configDict
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system config");
            return Ok(new ApiResponse<Dictionary<string, string>>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<Dictionary<string, string>>
                {
                    Status = false,
                    Error = new ErrorInfo { Code = 500, Message = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Set a system configuration value
    /// POST /system/setConfig
    /// </summary>
    [HttpPost("setConfig")]
    public async Task<IActionResult> SetConfig([FromForm] string key, [FromForm] string value)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var config = await _context.Configs.FirstOrDefaultAsync(c => c.Key == key);
            if (config == null)
            {
                config = new Config { Key = key, Value = value };
                _context.Configs.Add(config);
            }
            else
            {
                config.Value = value;
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { key, value }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting config");
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
    /// Delete a system configuration value
    /// DELETE /system/{key}
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteConfig(string key)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var config = await _context.Configs.FirstOrDefaultAsync(c => c.Key == key);
            if (config != null)
            {
                _context.Configs.Remove(config);
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new { key }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting config");
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
    /// Get system documentation
    /// GET /system/documentation
    /// </summary>
    [HttpGet("documentation")]
    public IActionResult GetDocumentation()
    {
        return Ok(new ApiResponse<object>
        {
            Result = new ResultWrapper<object>
            {
                Status = true,
                Value = new
                {
                    product = "PrivacyIDEA.NET",
                    version = "1.0.0",
                    framework = ".NET 8",
                    documentation_url = "https://privacyidea.readthedocs.io"
                }
            }
        });
    }
}
