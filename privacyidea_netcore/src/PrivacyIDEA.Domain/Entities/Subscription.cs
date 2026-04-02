using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Subscription information for PrivacyIDEA licensing
/// </summary>
[Table("subscription")]
public class Subscription
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    [Column("application")]
    public string? Application { get; set; }

    // "For" information (subscriber)
    [MaxLength(100)]
    [Column("for_name")]
    public string? ForName { get; set; }

    [Column("for_address")]
    public string? ForAddress { get; set; }

    [MaxLength(255)]
    [Column("for_email")]
    public string? ForEmail { get; set; }

    [MaxLength(50)]
    [Column("for_phone")]
    public string? ForPhone { get; set; }

    [MaxLength(255)]
    [Column("for_url")]
    public string? ForUrl { get; set; }

    [Column("for_comment")]
    public string? ForComment { get; set; }

    // "By" information (vendor)
    [MaxLength(100)]
    [Column("by_name")]
    public string? ByName { get; set; }

    [MaxLength(255)]
    [Column("by_email")]
    public string? ByEmail { get; set; }

    [Column("by_address")]
    public string? ByAddress { get; set; }

    [MaxLength(50)]
    [Column("by_phone")]
    public string? ByPhone { get; set; }

    [MaxLength(255)]
    [Column("by_url")]
    public string? ByUrl { get; set; }

    // Subscription details
    [Column("date_from", TypeName = "date")]
    public DateOnly? DateFrom { get; set; }

    [Column("date_till", TypeName = "date")]
    public DateOnly? DateTill { get; set; }

    [Column("num_users")]
    public int? NumUsers { get; set; }

    [Column("num_tokens")]
    public int? NumTokens { get; set; }

    [Column("num_clients")]
    public int? NumClients { get; set; }

    [MaxLength(30)]
    [Column("level")]
    public string? Level { get; set; }

    [Column("signature")]
    public string? Signature { get; set; }
}
