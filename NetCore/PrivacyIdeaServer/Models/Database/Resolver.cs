// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/resolver.py

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// The table "resolver" contains the names and types of the defined User
    /// Resolvers. As each Resolver can have different required config values the
    /// configuration of the resolvers is stored in the table "resolverconfig".
    /// Equivalent to Python's Resolver class
    /// </summary>
    [Table("resolver")]
    [Index(nameof(Name), IsUnique = true)]
    public class Resolver : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("rtype")]
        public string ResolverType { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<ResolverConfig> ConfigList { get; set; } = new List<ResolverConfig>();
        public ICollection<ResolverRealm> RealmList { get; set; } = new List<ResolverRealm>();

        public Resolver()
        {
        }

        public Resolver(string name, string resolverType)
        {
            Name = name;
            ResolverType = resolverType;
        }
    }

    /// <summary>
    /// Each Resolver can have multiple configuration entries.
    /// Each Resolver type can have different required config values. Therefore,
    /// the configuration is stored in simple key/value pairs. If the type of a
    /// config entry is set to "password" the value of this config entry is stored
    /// encrypted.
    /// Equivalent to Python's ResolverConfig class
    /// </summary>
    [Table("resolverconfig")]
    [Index(nameof(ResolverId), nameof(Key), IsUnique = true, Name = "rcix_2")]
    public class ResolverConfig : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Resolver))]
        public int? ResolverId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Value { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Type { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; } = string.Empty;

        // Navigation property
        public Resolver? Resolver { get; set; }

        public ResolverConfig()
        {
        }

        public ResolverConfig(
            int? resolverId = null,
            string? key = null,
            string? value = null,
            string type = "",
            string description = "")
        {
            ResolverId = resolverId;
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Type = type;
            Description = description;
        }
    }
}
