using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Password reset tokens for self-service recovery
/// </summary>
[Table("passwordreset")]
public class PasswordReset
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("recoverycode")]
    public string RecoveryCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("realm")]
    public string? Realm { get; set; }

    [MaxLength(64)]
    [Column("resolver")]
    public string? Resolver { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("expiration")]
    public DateTime? Expiration { get; set; }
}
