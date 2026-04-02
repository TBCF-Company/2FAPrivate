using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// SMS Gateway configuration
/// Maps to Python: privacyidea/models/smsgateway.py - SMSGateway class
/// </summary>
[Table("smsgateway")]
public class SmsGateway
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("identifier")]
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(1024)]
    public string? Description { get; set; }

    [Column("providermodule")]
    [Required]
    [MaxLength(1024)]
    public string ProviderModule { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<SmsGatewayOption> Options { get; set; } = new List<SmsGatewayOption>();
}
