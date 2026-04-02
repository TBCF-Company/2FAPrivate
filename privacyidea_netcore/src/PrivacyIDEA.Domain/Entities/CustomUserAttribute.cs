using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Custom user attributes for extended user information
/// </summary>
[Table("customuserattribute")]
public class CustomUserAttribute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(320)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [Column("resolver")]
    public string Resolver { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("realm")]
    public string? Realm { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [MaxLength(100)]
    [Column("type")]
    public string? Type { get; set; }
}
