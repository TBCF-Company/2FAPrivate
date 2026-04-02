using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// RADIUS Server configuration
/// Maps to Python: privacyidea/models/server.py - RADIUSServer class
/// </summary>
[Table("radiusserver")]
public class RadiusServer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("identifier")]
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    [Column("server")]
    [Required]
    [MaxLength(255)]
    public string Server { get; set; } = string.Empty;

    [Column("port")]
    public int Port { get; set; } = 1812;

    [Column("secret")]
    [Required]
    [MaxLength(255)]
    public string Secret { get; set; } = string.Empty;

    [Column("timeout")]
    public int Timeout { get; set; } = 5;

    [Column("retries")]
    public int Retries { get; set; } = 3;

    [Column("description")]
    [MaxLength(1024)]
    public string? Description { get; set; }
}
