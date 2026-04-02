using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// SMTP Server configuration
/// Maps to Python: privacyidea/models/server.py - SMTPServer class
/// </summary>
[Table("smtpserver")]
public class SmtpServer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("identifier")]
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    [Column("server")]
    [Required]
    [MaxLength(255)]
    public string Server { get; set; } = string.Empty;

    [Column("port")]
    public int Port { get; set; } = 25;

    [Column("username")]
    [MaxLength(255)]
    public string? Username { get; set; }

    [Column("password")]
    [MaxLength(255)]
    public string? Password { get; set; }

    [Column("sender")]
    [MaxLength(255)]
    public string? Sender { get; set; }

    [Column("tls")]
    public bool Tls { get; set; } = false;

    [Column("description")]
    [MaxLength(1024)]
    public string? Description { get; set; }

    [Column("timeout")]
    public int Timeout { get; set; } = 10;
}
