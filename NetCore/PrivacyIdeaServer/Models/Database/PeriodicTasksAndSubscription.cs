// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/periodictask.py and subscription.py

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Periodic task definitions
    /// Equivalent to Python's PeriodicTask class
    /// </summary>
    [Table("periodictask")]
    [Index(nameof(Name), IsUnique = true)]
    public class PeriodicTask : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;

        [StringLength(20)]
        public string? Interval { get; set; }

        [Required]
        [StringLength(255)]
        public string TaskModule { get; set; } = string.Empty;

        public int? Ordering { get; set; }

        [StringLength(255)]
        public string? Node { get; set; }

        // Navigation properties
        public ICollection<PeriodicTaskOption> Options { get; set; } = new List<PeriodicTaskOption>();
        public ICollection<PeriodicTaskLastRun> LastRuns { get; set; } = new List<PeriodicTaskLastRun>();

        public PeriodicTask()
        {
        }

        public PeriodicTask(string name, string taskModule, bool active = true, string? interval = null)
        {
            Name = name;
            TaskModule = taskModule;
            Active = active;
            Interval = interval;
        }
    }

    /// <summary>
    /// Periodic task options
    /// Equivalent to Python's PeriodicTaskOption class
    /// </summary>
    [Table("periodictaskoption")]
    [Index(nameof(PeriodicTaskId), nameof(Key), IsUnique = true)]
    public class PeriodicTaskOption : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(PeriodicTask))]
        public int PeriodicTaskId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Value { get; set; }

        // Navigation property
        public PeriodicTask? PeriodicTask { get; set; }
    }

    /// <summary>
    /// Tracks when periodic tasks were last executed
    /// Equivalent to Python's PeriodicTaskLastRun class
    /// </summary>
    [Table("periodictasklastrun")]
    [Index(nameof(PeriodicTaskId), nameof(Node), IsUnique = true)]
    public class PeriodicTaskLastRun : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(PeriodicTask))]
        public int PeriodicTaskId { get; set; }

        [StringLength(255)]
        public string? Node { get; set; }

        public DateTime? LastRun { get; set; }

        // Navigation property
        public PeriodicTask? PeriodicTask { get; set; }
    }

    /// <summary>
    /// Client application registrations for subscription management
    /// Equivalent to Python's ClientApplication class
    /// </summary>
    [Table("clientapplication")]
    [Index(nameof(ClientId), IsUnique = true)]
    public class ClientApplication : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string ClientId { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(255)]
        public string? Ip { get; set; }

        [StringLength(255)]
        public string? Hostname { get; set; }

        [StringLength(36)]
        public string? NodeUuid { get; set; }

        public DateTime? LastSeen { get; set; }

        public ClientApplication()
        {
        }

        public ClientApplication(string clientId, string? name = null, string? ip = null)
        {
            ClientId = clientId;
            Name = name;
            Ip = ip;
            LastSeen = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Subscription information for licensing
    /// Equivalent to Python's Subscription class
    /// </summary>
    [Table("subscription")]
    public class Subscription : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(ClientApplication))]
        public int ApplicationId { get; set; }

        [StringLength(255)]
        public string? SubscriptionId { get; set; }

        [StringLength(255)]
        public string? SubscriptionState { get; set; }

        [Column(TypeName = "text")]
        public string? Signature { get; set; }

        public DateTime? DateCreated { get; set; }

        // Navigation property
        public ClientApplication? ClientApplication { get; set; }

        public Subscription()
        {
            DateCreated = DateTime.UtcNow;
        }

        public Subscription(int applicationId, string? subscriptionId = null)
        {
            ApplicationId = applicationId;
            SubscriptionId = subscriptionId;
            DateCreated = DateTime.UtcNow;
        }
    }
}
