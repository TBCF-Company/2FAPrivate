using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Event handler conditions
/// Maps to Python: privacyidea/models/event.py - EventHandlerCondition class
/// </summary>
[Table("eventhandlercondition")]
public class EventHandlerCondition
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

    [Column("comparator")]
    [MaxLength(255)]
    public string? Comparator { get; set; }

    [ForeignKey(nameof(EventHandlerId))]
    public virtual EventHandler? EventHandler { get; set; }
}
