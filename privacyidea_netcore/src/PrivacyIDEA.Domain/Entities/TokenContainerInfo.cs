using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Metadata for token containers
/// </summary>
[Table("tokencontainerinfo")]
public class TokenContainerInfo
{
    [Key]
    public int Id { get; set; }

    [Column("container_id")]
    public int ContainerId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    // Navigation property
    [ForeignKey("ContainerId")]
    public TokenContainer? Container { get; set; }
}
