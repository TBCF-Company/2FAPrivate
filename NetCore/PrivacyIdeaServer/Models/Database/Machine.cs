// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/machine.py

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// This model holds the definition to the machine store.
    /// Machines could be located in flat files, LDAP directory or in puppet
    /// services or other sources.
    /// Equivalent to Python's MachineResolver class
    /// </summary>
    [Table("machineresolver")]
    [Index(nameof(Name), IsUnique = true)]
    public class MachineResolver : IMethodsMixin
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

        // Navigation property
        public ICollection<MachineResolverConfig> ResolverConfig { get; set; } = new List<MachineResolverConfig>();

        public MachineResolver()
        {
        }

        public MachineResolver(string name, string resolverType)
        {
            Name = name;
            ResolverType = resolverType;
        }
    }

    /// <summary>
    /// Each Machine Resolver can have multiple configuration entries.
    /// Equivalent to Python's MachineResolverConfig class
    /// </summary>
    [Table("machineresolverconfig")]
    [Index(nameof(ResolverId), nameof(Key), IsUnique = true, Name = "mrcix_2")]
    public class MachineResolverConfig : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(MachineResolver))]
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
        public MachineResolver? MachineResolver { get; set; }

        public MachineResolverConfig()
        {
        }

        public MachineResolverConfig(int? resolverId, string? key = null, string? value = null, string type = "", string description = "")
        {
            ResolverId = resolverId;
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Type = type;
            Description = description;
        }
    }

    /// <summary>
    /// The MachineToken table links tokens to machines.
    /// A machine can have multiple tokens.
    /// Equivalent to Python's MachineToken class
    /// </summary>
    [Table("machinetoken")]
    [Index(nameof(MachineId), nameof(TokenId), nameof(Application), IsUnique = true, Name = "mtix_1")]
    public class MachineToken : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(MachineResolver))]
        public int? MachineresolverId { get; set; }

        [Required]
        [StringLength(255)]
        public string MachineId { get; set; } = string.Empty;

        [ForeignKey(nameof(Token))]
        public int? TokenId { get; set; }

        [StringLength(64)]
        public string? Application { get; set; } = string.Empty;

        // Navigation properties
        public MachineResolver? MachineResolver { get; set; }
        public Token? Token { get; set; }
        public ICollection<MachineTokenOptions> Options { get; set; } = new List<MachineTokenOptions>();

        public MachineToken()
        {
        }

        public MachineToken(int? machineresolverId, string machineId, int? tokenId = null, string application = "")
        {
            MachineresolverId = machineresolverId;
            MachineId = machineId;
            TokenId = tokenId;
            Application = application;
        }
    }

    /// <summary>
    /// Additional options for machine-token associations
    /// Equivalent to Python's MachineTokenOptions class
    /// </summary>
    [Table("machinetokenoptions")]
    [Index(nameof(MachineTokenId), nameof(Key), IsUnique = true, Name = "mtoix_1")]
    public class MachineTokenOptions : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(MachineToken))]
        public int MachineTokenId { get; set; }

        [Required]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Value { get; set; } = string.Empty;

        // Navigation property
        public MachineToken? MachineToken { get; set; }

        public MachineTokenOptions()
        {
        }

        public MachineTokenOptions(int machineTokenId, string key, string? value = null)
        {
            MachineTokenId = machineTokenId;
            Key = key;
            Value = value;
        }
    }
}
