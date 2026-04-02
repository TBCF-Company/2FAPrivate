using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token owner relationship
/// Maps to Python: privacyidea/models/token.py - TokenOwner class
/// </summary>
[Table("tokenowner")]
public class TokenOwner
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("token_id")]
    public int TokenId { get; set; }

    [Column("resolver")]
    [MaxLength(120)]
    public string? Resolver { get; set; }

    [Column("user_id")]
    [MaxLength(320)]
    public string? UserId { get; set; }

    [Column("realm_id")]
    public int? RealmId { get; set; }

    [ForeignKey(nameof(TokenId))]
    public virtual Token? Token { get; set; }

    [ForeignKey(nameof(RealmId))]
    public virtual Realm? Realm { get; set; }
}
