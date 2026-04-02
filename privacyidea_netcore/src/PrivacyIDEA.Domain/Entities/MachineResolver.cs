using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Machine resolver definition for machine authentication
/// </summary>
[Table("machineresolver")]
public class MachineResolver
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("rtype")]
    public string ResolverType { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<MachineResolverConfig> Configs { get; set; } = new List<MachineResolverConfig>();
    public ICollection<MachineToken> MachineTokens { get; set; } = new List<MachineToken>();
}
