using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Owner of a token container
/// </summary>
[Table("tokencontainerowner")]
public class TokenContainerOwner
{
    [Key]
    public int Id { get; set; }

    [Column("container_id")]
    public int ContainerId { get; set; }

    [Column("resolver_id")]
    public int? ResolverId { get; set; }

    [Column("realm_id")]
    public int? RealmId { get; set; }

    [Required]
    [MaxLength(320)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("ContainerId")]
    public TokenContainer? Container { get; set; }

    [ForeignKey("ResolverId")]
    public Resolver? Resolver { get; set; }

    [ForeignKey("RealmId")]
    public Realm? Realm { get; set; }
}
