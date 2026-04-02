using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Realm entity - groups resolvers together
/// Maps to Python: privacyidea/models/realm.py - Realm class
/// </summary>
[Table("realm")]
public class Realm
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("default")]
    public bool IsDefault { get; set; } = false;

    [Column("option")]
    [MaxLength(40)]
    public string? Option { get; set; }

    // Navigation properties
    public virtual ICollection<ResolverRealm> ResolverRealms { get; set; } = new List<ResolverRealm>();
    public virtual ICollection<TokenRealm> TokenRealms { get; set; } = new List<TokenRealm>();
}
