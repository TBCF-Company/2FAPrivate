using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Periodic task definition for scheduled operations
/// </summary>
[Table("periodictask")]
public class PeriodicTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    /// Cron-style interval expression
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("interval")]
    public string Interval { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of nodes that can run this task
    /// </summary>
    [Column("nodes")]
    public string? Nodes { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("taskmodule")]
    public string TaskModule { get; set; } = string.Empty;

    [Column("ordering")]
    public int Ordering { get; set; }

    [Column("retry_if_failed")]
    public bool RetryIfFailed { get; set; } = true;

    // Navigation properties
    public ICollection<PeriodicTaskOption> Options { get; set; } = new List<PeriodicTaskOption>();
    public ICollection<PeriodicTaskLastRun> LastRuns { get; set; } = new List<PeriodicTaskLastRun>();
}
