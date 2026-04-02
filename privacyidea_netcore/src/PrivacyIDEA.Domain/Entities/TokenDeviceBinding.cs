using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token device binding for secure registration
/// Binds a token to a specific device for enhanced security
/// </summary>
[Table("token_device_binding")]
public class TokenDeviceBinding
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Token ID this binding belongs to
    /// </summary>
    [Column("token_id")]
    [Required]
    public int TokenId { get; set; }

    /// <summary>
    /// Unique device identifier provided by client
    /// </summary>
    [Column("device_id")]
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Binding ID returned to client
    /// </summary>
    [Column("binding_id")]
    [Required]
    [MaxLength(32)]
    public string BindingId { get; set; } = string.Empty;

    /// <summary>
    /// Device model (e.g., "iPhone 15 Pro")
    /// </summary>
    [Column("device_model")]
    [MaxLength(128)]
    public string? DeviceModel { get; set; }

    /// <summary>
    /// Operating system (e.g., "iOS", "Android")
    /// </summary>
    [Column("os")]
    [MaxLength(64)]
    public string? OS { get; set; }

    /// <summary>
    /// OS version (e.g., "17.4")
    /// </summary>
    [Column("os_version")]
    [MaxLength(32)]
    public string? OSVersion { get; set; }

    /// <summary>
    /// Application version
    /// </summary>
    [Column("app_version")]
    [MaxLength(32)]
    public string? AppVersion { get; set; }

    /// <summary>
    /// Device fingerprint for additional verification
    /// </summary>
    [Column("fingerprint")]
    [MaxLength(256)]
    public string? Fingerprint { get; set; }

    /// <summary>
    /// Client's RSA public key (for future re-keying)
    /// </summary>
    [Column("public_key")]
    public string? PublicKey { get; set; }

    /// <summary>
    /// When this binding was created
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this device was used for OTP validation
    /// </summary>
    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Is this binding active
    /// </summary>
    [Column("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    /// IP address when registered
    /// </summary>
    [Column("registered_ip")]
    [MaxLength(45)]
    public string? RegisteredIp { get; set; }

    /// <summary>
    /// IP address of last use
    /// </summary>
    [Column("last_ip")]
    [MaxLength(45)]
    public string? LastIp { get; set; }

    /// <summary>
    /// Number of successful authentications
    /// </summary>
    [Column("auth_count")]
    public int AuthCount { get; set; } = 0;

    // Navigation property
    [ForeignKey("TokenId")]
    public virtual Token Token { get; set; } = null!;
}
