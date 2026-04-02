using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Token entity - stores basic token data
/// Maps to Python: privacyidea/models/token.py - Token class
/// </summary>
[Table("token")]
public class Token
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("description")]
    [MaxLength(80)]
    public string Description { get; set; } = string.Empty;

    [Column("serial")]
    [Required]
    [MaxLength(40)]
    public string Serial { get; set; } = string.Empty;

    [Column("tokentype")]
    [MaxLength(30)]
    public string TokenType { get; set; } = "hotp";

    [Column("user_pin")]
    [MaxLength(512)]
    public string? UserPin { get; set; }

    [Column("user_pin_iv")]
    [MaxLength(32)]
    public string? UserPinIv { get; set; }

    [Column("so_pin")]
    [MaxLength(512)]
    public string? SoPin { get; set; }

    [Column("so_pin_iv")]
    [MaxLength(32)]
    public string? SoPinIv { get; set; }

    [Column("pin_seed")]
    [MaxLength(32)]
    public string? PinSeed { get; set; }

    [Column("otplen")]
    public int OtpLen { get; set; } = 6;

    [Column("pin_hash")]
    [MaxLength(512)]
    public string? PinHash { get; set; }

    [Column("key_enc")]
    [MaxLength(2800)]
    public string? KeyEnc { get; set; }

    [Column("key_iv")]
    [MaxLength(32)]
    public string? KeyIv { get; set; }

    [Column("maxfail")]
    public int MaxFail { get; set; } = 10;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("revoked")]
    public bool Revoked { get; set; } = false;

    [Column("locked")]
    public bool Locked { get; set; } = false;

    [Column("failcount")]
    public int FailCount { get; set; } = 0;

    [Column("count")]
    public int Count { get; set; } = 0;

    /// <summary>
    /// HOTP counter value (alias for Count, used for HOTP tokens)
    /// </summary>
    [NotMapped]
    public int Counter
    {
        get => Count;
        set => Count = value;
    }

    [Column("count_window")]
    public int CountWindow { get; set; } = 10;

    [Column("sync_window")]
    public int SyncWindow { get; set; } = 1000;

    /// <summary>
    /// TOTP time step in seconds (stored in TokenInfo as "timeStep")
    /// </summary>
    [NotMapped]
    public int TimeStep { get; set; } = 30;

    [Column("rollout_state")]
    [MaxLength(10)]
    public string? RolloutState { get; set; }

    // Navigation properties
    public virtual ICollection<TokenInfo> TokenInfos { get; set; } = new List<TokenInfo>();
    public virtual ICollection<TokenOwner> TokenOwners { get; set; } = new List<TokenOwner>();
    public virtual ICollection<TokenRealm> TokenRealms { get; set; } = new List<TokenRealm>();
}
