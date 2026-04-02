using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Configuration for CA connector
/// </summary>
[Table("caconnectorconfig")]
public class CAConnectorConfig
{
    [Key]
    public int Id { get; set; }

    [Column("caconnector_id")]
    public int CAConnectorId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [MaxLength(100)]
    [Column("type")]
    public string? Type { get; set; }

    [MaxLength(2000)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation property
    [ForeignKey("CAConnectorId")]
    public CAConnector? CAConnector { get; set; }
}
