using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Records of periodic task executions
/// </summary>
[Table("periodictasklastrun")]
public class PeriodicTaskLastRun
{
    [Key]
    public int Id { get; set; }

    [Column("periodictask_id")]
    public int PeriodicTaskId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("node")]
    public string Node { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("success")]
    public bool? Success { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    [Column("duration")]
    public decimal? Duration { get; set; }

    [Column("result")]
    public string? Result { get; set; }

    // Navigation property
    [ForeignKey("PeriodicTaskId")]
    public PeriodicTask? PeriodicTask { get; set; }
}
