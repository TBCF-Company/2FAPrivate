using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// State history for token containers
/// </summary>
[Table("tokencontainerstates")]
public class TokenContainerState
{
    [Key]
    public int Id { get; set; }

    [Column("container_id")]
    public int ContainerId { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("state")]
    public string State { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("ContainerId")]
    public TokenContainer? Container { get; set; }
}
