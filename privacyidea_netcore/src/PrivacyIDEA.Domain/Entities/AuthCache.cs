using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Stores authentication cache entries for performance optimization
/// </summary>
[Table("authcache")]
public class AuthCache
{
    [Key]
    public int Id { get; set; }

    [Column("first_auth")]
    public DateTime FirstAuth { get; set; } = DateTime.UtcNow;

    [Column("last_auth")]
    public DateTime? LastAuth { get; set; }

    [MaxLength(64)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("realm")]
    public string? Realm { get; set; }

    [MaxLength(64)]
    [Column("resolver")]
    public string? Resolver { get; set; }

    [MaxLength(40)]
    [Column("client_ip")]
    public string? ClientIp { get; set; }

    [MaxLength(255)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("auth_count")]
    public int AuthCount { get; set; } = 1;
}
