using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Tracks client applications connecting to PrivacyIDEA
/// </summary>
[Table("clientapplication")]
public class ClientApplication
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("ip")]
    public string IP { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("hostname")]
    public string? Hostname { get; set; }

    [MaxLength(30)]
    [Column("clienttype")]
    public string? ClientType { get; set; }

    [Column("lastseen")]
    public DateTime? LastSeen { get; set; }

    [MaxLength(255)]
    [Column("node")]
    public string? Node { get; set; }
}
