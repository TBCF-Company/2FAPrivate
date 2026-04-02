using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Associates tokens with token groups
/// </summary>
[Table("tokentokengroup")]
public class TokenTokenGroup
{
    [Key]
    public int Id { get; set; }

    [Column("token_id")]
    public int TokenId { get; set; }

    [Column("tokengroup_id")]
    public int TokenGroupId { get; set; }

    // Navigation properties
    [ForeignKey("TokenId")]
    public Token? Token { get; set; }

    [ForeignKey("TokenGroupId")]
    public TokenGroup? TokenGroup { get; set; }
}
