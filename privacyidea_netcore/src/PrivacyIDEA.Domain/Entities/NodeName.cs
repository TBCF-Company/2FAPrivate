using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Node identification for clustered deployments
/// </summary>
[Table("nodename")]
public class NodeName
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("last_seen")]
    public DateTime? LastSeen { get; set; }
}
