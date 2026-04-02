using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Monitoring statistics for tracking system metrics
/// </summary>
[Table("monitoringstats")]
public class MonitoringStats
{
    [Key]
    public int Id { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(255)]
    [Column("stats_key")]
    public string StatsKey { get; set; } = string.Empty;

    [Column("stats_value")]
    public int? StatsValue { get; set; }

    [MaxLength(255)]
    [Column("node")]
    public string? Node { get; set; }
}
