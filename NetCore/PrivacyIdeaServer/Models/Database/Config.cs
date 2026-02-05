// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/config.py

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// The config table holds all the system configuration in key value pairs.
    /// Additional configuration for realms, resolvers and machine resolvers is
    /// stored in specific tables.
    /// Equivalent to Python's Config class
    /// </summary>
    [Table("config")]
    [Index(nameof(Key), IsUnique = true)]
    public class Config
    {
        [Key]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Value { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Type { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; } = string.Empty;

        public Config()
        {
        }

        public Config(string key, string value, string type = "", string description = "")
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Type = type ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public override string ToString()
        {
            return $"<{Key} ({Type})>";
        }
    }

    /// <summary>
    /// Constants for config table
    /// </summary>
    public static class ConfigConstants
    {
        public const string PrivacyIdeaTimestamp = "__timestamp__";
        public const string SafeStore = "PI_DB_SAFE_STORE";
    }

    /// <summary>
    /// Node name table for tracking cluster nodes
    /// Equivalent to Python's NodeName class
    /// </summary>
    [Table("nodename")]
    [Index(nameof(Name))]
    [Index(nameof(LastSeen))]
    public class NodeName
    {
        [Key]
        [StringLength(36)]
        public string Id { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Name { get; set; }

        public DateTime? LastSeen { get; set; }

        public NodeName()
        {
            Id = Guid.NewGuid().ToString();
            LastSeen = DateTime.UtcNow;
        }

        public NodeName(string id, string? name = null)
        {
            Id = id;
            Name = name;
            LastSeen = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// The administrators for managing the system.
    /// To manage the administrators use the command pi-manage (in Python)
    /// or equivalent .NET CLI tools.
    /// Equivalent to Python's Admin class
    /// </summary>
    [Table("admin")]
    public class Admin
    {
        [Key]
        [StringLength(120)]
        public string Username { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Password { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        public Admin()
        {
        }

        public Admin(string username, string? password = null, string? email = null)
        {
            Username = username ?? string.Empty;
            Password = password;
            Email = email;
        }
    }

    /// <summary>
    /// Table for handling password resets.
    /// This table stores the recovery codes sent to a given user.
    /// The application should save the HASH of the recovery code.
    /// Equivalent to Python's PasswordReset class
    /// </summary>
    [Table("passwordreset")]
    [Index(nameof(Username))]
    [Index(nameof(Realm))]
    public class PasswordReset : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string RecoveryCode { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string Realm { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Resolver { get; set; }

        [StringLength(255)]
        public string? Email { get; set; }

        public DateTime? Timestamp { get; set; }

        public DateTime? Expiration { get; set; }

        public PasswordReset()
        {
            Timestamp = DateTime.UtcNow;
            Expiration = DateTime.UtcNow.AddHours(1);
        }

        public PasswordReset(
            string recoveryCode,
            string username,
            string realm,
            string? resolver = null,
            string? email = null,
            DateTime? timestamp = null,
            DateTime? expiration = null,
            int expirationSeconds = 3600)
        {
            RecoveryCode = recoveryCode;
            Username = username;
            Realm = realm;
            Resolver = resolver ?? string.Empty;
            Email = email;
            Timestamp = timestamp ?? DateTime.UtcNow;
            Expiration = expiration ?? DateTime.UtcNow.AddSeconds(expirationSeconds);
        }
    }
}
