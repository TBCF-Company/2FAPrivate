using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Admin user entity
/// Maps to Python: privacyidea/models - Admin class
/// </summary>
[Table("admin")]
public class Admin
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    [Required]
    [MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Column("password")]
    [Required]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;
}
