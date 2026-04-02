using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token-Realm relationship
/// Maps to Python: privacyidea/models/token.py - TokenRealm class
/// </summary>
[Table("tokenrealm")]
public class TokenRealm
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("token_id")]
    public int TokenId { get; set; }

    [Column("realm_id")]
    public int RealmId { get; set; }

    [ForeignKey(nameof(TokenId))]
    public virtual Token? Token { get; set; }

    [ForeignKey(nameof(RealmId))]
    public virtual Realm? Realm { get; set; }
}
