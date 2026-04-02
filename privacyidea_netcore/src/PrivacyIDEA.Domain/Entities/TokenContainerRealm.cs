using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Associates token containers with realms
/// </summary>
[Table("tokencontainerrealm")]
public class TokenContainerRealm
{
    [Key]
    public int Id { get; set; }

    [Column("container_id")]
    public int ContainerId { get; set; }

    [Column("realm_id")]
    public int? RealmId { get; set; }

    // Navigation properties
    [ForeignKey("ContainerId")]
    public TokenContainer? Container { get; set; }

    [ForeignKey("RealmId")]
    public Realm? Realm { get; set; }
}
