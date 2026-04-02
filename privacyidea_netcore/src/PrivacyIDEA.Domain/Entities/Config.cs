using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// System configuration entity
/// Maps to Python: privacyidea/models/config.py - Config class
/// </summary>
[Table("config")]
public class Config
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("key")]
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("type")]
    [MaxLength(100)]
    public string? Type { get; set; }

    [Column("description")]
    [MaxLength(2000)]
    public string? Description { get; set; }
}
