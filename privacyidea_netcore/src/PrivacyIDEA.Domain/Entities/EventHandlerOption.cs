using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Event handler options
/// Maps to Python: privacyidea/models/event.py - EventHandlerOption class
/// </summary>
[Table("eventhandleroption")]
public class EventHandlerOption
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("eventhandler_id")]
    public int EventHandlerId { get; set; }

    [Column("key")]
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("type")]
    [MaxLength(100)]
    public string? Type { get; set; }

    [Column("description")]
    [MaxLength(2000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(EventHandlerId))]
    public virtual EventHandler? EventHandler { get; set; }
}
