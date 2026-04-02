using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// SMS Gateway options
/// Maps to Python: privacyidea/models/smsgateway.py - SMSGatewayOption class
/// </summary>
[Table("smsgatewayoption")]
public class SmsGatewayOption
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("gateway_id")]
    public int GatewayId { get; set; }

    [Column("key")]
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("type")]
    [MaxLength(100)]
    public string? Type { get; set; }

    [ForeignKey(nameof(GatewayId))]
    public virtual SmsGateway? Gateway { get; set; }
}
