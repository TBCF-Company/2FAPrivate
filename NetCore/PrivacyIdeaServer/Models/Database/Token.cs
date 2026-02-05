// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/token.py

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// Token credential ID hash table for FIDO2/WebAuthn tokens
    /// Equivalent to Python's TokenCredentialIdHash class
    /// </summary>
    [Table("tokencredentialidhash")]
    [Index(nameof(CredentialIdHash), IsUnique = true, Name = "ix_tokencredentialidhash_credentialidhash")]
    public class TokenCredentialIdHash : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [Required]
        [StringLength(256)]
        public string CredentialIdHash { get; set; } = string.Empty;

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        // Navigation property
        public Token? Token { get; set; }

        public TokenCredentialIdHash()
        {
        }

        public TokenCredentialIdHash(string credentialIdHash, int tokenId)
        {
            CredentialIdHash = credentialIdHash;
            TokenId = tokenId;
        }
    }

    /// <summary>
    /// The "Token" table contains the basic token data.
    /// It contains data like serial number, secret key, PINs, etc.
    /// Equivalent to Python's Token class
    /// </summary>
    [Table("token")]
    [Index(nameof(Serial), IsUnique = true)]
    [Index(nameof(TokenType))]
    public class Token : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [StringLength(80)]
        public string? Description { get; set; }

        [Required]
        [StringLength(40)]
        public string Serial { get; set; } = string.Empty;

        [StringLength(30)]
        public string? TokenType { get; set; }

        /// <summary>
        /// Encrypted user PIN
        /// </summary>
        [StringLength(512)]
        public string? UserPin { get; set; }

        /// <summary>
        /// IV for user PIN encryption
        /// </summary>
        [StringLength(32)]
        public string? UserPinIv { get; set; }

        /// <summary>
        /// Encrypted Security Officer PIN (for smartcards)
        /// </summary>
        [StringLength(512)]
        public string? SoPin { get; set; }

        /// <summary>
        /// IV for SO PIN encryption
        /// </summary>
        [StringLength(32)]
        public string? SoPinIv { get; set; }

        [StringLength(32)]
        public string? PinSeed { get; set; }

        public int OtpLen { get; set; } = 6;

        /// <summary>
        /// Hashed PIN value
        /// </summary>
        [StringLength(512)]
        public string? PinHash { get; set; }

        /// <summary>
        /// Encrypted OTP key
        /// </summary>
        [StringLength(2800)]
        public string? KeyEnc { get; set; }

        /// <summary>
        /// IV for key encryption
        /// </summary>
        [StringLength(32)]
        public string? KeyIv { get; set; }

        public int MaxFail { get; set; } = 10;

        public bool Active { get; set; } = true;

        public bool Revoked { get; set; } = false;

        public bool Locked { get; set; } = false;

        public int FailCount { get; set; } = 0;

        public int Count { get; set; } = 0;

        public int CountWindow { get; set; } = 10;

        public int SyncWindow { get; set; } = 1000;

        [StringLength(10)]
        public string? RolloutState { get; set; }

        // Navigation properties
        public ICollection<TokenInfo> InfoList { get; set; } = new List<TokenInfo>();
        public ICollection<TokenOwner> Owners { get; set; } = new List<TokenOwner>();
        public ICollection<TokenRealm> RealmList { get; set; } = new List<TokenRealm>();
        public ICollection<TokenContainerToken> Containers { get; set; } = new List<TokenContainerToken>();
        public ICollection<TokenTokengroup> TokengroupList { get; set; } = new List<TokenTokengroup>();
        public ICollection<MachineToken> MachineList { get; set; } = new List<MachineToken>();

        public Token()
        {
        }

        public Token(string serial, string tokenType = "", bool isActive = true, int otpLen = 6, string otpKey = "")
        {
            Serial = serial;
            TokenType = string.IsNullOrEmpty(tokenType) ? "HOTP" : tokenType;
            Active = isActive;
            OtpLen = otpLen;
            Count = 0;
            FailCount = 0;
            MaxFail = 10;
            Revoked = false;
            Locked = false;
            CountWindow = 10;
            // Note: Set OTP key using SetOtpKey method which handles encryption
        }
    }

    /// <summary>
    /// TokenInfo table stores additional token-specific information as key-value pairs
    /// Equivalent to Python's TokenInfo class
    /// </summary>
    [Table("tokeninfo")]
    [Index(nameof(TokenId), nameof(Key), IsUnique = true, Name = "tiix_2")]
    public class TokenInfo : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        [Required]
        [StringLength(255)]
        public string Key { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Value { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Type { get; set; } = "string";

        [StringLength(2000)]
        public string? Description { get; set; } = string.Empty;

        // Navigation property
        public Token? Token { get; set; }

        public TokenInfo()
        {
        }

        public TokenInfo(int tokenId, string key, string? value = null, string type = "string", string description = "")
        {
            TokenId = tokenId;
            Key = key;
            Value = value ?? string.Empty;
            Type = type;
            Description = description;
        }
    }

    /// <summary>
    /// TokenOwner table links tokens to their owners (users)
    /// Equivalent to Python's TokenOwner class
    /// </summary>
    [Table("tokenowner")]
    [Index(nameof(TokenId))]
    [Index(nameof(RealmId), nameof(UserId))]
    public class TokenOwner : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        [ForeignKey(nameof(Realm))]
        public int? RealmId { get; set; }

        [StringLength(320)]
        public string? Resolver { get; set; }

        [StringLength(320)]
        public string? UserId { get; set; }

        // Navigation properties
        public Token? Token { get; set; }
        public Realm? Realm { get; set; }

        public TokenOwner()
        {
        }

        public TokenOwner(int tokenId, int? realmId = null, string? resolver = null, string? userId = null)
        {
            TokenId = tokenId;
            RealmId = realmId;
            Resolver = resolver;
            UserId = userId;
        }
    }

    /// <summary>
    /// TokenRealm table links tokens to realms (many-to-many)
    /// Equivalent to Python's TokenRealm class
    /// </summary>
    [Table("tokenrealm")]
    [Index(nameof(TokenId), nameof(RealmId), IsUnique = true, Name = "trix_2")]
    public class TokenRealm : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Token))]
        public int TokenId { get; set; }

        [ForeignKey(nameof(Realm))]
        public int RealmId { get; set; }

        // Navigation properties
        public Token? Token { get; set; }
        public Realm? Realm { get; set; }

        public TokenRealm()
        {
        }

        public TokenRealm(int tokenId, int realmId)
        {
            TokenId = tokenId;
            RealmId = realmId;
        }
    }
}
