using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrivacyIDEA.Domain.Entities;

/// <summary>
/// Event handler configuration
/// Maps to Python: privacyidea/models/event.py - EventHandler class
/// </summary>
[Table("eventhandler")]
public class EventHandler
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("ordering")]
    public int Ordering { get; set; } = 0;

    [Column("name")]
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("event")]
    [Required]
    public string Event { get; set; } = string.Empty;

    [Column("handlermodule")]
    [Required]
    [MaxLength(255)]
    public string HandlerModule { get; set; } = string.Empty;

    [Column("position")]
    [MaxLength(10)]
    public string Position { get; set; } = "post";

    // Navigation properties
    public virtual ICollection<EventHandlerOption> Options { get; set; } = new List<EventHandlerOption>();
    public virtual ICollection<EventHandlerCondition> Conditions { get; set; } = new List<EventHandlerCondition>();
}
