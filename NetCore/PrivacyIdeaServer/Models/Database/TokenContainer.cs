// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/tokencontainer.py and tokengroup.py

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Token containers for grouping tokens
    /// Equivalent to Python's TokenContainer class
    /// </summary>
    [Table("tokencontainer")]
    [Index(nameof(Serial), IsUnique = true)]
    [Index(nameof(Type))]
    public class TokenContainer : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(64)]
        public string Serial { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Type { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<TokenContainerInfo> InfoList { get; set; } = new List<TokenContainerInfo>();
        public ICollection<TokenContainerRealm> Realms { get; set; } = new List<TokenContainerRealm>();
        public ICollection<TokenContainerOwner> Owners { get; set; } = new List<TokenContainerOwner>();
        public ICollection<TokenContainerToken> Tokens { get; set; } = new List<TokenContainerToken>();
        public ICollection<TokenContainerStates> States { get; set; } = new List<TokenContainerStates>();

        public TokenContainer()
        {
        }

        public TokenContainer(string serial, string? type = null, string? description = null)
        {
            Serial = serial;
            Type = type;
            Description = description;
        }
    }

    /// <summary>
    /// Token container info (key-value pairs)
    /// </summary>
    [Table("tokencontainerinfo")]
    [Index(nameof(ContainerId), nameof(Key), IsUnique = true)]
    public class TokenContainerInfo : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(TokenContainer))]
        public int ContainerId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Value { get; set; } = string.Empty;

        // Navigation property
        public TokenContainer? TokenContainer { get; set; }
    }

    /// <summary>
    /// Links token containers to realms
    /// </summary>
    [Table("tokencontainerrealm")]
    [Index(nameof(ContainerId), nameof(RealmId), IsUnique = true)]
    public class TokenContainerRealm : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(TokenContainer))]
        public int ContainerId { get; set; }

        [ForeignKey(nameof(Realm))]
        public int RealmId { get; set; }

        // Navigation properties
        public TokenContainer? TokenContainer { get; set; }
        public Realm? Realm { get; set; }
    }

    /// <summary>
    /// Links token containers to owners
    /// </summary>
    [Table("tokencontainerowner")]
    public class TokenContainerOwner : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(TokenContainer))]
        public int ContainerId { get; set; }

        [ForeignKey(nameof(Realm))]
        public int? RealmId { get; set; }

        [StringLength(320)]
        public string? Resolver { get; set; }

        [StringLength(320)]
        public string? UserId { get; set; }

        // Navigation properties
        public TokenContainer? TokenContainer { get; set; }
        public Realm? Realm { get; set; }
    }

    /// <summary>
    /// Links tokens to containers
    /// </summary>
    [Table("tokencontainertoken")]
    [Index(nameof(ContainerId), nameof(TokenId), IsUnique = true)]
    public class TokenContainerToken : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(TokenContainer))]
        public int ContainerId { get; set; }

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        // Navigation properties
        public TokenContainer? TokenContainer { get; set; }
        public Token? Token { get; set; }
    }

    /// <summary>
    /// Container states
    /// </summary>
    [Table("tokencontainerstates")]
    public class TokenContainerStates : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(TokenContainer))]
        public int ContainerId { get; set; }

        [StringLength(64)]
        public string? State { get; set; }

        // Navigation property
        public TokenContainer? TokenContainer { get; set; }
    }

    /// <summary>
    /// Container templates
    /// </summary>
    [Table("tokencontainertemplate")]
    public class TokenContainerTemplate : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Template { get; set; }
    }

    /// <summary>
    /// Token groups for organizing tokens
    /// Equivalent to Python's Tokengroup class
    /// </summary>
    [Table("tokengroup")]
    [Index(nameof(Name), IsUnique = true)]
    public class Tokengroup : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation property
        public ICollection<TokenTokengroup> Tokens { get; set; } = new List<TokenTokengroup>();
    }

    /// <summary>
    /// Links tokens to token groups
    /// </summary>
    [Table("tokentokengroup")]
    [Index(nameof(TokenId), nameof(TokengroupId), IsUnique = true)]
    public class TokenTokengroup : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        [ForeignKey(nameof(Tokengroup))]
        public int TokengroupId { get; set; }

        // Navigation properties
        public Token? Token { get; set; }
        public Tokengroup? Tokengroup { get; set; }
    }
}
