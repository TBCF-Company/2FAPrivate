using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Resolver-Realm relationship
/// Maps to Python: privacyidea/models/resolver.py - ResolverRealm class
/// </summary>
[Table("resolverrealm")]
public class ResolverRealm
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("resolver_id")]
    public int ResolverId { get; set; }

    [Column("realm_id")]
    public int RealmId { get; set; }

    [Column("priority")]
    public int? Priority { get; set; }

    [ForeignKey(nameof(ResolverId))]
    public virtual Resolver? Resolver { get; set; }

    [ForeignKey(nameof(RealmId))]
    public virtual Realm? Realm { get; set; }
}
