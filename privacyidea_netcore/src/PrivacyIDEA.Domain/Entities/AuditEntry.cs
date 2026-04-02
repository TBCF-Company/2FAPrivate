using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Audit log entry
/// Maps to Python: privacyidea/models/audit.py - Audit class
/// </summary>
[Table("pidea_audit")]
public class AuditEntry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("signature")]
    public string? Signature { get; set; }

    [Column("action")]
    [MaxLength(50)]
    public string? Action { get; set; }

    [Column("success")]
    public bool Success { get; set; } = false;

    [Column("serial")]
    [MaxLength(40)]
    public string? Serial { get; set; }

    [Column("token_type")]
    [MaxLength(100)]
    public string? TokenType { get; set; }

    [Column("user")]
    [MaxLength(255)]
    public string? User { get; set; }

    [Column("realm")]
    [MaxLength(255)]
    public string? Realm { get; set; }

    [Column("resolver")]
    [MaxLength(255)]
    public string? Resolver { get; set; }

    [Column("administrator")]
    [MaxLength(255)]
    public string? Administrator { get; set; }

    [Column("action_detail")]
    public string? ActionDetail { get; set; }

    [Column("info")]
    public string? Info { get; set; }

    [Column("privacyidea_server")]
    [MaxLength(255)]
    public string? PrivacyIdeaServer { get; set; }

    [Column("client")]
    [MaxLength(50)]
    public string? Client { get; set; }

    [Column("loglevel")]
    [MaxLength(12)]
    public string? LogLevel { get; set; }

    [Column("clearance_level")]
    [MaxLength(12)]
    public string? ClearanceLevel { get; set; }

    [Column("policies")]
    public string? Policies { get; set; }

    [Column("startdate")]
    public DateTime? StartDate { get; set; }

    [Column("duration")]
    public double? Duration { get; set; }

    [Column("thread_id")]
    [MaxLength(32)]
    public string? ThreadId { get; set; }
}
