using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Periodic Tasks API (Token Janitor, Cleanup)
/// Maps to Python: privacyidea/api/periodictasks.py
/// </summary>
[ApiController]
[Route("periodictask")]
[Authorize(Policy = "Admin")]
public class PeriodicTaskController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PeriodicTaskController> _logger;

    public PeriodicTaskController(
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<PeriodicTaskController> logger)
    {
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /periodictask/ - List all periodic tasks
    /// </summary>
    [HttpGet]
    public IActionResult ListTasks()
    {
        var tasks = new List<object>
        {
            new
            {
                id = "token_cleanup",
                name = "Token Cleanup",
                description = "Remove expired and orphaned tokens",
                interval = "0 0 * * *",  // Daily at midnight
                active = true,
                last_run = (DateTime?)null,
                next_run = (DateTime?)DateTime.UtcNow.Date.AddDays(1)
            },
            new
            {
                id = "audit_cleanup",
                name = "Audit Log Cleanup",
                description = "Remove old audit log entries",
                interval = "0 1 * * 0",  // Weekly on Sunday at 1 AM
                active = true,
                last_run = (DateTime?)null,
                next_run = (DateTime?)GetNextSunday()
            },
            new
            {
                id = "eventcounter_reset",
                name = "Event Counter Reset",
                description = "Reset event counters",
                interval = "0 0 1 * *",  // Monthly on 1st
                active = true,
                last_run = (DateTime?)null,
                next_run = (DateTime?)GetNextMonthStart()
            },
            new
            {
                id = "challenge_cleanup",
                name = "Challenge Cleanup",
                description = "Remove expired challenges",
                interval = "*/5 * * * *",  // Every 5 minutes
                active = true,
                last_run = (DateTime?)DateTime.UtcNow.AddMinutes(-3),
                next_run = (DateTime?)DateTime.UtcNow.AddMinutes(2)
            }
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = tasks
            }
        });
    }

    /// <summary>
    /// POST /periodictask/run/{taskId} - Run a specific task
    /// </summary>
    [HttpPost("run/{taskId}")]
    public async Task<IActionResult> RunTask(string taskId)
    {
        var result = new Dictionary<string, object>();

        switch (taskId)
        {
            case "token_cleanup":
                result = await RunTokenCleanupAsync();
                break;
            case "audit_cleanup":
                result = await RunAuditCleanupAsync();
                break;
            case "eventcounter_reset":
                result = await RunEventCounterResetAsync();
                break;
            case "challenge_cleanup":
                result = await RunChallengeCleanupAsync();
                break;
            default:
                return BadRequest(new
                {
                    jsonrpc = "2.0",
                    result = new
                    {
                        status = false,
                        error = new { message = $"Unknown task: {taskId}" }
                    }
                });
        }

        await _auditService.LogAsync("periodictask_run", true, User.Identity?.Name ?? "admin",
            null, null, null, $"Ran periodic task: {taskId}");

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
    /// POST /periodictask/enable/{taskId} - Enable a task
    /// </summary>
    [HttpPost("enable/{taskId}")]
    public IActionResult EnableTask(string taskId)
    {
        // In production, this would update database
        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new { task_id = taskId, enabled = true }
            }
        });
    }

    /// <summary>
    /// POST /periodictask/disable/{taskId} - Disable a task
    /// </summary>
    [HttpPost("disable/{taskId}")]
    public IActionResult DisableTask(string taskId)
    {
        return Ok(new
        {
            jsonrpc = "2.0",
            result = new
            {
                status = true,
                value = new { task_id = taskId, enabled = false }
            }
        });
    }

    private async Task<Dictionary<string, object>> RunTokenCleanupAsync()
    {
        var stats = await _tokenService.GetStatisticsAsync();
        // In production, would actually delete expired tokens
        _logger.LogInformation("Running token cleanup task");
        
        return new Dictionary<string, object>
        {
            ["task"] = "token_cleanup",
            ["tokens_checked"] = stats.TotalTokens,
            ["tokens_deleted"] = 0,  // Placeholder
            ["completed_at"] = DateTime.UtcNow
        };
    }

    private Task<Dictionary<string, object>> RunAuditCleanupAsync()
    {
        // In production, would delete old audit entries
        _logger.LogInformation("Running audit cleanup task");
        
        return Task.FromResult(new Dictionary<string, object>
        {
            ["task"] = "audit_cleanup",
            ["entries_deleted"] = 0,
            ["completed_at"] = DateTime.UtcNow
        });
    }

    private Task<Dictionary<string, object>> RunEventCounterResetAsync()
    {
        _logger.LogInformation("Running event counter reset task");
        
        return Task.FromResult(new Dictionary<string, object>
        {
            ["task"] = "eventcounter_reset",
            ["counters_reset"] = 0,
            ["completed_at"] = DateTime.UtcNow
        });
    }

    private Task<Dictionary<string, object>> RunChallengeCleanupAsync()
    {
        _logger.LogInformation("Running challenge cleanup task");
        
        return Task.FromResult(new Dictionary<string, object>
        {
            ["task"] = "challenge_cleanup",
            ["challenges_deleted"] = 0,
            ["completed_at"] = DateTime.UtcNow
        });
    }

    private static DateTime GetNextSunday()
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7;
        return today.AddDays(daysUntilSunday).AddHours(1);
    }

    private static DateTime GetNextMonthStart()
    {
        var today = DateTime.UtcNow.Date;
        return new DateTime(today.Year, today.Month, 1).AddMonths(1);
    }
}
