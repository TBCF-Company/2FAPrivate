using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// PrivacyIDEA server connections for federation
/// </summary>
[Table("privacyideaserver")]
public class PrivacyIDEAServer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("tls")]
    public bool Tls { get; set; } = true;
}
