// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/smsgateway.py and event.py

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// SMS Gateway configuration
    /// Equivalent to Python's SMSGateway class
    /// </summary>
    [Table("smsgateway")]
    [Index(nameof(Identifier), IsUnique = true)]
    public class SMSGateway : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Identifier { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ProviderModule { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        // Navigation property
        public ICollection<SMSGatewayOption> Options { get; set; } = new List<SMSGatewayOption>();

        public SMSGateway()
        {
        }

        public SMSGateway(string identifier, string? providerModule = null, string? description = null)
        {
            Identifier = identifier;
            ProviderModule = providerModule;
            Description = description;
        }
    }

    /// <summary>
    /// SMS Gateway options
    /// Equivalent to Python's SMSGatewayOption class
    /// </summary>
    [Table("smsgatewayoption")]
    [Index(nameof(GatewayId), nameof(Key), IsUnique = true)]
    public class SMSGatewayOption : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(SMSGateway))]
        public int GatewayId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Value { get; set; }

        // Navigation property
        public SMSGateway? SMSGateway { get; set; }
    }

    /// <summary>
    /// Event handlers for automated actions
    /// Equivalent to Python's EventHandler class
    /// </summary>
    [Table("eventhandler")]
    [Index(nameof(Name), IsUnique = true)]
    public class EventHandler : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;

        [StringLength(64)]
        public string? Event { get; set; }

        [StringLength(64)]
        public string? HandlerModule { get; set; }

        [StringLength(64)]
        public string? Action { get; set; }

        public int? Position { get; set; }

        // Navigation properties
        public ICollection<EventHandlerOption> Options { get; set; } = new List<EventHandlerOption>();
        public ICollection<EventHandlerCondition> Conditions { get; set; } = new List<EventHandlerCondition>();

        public EventHandler()
        {
        }

        public EventHandler(string name, string? eventType = null, string? handlerModule = null, string? action = null)
        {
            Name = name;
            Event = eventType;
            HandlerModule = handlerModule;
            Action = action;
        }
    }

    /// <summary>
    /// Event handler options
    /// Equivalent to Python's EventHandlerOption class
    /// </summary>
    [Table("eventhandleroption")]
    [Index(nameof(EventHandlerId), nameof(Key), IsUnique = true)]
    public class EventHandlerOption : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(EventHandler))]
        public int EventHandlerId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Value { get; set; }

        // Navigation property
        public EventHandler? EventHandler { get; set; }
    }

    /// <summary>
    /// Event handler conditions
    /// Equivalent to Python's EventHandlerCondition class
    /// </summary>
    [Table("eventhandlercondition")]
    public class EventHandlerCondition : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(EventHandler))]
        public int EventHandlerId { get; set; }

        [Required]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Value { get; set; }

        [StringLength(256)]
        public string? Comparator { get; set; }

        // Navigation property
        public EventHandler? EventHandler { get; set; }
    }

    /// <summary>
    /// Event counter for tracking occurrences
    /// Equivalent to Python's EventCounter class
    /// </summary>
    [Table("eventcounter")]
    [Index(nameof(Counter), nameof(Node), IsUnique = true)]
    public class EventCounter : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(80)]
        public string Counter { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Node { get; set; } = string.Empty;

        public int CounterValue { get; set; } = 0;

        public DateTime? ResetAt { get; set; }

        public DateTime? LastReset { get; set; }

        public EventCounter()
        {
        }

        public EventCounter(string counter, string? node = null)
        {
            Counter = counter;
            Node = node ?? string.Empty;
        }
    }
}
