using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Configuration options for a machine resolver
/// </summary>
[Table("machineresolverconfig")]
public class MachineResolverConfig
{
    [Key]
    public int Id { get; set; }

    [Column("machineresolver_id")]
    public int MachineResolverId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [MaxLength(100)]
    [Column("type")]
    public string? Type { get; set; }

    // Navigation property
    [ForeignKey("MachineResolverId")]
    public MachineResolver? MachineResolver { get; set; }
}
