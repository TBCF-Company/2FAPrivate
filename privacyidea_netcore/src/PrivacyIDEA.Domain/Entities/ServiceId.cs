using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Service identifier for application services
/// </summary>
[Table("serviceid")]
public class ServiceId
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Column("description")]
    public string? Description { get; set; }
}
