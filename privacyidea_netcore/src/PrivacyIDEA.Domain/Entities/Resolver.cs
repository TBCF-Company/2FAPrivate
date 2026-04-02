using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Resolver entity - defines user sources
/// Maps to Python: privacyidea/models/resolver.py - Resolver class
/// </summary>
[Table("resolver")]
public class Resolver
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("rtype")]
    [Required]
    [MaxLength(255)]
    public string Type { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<ResolverConfig> Configs { get; set; } = new List<ResolverConfig>();
    public virtual ICollection<ResolverRealm> ResolverRealms { get; set; } = new List<ResolverRealm>();
}
