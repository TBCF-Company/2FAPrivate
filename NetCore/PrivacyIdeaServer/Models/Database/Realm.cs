// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/realm.py

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// The realm table contains the defined realms. User Resolvers can be
    /// grouped to realms. This table contains just the names of
    /// the realms. The linking to resolvers is stored in the table "resolverrealm".
    /// Equivalent to Python's Realm class
    /// </summary>
    [Table("realm")]
    [Index(nameof(Name), IsUnique = true)]
    public class Realm : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public bool Default { get; set; } = false;

        // Navigation properties
        public ICollection<ResolverRealm> ResolverList { get; set; } = new List<ResolverRealm>();
        public ICollection<TokenOwner> TokenOwners { get; set; } = new List<TokenOwner>();
        public ICollection<TokenRealm> TokenList { get; set; } = new List<TokenRealm>();
        public ICollection<TokenContainerRealm> Containers { get; set; } = new List<TokenContainerRealm>();

        public Realm()
        {
        }

        public Realm(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// This table stores which Resolver is located in which realm
    /// This is a N:M relation
    /// Equivalent to Python's ResolverRealm class
    /// </summary>
    [Table("resolverrealm")]
    [Index(nameof(ResolverId), nameof(RealmId), nameof(NodeUuid), IsUnique = true, Name = "rrix_2")]
    public class ResolverRealm : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Resolver))]
        public int? ResolverId { get; set; }

        [ForeignKey(nameof(Realm))]
        public int? RealmId { get; set; }

        /// <summary>
        /// If there are several resolvers in a realm, the priority is used to
        /// find a user first in a resolver with a higher priority (i.e. lower number)
        /// </summary>
        public int? Priority { get; set; }

        [StringLength(36)]
        public string? NodeUuid { get; set; } = string.Empty;

        // Navigation properties
        public Resolver? Resolver { get; set; }
        public Realm? Realm { get; set; }

        public ResolverRealm()
        {
        }

        public ResolverRealm(
            int? resolverId = null,
            int? realmId = null,
            int? priority = null,
            string? nodeUuid = null)
        {
            ResolverId = resolverId;
            RealmId = realmId;
            Priority = priority;
            NodeUuid = nodeUuid ?? string.Empty;
        }
    }
}
