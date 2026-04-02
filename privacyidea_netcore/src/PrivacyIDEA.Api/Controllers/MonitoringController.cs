using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrivacyIDEA.Core.Interfaces;

namespace PrivacyIDEA.Api.Controllers;

/// <summary>
/// Monitoring and statistics API
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(IMonitoringService monitoringService, ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    /// <summary>
    /// Get token statistics
    /// </summary>
    [HttpGet("token")]
    public async Task<IActionResult> GetTokenStats()
    {
        var stats = await _monitoringService.GetTokenStatisticsAsync();
        return Ok(new
        {
            result = new { value = stats },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get authentication statistics
    /// </summary>
    [HttpGet("auth")]
    public async Task<IActionResult> GetAuthStats([FromQuery] DateTime? since = null)
    {
        var stats = await _monitoringService.GetAuthStatisticsAsync(since);
        return Ok(new
        {
            result = new { value = stats },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get all event counters
    /// </summary>
    [HttpGet("eventcounter")]
    public async Task<IActionResult> GetEventCounters()
    {
        var counters = await _monitoringService.GetAllEventCountersAsync();
        return Ok(new
        {
            result = new { value = counters.ToList() },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get a specific event counter
    /// </summary>
    [HttpGet("eventcounter/{name}")]
    public async Task<IActionResult> GetEventCounter(string name)
    {
        var value = await _monitoringService.GetEventCounterAsync(name);
        return Ok(new
        {
            result = new { value = new { name, counter_value = value } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Increment an event counter
    /// </summary>
    [HttpPost("eventcounter/{name}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> IncrementCounter(string name, [FromQuery] int amount = 1)
    {
        var newValue = await _monitoringService.IncrementEventCounterAsync(name, amount);
        return Ok(new
        {
            result = new { status = true, value = new { name, counter_value = newValue } },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Reset an event counter
    /// </summary>
    [HttpDelete("eventcounter/{name}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ResetCounter(string name)
    {
        var reset = await _monitoringService.ResetEventCounterAsync(name);
        if (!reset)
            return NotFound(new { result = new { status = false }, detail = $"Counter '{name}' not found" });

        _logger.LogInformation("Event counter reset: {Name}", name);

        return Ok(new
        {
            result = new { status = true, value = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get monitoring stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string? key = null, [FromQuery] DateTime? since = null)
    {
        var stats = await _monitoringService.GetStatsAsync(key, since);
        return Ok(new
        {
            result = new { value = stats.ToList() },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Record a monitoring stat
    /// </summary>
    [HttpPost("stats")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> RecordStat([FromBody] RecordStatRequest request)
    {
        await _monitoringService.RecordStatAsync(request.Key, request.Value, request.Node);
        return Ok(new
        {
            result = new { status = true },
            version = "1.0",
            id = 1
        });
    }

    /// <summary>
    /// Get system health
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth()
    {
        var health = await _monitoringService.GetSystemHealthAsync();
        return Ok(new
        {
            result = new { value = health },
            version = "1.0",
            id = 1
        });
    }
}

public class RecordStatRequest
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
    public string? Node { get; set; }
}
