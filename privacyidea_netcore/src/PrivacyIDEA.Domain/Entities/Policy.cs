using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Policy entity
/// Maps to Python: privacyidea/models/policy.py - Policy class
/// </summary>
[Table("policy")]
public class Policy
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("check_all_resolvers")]
    public bool CheckAllResolvers { get; set; } = false;

    [Column("name")]
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Column("scope")]
    [Required]
    [MaxLength(32)]
    public string Scope { get; set; } = string.Empty;

    [Column("action")]
    public string? Action { get; set; }

    [Column("realm")]
    public string? Realm { get; set; }

    [Column("adminrealm")]
    public string? AdminRealm { get; set; }

    [Column("adminuser")]
    public string? AdminUser { get; set; }

    [Column("resolver")]
    public string? Resolver { get; set; }

    [Column("pinode")]
    public string? PiNode { get; set; }

    [Column("user")]
    public string? User { get; set; }

    [Column("client")]
    public string? Client { get; set; }

    [Column("time")]
    public string? Time { get; set; }

    [Column("priority")]
    public int Priority { get; set; } = 1;

    // Navigation properties
    public virtual ICollection<PolicyCondition> Conditions { get; set; } = new List<PolicyCondition>();
}
