using PrivacyIDEA.Domain.Entities;

namespace PrivacyIDEA.Core.Interfaces;

/// <summary>
/// Interface for Audit Service
/// Maps to Python: privacyidea/lib/audit.py
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task LogAsync(AuditLogRequest request);

    /// <summary>
    /// Get audit entries
    /// </summary>
    Task<PagedResult<AuditEntry>> GetAuditLogAsync(AuditSearchFilter filter, int page = 1, int pageSize = 15);

    /// <summary>
    /// Get audit statistics
    /// </summary>
    Task<AuditStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get last successful authentication for user
    /// </summary>
    Task<AuditEntry?> GetLastAuthAsync(string user, string? realm = null);

    /// <summary>
    /// Count authentications for user
    /// </summary>
    Task<int> CountAuthenticationsAsync(string user, string? realm = null, bool? success = null, DateTime? since = null);

    /// <summary>
    /// Sign an audit entry
    /// </summary>
    Task<string> SignAuditEntryAsync(AuditEntry entry);

    /// <summary>
    /// Verify audit entry signature
    /// </summary>
    Task<bool> VerifySignatureAsync(AuditEntry entry);

    /// <summary>
    /// Delete old audit entries
    /// </summary>
    Task<int> CleanupAsync(DateTime olderThan);
}

/// <summary>
/// Audit log request
/// </summary>
public class AuditLogRequest
{
    public string? Action { get; set; }
    public bool Success { get; set; }
    public string? Serial { get; set; }
    public string? TokenType { get; set; }
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Resolver { get; set; }
    public string? Administrator { get; set; }
    public string? ActionDetail { get; set; }
    public string? Info { get; set; }
    public string? Client { get; set; }
    public string? Policies { get; set; }
    public DateTime? StartDate { get; set; }
}

/// <summary>
/// Audit search filter
/// </summary>
public class AuditSearchFilter
{
    public string? Serial { get; set; }
    public string? User { get; set; }
    public string? Realm { get; set; }
    public string? Action { get; set; }
    public bool? Success { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Client { get; set; }
    public string SortOrder { get; set; } = "desc";
}

/// <summary>
/// Audit statistics
/// </summary>
public class AuditStatistics
{
    public int TotalEntries { get; set; }
    public int SuccessfulAuthentications { get; set; }
    public int FailedAuthentications { get; set; }
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByUser { get; set; } = new();
    public Dictionary<string, int> ByRealm { get; set; } = new();
}
