using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Resolver configuration
/// Maps to Python: privacyidea/models/resolver.py - ResolverConfig class
/// </summary>
[Table("resolverconfig")]
public class ResolverConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("resolver_id")]
    public int ResolverId { get; set; }

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

    [ForeignKey(nameof(ResolverId))]
    public virtual Resolver? Resolver { get; set; }
}
