using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Challenge entity for challenge-response authentication
/// Maps to Python: privacyidea/models/challenge.py - Challenge class
/// </summary>
[Table("challenge")]
public class Challenge
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("transaction_id")]
    [Required]
    [MaxLength(64)]
    public string TransactionId { get; set; } = string.Empty;

    [Column("data")]
    public string? Data { get; set; }

    [Column("challenge")]
    public string? ChallengeData { get; set; }

    [Column("session")]
    public string? Session { get; set; }

    [Column("serial")]
    [MaxLength(40)]
    public string? Serial { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("expiration")]
    public DateTime? Expiration { get; set; }

    [Column("otp_valid")]
    public bool? OtpValid { get; set; }

    [Column("otp_received")]
    public bool OtpReceived { get; set; } = false;
}
