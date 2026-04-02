using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Cache for user data to improve resolver performance
/// </summary>
[Table("usercache")]
public class UserCache
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    [Column("resolver")]
    public string Resolver { get; set; } = string.Empty;

    [MaxLength(320)]
    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON encoded user data
    /// </summary>
    [Column("user_data")]
    public string? UserData { get; set; }
}
