// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/audit.py, cache.py, and others

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Audit log table for compliance and security monitoring
    /// Equivalent to Python's Audit class
    /// </summary>
    [Table("audit")]
    [Index(nameof(Date))]
    [Index(nameof(Action))]
    [Index(nameof(Serial))]
    public class Audit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime? Date { get; set; }

        [StringLength(50)]
        public string? Signature { get; set; }

        [StringLength(50)]
        public string? Action { get; set; }

        public bool? Success { get; set; }

        [StringLength(40)]
        public string? Serial { get; set; }

        [StringLength(30)]
        public string? TokenType { get; set; }

        [StringLength(80)]
        public string? User { get; set; }

        [StringLength(80)]
        public string? Realm { get; set; }

        [StringLength(120)]
        public string? Resolver { get; set; }

        [StringLength(80)]
        public string? Administrator { get; set; }

        [StringLength(50)]
        public string? ActionDetail { get; set; }

        [Column(TypeName = "text")]
        public string? Info { get; set; }

        [StringLength(255)]
        public string? PrivacyIDEAServer { get; set; }

        [StringLength(40)]
        public string? Client { get; set; }

        [StringLength(40)]
        public string? LogLevel { get; set; }

        [StringLength(255)]
        public string? Clearance { get; set; }

        public Audit()
        {
            Date = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Authentication cache for performance optimization
    /// Equivalent to Python's AuthCache class
    /// </summary>
    [Table("authcache")]
    [Index(nameof(FirstAuth))]
    [Index(nameof(Authentication))]
    public class AuthCache : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(60)]
        public string Authentication { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Realm { get; set; }

        [StringLength(120)]
        public string? Resolver { get; set; }

        public DateTime FirstAuth { get; set; }

        public DateTime? LastAuth { get; set; }

        public AuthCache()
        {
            FirstAuth = DateTime.UtcNow;
        }

        public AuthCache(string authentication, string username, string? realm = null, string? resolver = null)
        {
            Authentication = authentication;
            Username = username;
            Realm = realm;
            Resolver = resolver;
            FirstAuth = DateTime.UtcNow;
            LastAuth = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// User cache for performance optimization
    /// Equivalent to Python's UserCache class
    /// </summary>
    [Table("usercache")]
    [Index(nameof(Timestamp))]
    [Index(nameof(Username), nameof(Resolver))]
    public class UserCache : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Resolver { get; set; }

        [Column(TypeName = "text")]
        public string? UserInfo { get; set; }

        public DateTime Timestamp { get; set; }

        public UserCache()
        {
            Timestamp = DateTime.UtcNow;
        }

        public UserCache(string username, string? resolver = null, string? userInfo = null)
        {
            Username = username;
            Resolver = resolver;
            UserInfo = userInfo;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Service ID configuration
    /// Equivalent to Python's Serviceid class
    /// </summary>
    [Table("serviceid")]
    [Index(nameof(Name), IsUnique = true)]
    public class Serviceid : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public Serviceid()
        {
        }

        public Serviceid(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Monitoring statistics
    /// Equivalent to Python's MonitoringStats class
    /// </summary>
    [Table("monitoringstats")]
    [Index(nameof(Timestamp))]
    [Index(nameof(StatsKey))]
    public class MonitoringStats : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        public DateTime Timestamp { get; set; }

        [Required]
        [StringLength(64)]
        public string StatsKey { get; set; } = string.Empty;

        public double StatsValue { get; set; }

        public MonitoringStats()
        {
            Timestamp = DateTime.UtcNow;
        }

        public MonitoringStats(string statsKey, double statsValue)
        {
            StatsKey = statsKey;
            StatsValue = statsValue;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// CA Connector configuration
    /// Equivalent to Python's CAConnector class
    /// </summary>
    [Table("caconnector")]
    [Index(nameof(Name), IsUnique = true)]
    public class CAConnector : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ConnectorType { get; set; }

        public CAConnector()
        {
        }

        public CAConnector(string name, string? connectorType = null)
        {
            Name = name;
            ConnectorType = connectorType;
        }
    }

    /// <summary>
    /// CA Connector configuration options
    /// </summary>
    [Table("caconnectorconfig")]
    [Index(nameof(CAConnectorId), nameof(Key), IsUnique = true)]
    public class CAConnectorConfig : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(CAConnector))]
        public int CAConnectorId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Value { get; set; }

        [StringLength(100)]
        public string? Type { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        // Navigation property
        public CAConnector? CAConnector { get; set; }
    }

    /// <summary>
    /// Custom user attributes
    /// </summary>
    [Table("customuserattribute")]
    [Index(nameof(Username), nameof(Resolver), nameof(Key), IsUnique = true)]
    public class CustomUserAttribute : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Resolver { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Value { get; set; }
    }
}
