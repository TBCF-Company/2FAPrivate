using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivacyIDEA.Api.Models;
using PrivacyIDEA.Infrastructure.Data;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Audit API - Audit log endpoints
/// Maps to Python: privacyidea/api/audit.py
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuditController : ControllerBase
{
    private readonly PrivacyIdeaDbContext _context;
    private readonly ILogger<AuditController> _logger;

    public AuditController(PrivacyIdeaDbContext context, ILogger<AuditController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get audit log entries
    /// GET /audit/
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string? serial = null,
        [FromQuery] string? user = null,
        [FromQuery] string? action = null,
        [FromQuery] bool? success = null,
        [FromQuery] DateTime? startdate = null,
        [FromQuery] DateTime? enddate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pagesize = 15,
        [FromQuery] string sortorder = "desc")
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var query = _context.AuditEntries.AsQueryable();

            if (!string.IsNullOrEmpty(serial))
                query = query.Where(a => a.Serial != null && a.Serial.Contains(serial));
            
            if (!string.IsNullOrEmpty(user))
                query = query.Where(a => a.User != null && a.User.Contains(user));
            
            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action != null && a.Action.Contains(action));
            
            if (success.HasValue)
                query = query.Where(a => a.Success == success.Value);
            
            if (startdate.HasValue)
                query = query.Where(a => a.Date >= startdate.Value);
            
            if (enddate.HasValue)
                query = query.Where(a => a.Date <= enddate.Value);

            query = sortorder.ToLower() == "asc" 
                ? query.OrderBy(a => a.Date) 
                : query.OrderByDescending(a => a.Date);

            var totalCount = await query.CountAsync();
            var entries = await query
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .ToListAsync();

            var auditDtos = entries.Select(a => new
            {
                a.Id,
                a.Date,
                a.Action,
                a.Success,
                a.Serial,
                a.TokenType,
                a.User,
                a.Realm,
                a.Resolver,
                a.Administrator,
                a.ActionDetail,
                a.Info,
                a.Client,
                a.Policies,
                a.Duration
            });

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new
                    {
                        count = totalCount,
                        current = page,
                        auditdata = auditDtos
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit log");
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
    /// Get audit statistics
    /// GET /audit/statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? startdate = null,
        [FromQuery] DateTime? enddate = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var query = _context.AuditEntries.AsQueryable();

            if (startdate.HasValue)
                query = query.Where(a => a.Date >= startdate.Value);
            
            if (enddate.HasValue)
                query = query.Where(a => a.Date <= enddate.Value);

            var totalCount = await query.CountAsync();
            var successCount = await query.CountAsync(a => a.Success);
            var failCount = totalCount - successCount;

            var actionStats = await query
                .Where(a => a.Action != null)
                .GroupBy(a => a.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync();

            return Ok(new ApiResponse<object>
            {
                Time = (DateTime.UtcNow - startTime).TotalSeconds,
                Result = new ResultWrapper<object>
                {
                    Status = true,
                    Value = new
                    {
                        total = totalCount,
                        success = successCount,
                        failed = failCount,
                        by_action = actionStats
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit statistics");
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
