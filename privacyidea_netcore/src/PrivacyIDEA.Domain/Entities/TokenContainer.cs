using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Container for grouping related tokens
/// </summary>
[Table("tokencontainer")]
public class TokenContainer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("serial")]
    public string Serial { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [MaxLength(80)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<TokenContainerOwner> Owners { get; set; } = new List<TokenContainerOwner>();
    public ICollection<TokenContainerInfo> Infos { get; set; } = new List<TokenContainerInfo>();
    public ICollection<TokenContainerRealm> Realms { get; set; } = new List<TokenContainerRealm>();
    public ICollection<TokenContainerState> States { get; set; } = new List<TokenContainerState>();
    public ICollection<TokenContainerToken> Tokens { get; set; } = new List<TokenContainerToken>();
}
