namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Service for system monitoring and statistics
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Get token statistics
    /// </summary>
    Task<MonitoringTokenStatistics> GetTokenStatisticsAsync();

    /// <summary>
    /// Get authentication statistics
    /// </summary>
    Task<AuthStatistics> GetAuthStatisticsAsync(DateTime? since = null);

    /// <summary>
    /// Get event counter value
    /// </summary>
    Task<int> GetEventCounterAsync(string counterName);

    /// <summary>
    /// Increment event counter
    /// </summary>
    Task<int> IncrementEventCounterAsync(string counterName, int amount = 1);

    /// <summary>
    /// Reset event counter
    /// </summary>
    Task<bool> ResetEventCounterAsync(string counterName);

    /// <summary>
    /// Get all event counters
    /// </summary>
    Task<IEnumerable<EventCounterInfo>> GetAllEventCountersAsync();

    /// <summary>
    /// Record monitoring stat
    /// </summary>
    Task RecordStatAsync(string key, int value, string? node = null);

    /// <summary>
    /// Get monitoring stats
    /// </summary>
    Task<IEnumerable<MonitoringStatInfo>> GetStatsAsync(string? key = null, DateTime? since = null);

    /// <summary>
    /// Get system health status
    /// </summary>
    Task<SystemHealthInfo> GetSystemHealthAsync();
}

public class MonitoringTokenStatistics
{
    public int TotalTokens { get; set; }
    public int ActiveTokens { get; set; }
    public int DisabledTokens { get; set; }
    public int RevokedTokens { get; set; }
    public int LockedTokens { get; set; }
    public int UnassignedTokens { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByRealm { get; set; } = new();
}

public class AuthStatistics
{
    public int TotalAuthentications { get; set; }
    public int SuccessfulAuthentications { get; set; }
    public int FailedAuthentications { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, int> ByRealm { get; set; } = new();
    public Dictionary<string, int> ByTokenType { get; set; } = new();
    public Dictionary<string, int> ByHour { get; set; } = new();
}

public class EventCounterInfo
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime? ResetDate { get; set; }
    public string? Node { get; set; }
}

public class MonitoringStatInfo
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Node { get; set; }
}

public class SystemHealthInfo
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "unknown";
    public DateTime Timestamp { get; set; }
    public DatabaseHealthInfo Database { get; set; } = new();
    public Dictionary<string, ComponentHealthInfo> Components { get; set; } = new();
}

public class DatabaseHealthInfo
{
    public bool IsConnected { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int? ResponseTimeMs { get; set; }
}

public class ComponentHealthInfo
{
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
}
