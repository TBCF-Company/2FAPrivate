using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token group definition
/// </summary>
[Table("tokengroup")]
public class TokenGroup
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    [Column("description")]
    public string? Description { get; set; }

    // Navigation property
    public ICollection<TokenTokenGroup> TokenAssociations { get; set; } = new List<TokenTokenGroup>();
}
