using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Certificate Authority connector definition
/// </summary>
[Table("caconnector")]
public class CAConnector
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("catype")]
    public string CAType { get; set; } = string.Empty;

    // Navigation property
    public ICollection<CAConnectorConfig> Configs { get; set; } = new List<CAConnectorConfig>();
}
