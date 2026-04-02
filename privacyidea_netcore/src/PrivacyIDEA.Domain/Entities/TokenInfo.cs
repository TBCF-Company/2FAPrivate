using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token additional information
/// Maps to Python: privacyidea/models/token.py - TokenInfo class
/// </summary>
[Table("tokeninfo")]
public class TokenInfo
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

    [Column("token_id")]
    public int TokenId { get; set; }

    [ForeignKey(nameof(TokenId))]
    public virtual Token? Token { get; set; }
}
