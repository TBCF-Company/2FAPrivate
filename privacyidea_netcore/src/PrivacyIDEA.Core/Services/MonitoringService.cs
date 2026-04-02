using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrivacyIDEA.Core.Interfaces;
using PrivacyIDEA.Domain.Entities;
using System.Diagnostics;

namespace PrivacyIDEA.Core.Services;

/// <summary>
/// Service for system monitoring and statistics
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(IUnitOfWork unitOfWork, ILogger<MonitoringService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MonitoringTokenStatistics> GetTokenStatisticsAsync()
    {
        var tokens = await _unitOfWork.Query<Token>()
            .Include(t => t.TokenRealms).ThenInclude(tr => tr.Realm)
            .ToListAsync();

        var stats = new MonitoringTokenStatistics
        {
            TotalTokens = tokens.Count,
            ActiveTokens = tokens.Count(t => t.Active && !t.Revoked && !t.Locked),
            DisabledTokens = tokens.Count(t => !t.Active),
            RevokedTokens = tokens.Count(t => t.Revoked),
            LockedTokens = tokens.Count(t => t.Locked),
            UnassignedTokens = tokens.Count(t => !t.TokenOwners.Any())
        };

        // By type
        stats.ByType = tokens
            .GroupBy(t => t.TokenType)
            .ToDictionary(g => g.Key, g => g.Count());

        // By realm
        stats.ByRealm = tokens
            .SelectMany(t => t.TokenRealms.Select(tr => new { Token = t, Realm = tr.Realm?.Name ?? "default" }))
            .GroupBy(x => x.Realm)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<AuthStatistics> GetAuthStatisticsAsync(DateTime? since = null)
    {
        var query = _unitOfWork.Query<AuditEntry>()
            .Where(a => a.Action == "validate/check" || a.Action == "authenticate");

        if (since.HasValue)
        {
            query = query.Where(a => a.Date >= since.Value);
        }

        var audits = await query.ToListAsync();

        var stats = new AuthStatistics
        {
            TotalAuthentications = audits.Count,
            SuccessfulAuthentications = audits.Count(a => a.Success == true),
            FailedAuthentications = audits.Count(a => a.Success == false)
        };

        stats.SuccessRate = stats.TotalAuthentications > 0
            ? (double)stats.SuccessfulAuthentications / stats.TotalAuthentications * 100
            : 0;

        // By realm
        stats.ByRealm = audits
            .Where(a => !string.IsNullOrEmpty(a.Realm))
            .GroupBy(a => a.Realm!)
            .ToDictionary(g => g.Key, g => g.Count());

        // By token type
        stats.ByTokenType = audits
            .Where(a => !string.IsNullOrEmpty(a.TokenType))
            .GroupBy(a => a.TokenType!)
            .ToDictionary(g => g.Key, g => g.Count());

        // By hour
        stats.ByHour = audits
            .GroupBy(a => a.Date.Hour.ToString("D2"))
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<int> GetEventCounterAsync(string counterName)
    {
        var counter = await _unitOfWork.Query<EventCounter>()
            .FirstOrDefaultAsync(c => c.CounterName == counterName);

        return counter?.CounterValue ?? 0;
    }

    public async Task<int> IncrementEventCounterAsync(string counterName, int amount = 1)
    {
        var counter = await _unitOfWork.Query<EventCounter>()
            .FirstOrDefaultAsync(c => c.CounterName == counterName);

        if (counter == null)
        {
            counter = new EventCounter
            {
                CounterName = counterName,
                CounterValue = amount,
                Node = Environment.MachineName
            };
            _unitOfWork.Add(counter);
        }
        else
        {
            counter.CounterValue += amount;
        }

        await _unitOfWork.SaveChangesAsync();
        return counter.CounterValue;
    }

    public async Task<bool> ResetEventCounterAsync(string counterName)
    {
        var counter = await _unitOfWork.Query<EventCounter>()
            .FirstOrDefaultAsync(c => c.CounterName == counterName);

        if (counter == null)
            return false;

        counter.CounterValue = 0;
        counter.ResetDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Reset event counter: {Name}", counterName);
        return true;
    }

    public async Task<IEnumerable<EventCounterInfo>> GetAllEventCountersAsync()
    {
        var counters = await _unitOfWork.Query<EventCounter>().ToListAsync();

        return counters.Select(c => new EventCounterInfo
        {
            Name = c.CounterName,
            Value = c.CounterValue,
            ResetDate = c.ResetDate,
            Node = c.Node
        });
    }

    public async Task RecordStatAsync(string key, int value, string? node = null)
    {
        var stat = new MonitoringStats
        {
            StatsKey = key,
            StatsValue = value,
            Timestamp = DateTime.UtcNow,
            Node = node ?? Environment.MachineName
        };

        _unitOfWork.Add(stat);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<MonitoringStatInfo>> GetStatsAsync(string? key = null, DateTime? since = null)
    {
        var query = _unitOfWork.Query<MonitoringStats>().AsQueryable();

        if (!string.IsNullOrEmpty(key))
        {
            query = query.Where(s => s.StatsKey == key);
        }

        if (since.HasValue)
        {
            query = query.Where(s => s.Timestamp >= since.Value);
        }

        var stats = await query
            .OrderByDescending(s => s.Timestamp)
            .Take(1000)
            .ToListAsync();

        return stats.Select(s => new MonitoringStatInfo
        {
            Key = s.StatsKey,
            Value = s.StatsValue ?? 0,
            Timestamp = s.Timestamp,
            Node = s.Node
        });
    }

    public async Task<SystemHealthInfo> GetSystemHealthAsync()
    {
        var health = new SystemHealthInfo
        {
            Timestamp = DateTime.UtcNow
        };

        // Check database
        var dbStopwatch = Stopwatch.StartNew();
        try
        {
            await _unitOfWork.Query<Config>().FirstOrDefaultAsync();
            dbStopwatch.Stop();
            
            health.Database = new DatabaseHealthInfo
            {
                IsConnected = true,
                Provider = "EntityFramework",
                ResponseTimeMs = (int)dbStopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            health.Database = new DatabaseHealthInfo
            {
                IsConnected = false,
                Provider = "EntityFramework"
            };
            health.Components["database_error"] = new ComponentHealthInfo
            {
                IsHealthy = false,
                Message = ex.Message
            };
        }

        // Check token service
        try
        {
            var tokenCount = await _unitOfWork.Query<Token>().CountAsync();
            health.Components["tokens"] = new ComponentHealthInfo
            {
                IsHealthy = true,
                Message = $"{tokenCount} tokens"
            };
        }
        catch
        {
            health.Components["tokens"] = new ComponentHealthInfo
            {
                IsHealthy = false,
                Message = "Unable to count tokens"
            };
        }

        // Overall health
        health.IsHealthy = health.Database.IsConnected && 
                          health.Components.Values.All(c => c.IsHealthy);
        health.Status = health.IsHealthy ? "healthy" : "unhealthy";

        return health;
    }
}
