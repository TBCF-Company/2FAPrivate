using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Association between tokens and machines
/// </summary>
[Table("machinetoken")]
public class MachineToken
{
    [Key]
    public int Id { get; set; }

    [Column("token_id")]
    public int TokenId { get; set; }

    [Column("machineresolver_id")]
    public int? MachineResolverId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("machine_id")]
    public string MachineId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [Column("application")]
    public string Application { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("TokenId")]
    public Token? Token { get; set; }

    [ForeignKey("MachineResolverId")]
    public MachineResolver? MachineResolver { get; set; }

    public ICollection<MachineTokenOption> Options { get; set; } = new List<MachineTokenOption>();
}
