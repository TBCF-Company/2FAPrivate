using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Options for machine token association
/// </summary>
[Table("machinetokenoption")]
public class MachineTokenOption
{
    [Key]
    public int Id { get; set; }

    [Column("machinetoken_id")]
    public int MachineTokenId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    // Navigation property
    [ForeignKey("MachineTokenId")]
    public MachineToken? MachineToken { get; set; }
}
