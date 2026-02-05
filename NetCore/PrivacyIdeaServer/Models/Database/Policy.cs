// SPDX-FileCopyrightText: (C) 2025 NetKnights GmbH <https://netknights.it>
// SPDX-License-Identifier: AGPL-3.0-or-later
//
// Converted from Python to C# from privacyidea/models/policy.py

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PrivacyIdeaServer.Models.Database
{
    /// <summary>
    /// The policy table contains the policy definitions.
    /// The Policies control the behaviour in the scopes:
    ///  * enrollment
    ///  * authentication
    ///  * authorization
    ///  * administration
    ///  * user actions
    ///  * webui
    /// Equivalent to Python's Policy class
    /// </summary>
    [Table("policy")]
    [Index(nameof(Name), IsUnique = true)]
    public class Policy : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        public bool Active { get; set; } = true;

        public bool CheckAllResolvers { get; set; } = false;

        [Required]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        public bool UserCaseInsensitive { get; set; } = false;

        [Required]
        [StringLength(32)]
        public string Scope { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Action { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Realm { get; set; } = string.Empty;

        [StringLength(256)]
        public string? AdminRealm { get; set; } = string.Empty;

        [StringLength(256)]
        public string? AdminUser { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Resolver { get; set; } = string.Empty;

        [StringLength(256)]
        public string? PiNode { get; set; } = string.Empty;

        [StringLength(256)]
        public string? User { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Client { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Time { get; set; } = string.Empty;

        [StringLength(256)]
        public string? UserAgents { get; set; } = string.Empty;

        /// <summary>
        /// If there are multiple matching policies, choose the one
        /// with the lowest priority number. Default priority is 1.
        /// </summary>
        public int Priority { get; set; } = 1;

        // Navigation properties
        public ICollection<PolicyCondition> Conditions { get; set; } = new List<PolicyCondition>();
        public ICollection<PolicyDescription> Descriptions { get; set; } = new List<PolicyDescription>();

        public Policy()
        {
        }

        public Policy(string name, bool active = true, string scope = "", string action = "")
        {
            Name = name;
            Active = active;
            Scope = scope;
            Action = action;
        }
    }

    /// <summary>
    /// Policy conditions for advanced policy matching
    /// Equivalent to Python's PolicyCondition class
    /// </summary>
    [Table("policycondition")]
    public class PolicyCondition : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Policy))]
        public int PolicyId { get; set; }

        [Required]
        [StringLength(64)]
        public string Key { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Value { get; set; } = string.Empty;

        [StringLength(256)]
        public string? Comparator { get; set; } = string.Empty;

        public bool Active { get; set; } = true;

        // Navigation property
        public Policy? Policy { get; set; }

        public PolicyCondition()
        {
        }

        public PolicyCondition(int policyId, string key, string? value = null, string? comparator = null)
        {
            PolicyId = policyId;
            Key = key;
            Value = value;
            Comparator = comparator;
        }
    }

    /// <summary>
    /// Policy description for documentation
    /// Equivalent to Python's PolicyDescription class
    /// </summary>
    [Table("policydescription")]
    public class PolicyDescription : IMethodsMixin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        int? IMethodsMixin.Id => Id;

        [ForeignKey(nameof(Policy))]
        public int PolicyId { get; set; }

        [Column(TypeName = "text")]
        public string? Description { get; set; } = string.Empty;

        // Navigation property
        public Policy? Policy { get; set; }

        public PolicyDescription()
        {
        }

        public PolicyDescription(int policyId, string? description = null)
        {
            PolicyId = policyId;
            Description = description;
        }
    }
}
