using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Policy condition entity
/// Maps to Python: privacyidea/models/policy.py - PolicyCondition class
/// </summary>
[Table("policycondition")]
public class PolicyCondition
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("policy_id")]
    public int PolicyId { get; set; }

    [Column("section")]
    [Required]
    [MaxLength(255)]
    public string Section { get; set; } = string.Empty;

    [Column("key")]
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    [Column("comparator")]
    [Required]
    [MaxLength(255)]
    public string Comparator { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [ForeignKey(nameof(PolicyId))]
    public virtual Policy? Policy { get; set; }
}
