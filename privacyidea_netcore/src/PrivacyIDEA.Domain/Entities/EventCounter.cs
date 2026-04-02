using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Counter for tracking events (used by CounterEventHandler)
/// </summary>
[Table("eventcounter")]
public class EventCounter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    [Column("counter_name")]
    public string CounterName { get; set; } = string.Empty;

    [Column("counter_value")]
    public int CounterValue { get; set; }

    [Column("reset_date")]
    public DateTime? ResetDate { get; set; }

    [MaxLength(255)]
    [Column("node")]
    public string? Node { get; set; }
}
