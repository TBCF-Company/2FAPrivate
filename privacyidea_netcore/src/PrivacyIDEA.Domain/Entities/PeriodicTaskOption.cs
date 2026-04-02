using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Options for periodic tasks
/// </summary>
[Table("periodictaskoption")]
public class PeriodicTaskOption
{
    [Key]
    public int Id { get; set; }

    [Column("periodictask_id")]
    public int PeriodicTaskId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    // Navigation property
    [ForeignKey("PeriodicTaskId")]
    public PeriodicTask? PeriodicTask { get; set; }
}
